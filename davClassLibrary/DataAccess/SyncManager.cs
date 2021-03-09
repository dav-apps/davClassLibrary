using davClassLibrary.Common;
using davClassLibrary.Controllers;
using davClassLibrary.Models;
using MimeTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Websockets;

namespace davClassLibrary.DataAccess
{
    internal static class SyncManager
    {
        private static bool isSyncing = false;
        private static bool syncAgain = false;

        internal static List<TableObjectDownload> fileDownloads = new List<TableObjectDownload>();
        internal static bool downloadingFiles = false;
        internal static Guid currentFileDownloadUuid = Guid.Empty;
        internal static WebClient currentFileDownloadWebClient = null;
        internal static Dictionary<Guid, List<IProgress<(Guid, int)>>> fileDownloadProgressList = new Dictionary<Guid, List<IProgress<(Guid, int)>>>();

        private static IWebSocketConnection websocketConnection;

        internal static async Task SessionSyncPush()
        {
            var accessToken = SettingsManager.GetAccessToken();
            var sessionUploadStatus = SettingsManager.GetSessionUploadStatus();

            if (string.IsNullOrEmpty(accessToken) || sessionUploadStatus == SessionUploadStatus.UpToDate)
                return;

            // Delete the session on the server
            var deleteSessionResponse = await SessionsController.DeleteSession(accessToken);

            if (deleteSessionResponse.Success)
            {
                // Remove the session
                SettingsManager.RemoveSession();
            }
            else
            {
                // Check the error
                int i = deleteSessionResponse.Errors.ToList().FindIndex(error => error.Code == ErrorCodes.SessionDoesNotExist);

                if(i != -1)
                {
                    // Remove the session
                    SettingsManager.RemoveSession();
                }
            }
        }

        internal static void LoadUser()
        {
            Dav.User.FirstName = SettingsManager.GetFirstName();
            Dav.User.Email = SettingsManager.GetEmail();
            Dav.User.TotalStorage = SettingsManager.GetTotalStorage();
            Dav.User.UsedStorage = SettingsManager.GetUsedStorage();
            Dav.User.Plan = SettingsManager.GetPlan();
            Dav.User.ProfileImageEtag = SettingsManager.GetProfileImageEtag();
            
            // TODO: Load the profile image
        }

        internal static async Task<bool> SyncUser()
        {
            if (string.IsNullOrEmpty(Dav.AccessToken)) return false;

            // Get the user
            var getUserResponse = await UsersController.GetUser();
            if (!getUserResponse.Success)
            {
                Dav.Logout();
                return false;
            }

            var userResponseData = getUserResponse.Data;

            // Update the values in the local settings
            if (Dav.User.Email != userResponseData.Email) SettingsManager.SetEmail(userResponseData.Email);
            if (Dav.User.FirstName != userResponseData.FirstName) SettingsManager.SetFirstName(userResponseData.FirstName);
            if (Dav.User.TotalStorage != userResponseData.TotalStorage) SettingsManager.SetTotalStorage(userResponseData.TotalStorage);
            if (Dav.User.UsedStorage != userResponseData.UsedStorage) SettingsManager.SetUsedStorage(userResponseData.UsedStorage);
            if (Dav.User.Plan != userResponseData.Plan) SettingsManager.SetPlan(userResponseData.Plan);

            if(Dav.User.ProfileImageEtag != userResponseData.ProfileImageEtag)
            {
                // TODO: Download the new profile image
            }

            return true;
        }

        internal static async Task<bool> Sync()
        {
            if (
                isSyncing
                || string.IsNullOrEmpty(Dav.AccessToken)
            ) return false;
            isSyncing = true;

            // Holds the table ids, e.g. 1, 2, 3, 4
            var tableIds = Dav.TableIds;
            // Holds the parallel table ids, e.g. 2, 3
            var parallelTableIds = Dav.TableIds;
            // Holds the order of the table ids, sorted by the pages and the parallel table ids, e.g. 1, 2, 3, 2, 3, 4
            var sortedTableIds = new List<int>();
            // Holds the pages of the table; in the format <tableId, pages>
            var tablePages = new Dictionary<int, int>();
            // Holds the last downloaded page; in the format <tableId, pages>
            var currentTablePages = new Dictionary<int, int>();
            // Holds the latest table result; in the format <tableId, tableData>
            var tableResults = new Dictionary<int, GetTableResponse>();
            // Holds the uuids of the table objects that were removed on the server but not locally; in the format <tableId, List<Guid>>
            var removedTableObjectUuids = new Dictionary<int, List<Guid>>();
            // Is true if all http calls of the specified table are successful; in the format <tableId, bool>
            var tableGetResultsOkay = new Dictionary<int, bool>();

            if (tableIds == null || parallelTableIds == null) return false;

            // Populate removedTableObjectUuids
            foreach (var tableId in tableIds)
            {
                removedTableObjectUuids[tableId] = new List<Guid>();

                foreach (var tableObject in await Dav.Database.GetAllTableObjectsAsync(tableId, true))
                    removedTableObjectUuids[tableId].Add(tableObject.Uuid);
            }

            // Get the first page of each table and generate the sorted tableIds list
            foreach (var tableId in tableIds)
            {
                // Get the first page of the table
                var getTableResult = await TablesController.GetTable(tableId);

                tableGetResultsOkay[tableId] = getTableResult.Status == 200;
                if (getTableResult.Status != 200) continue;

                var tableData = getTableResult.Data;
                
                // Save the result
                tableResults[tableId] = tableData;
                tablePages[tableId] = tableResults[tableId].Pages;
                currentTablePages[tableId] = 1;
            }

            sortedTableIds = Utils.SortTableIds(tableIds, parallelTableIds, tablePages);

            // Process the table results
            foreach (var tableId in sortedTableIds)
            {
                if (!tableGetResultsOkay[tableId]) continue;

                var tableObjects = tableResults[tableId].TableObjects;
                bool tableChanged = false;

                foreach (var obj in tableObjects)
                {
                    removedTableObjectUuids[tableId].Remove(obj.Uuid);

                    // Is obj in the database?
                    var currentTableObject = await Dav.Database.GetTableObjectAsync(obj.Uuid);

                    if (currentTableObject != null)
                    {
                        // Has the etag changed?
                        if (Equals(obj.Etag, currentTableObject.Etag))
                        {
                            // Is it a file and is it already downloaded?
                            if (currentTableObject.IsFile && !currentTableObject.FileDownloaded())
                            {
                                // Download the file
                                fileDownloads.Add(new TableObjectDownload
                                {
                                    uuid = currentTableObject.Uuid
                                });
                            }
                        }
                        else
                        {
                            // Get the updated table object from the server
                            var getTableObjectResponse = await TableObjectsController.GetTableObject(currentTableObject.Uuid);
                            if (getTableObjectResponse.Status != 200) continue;

                            var tableObject = getTableObjectResponse.Data;
                            tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;

                            // Is it a file?
                            if (tableObject.IsFile)
                            {
                                // Set the old etag
                                tableObject.Etag = currentTableObject.Etag;

                                // Save the table object
                                await tableObject.SaveWithPropertiesAsync();

                                // Download the file
                                fileDownloads.Add(new TableObjectDownload
                                {
                                    uuid = tableObject.Uuid,
                                    etag = obj.Etag
                                });
                            }
                            else
                            {
                                // Save the table object
                                await tableObject.SaveWithPropertiesAsync();

                                ProjectInterface.Callbacks.UpdateTableObject(tableObject, false);
                                tableChanged = true;
                            }
                        }
                    }
                    else
                    {
                        // Get the table object
                        var getTableObjectResponse = await TableObjectsController.GetTableObject(obj.Uuid);
                        if (getTableObjectResponse.Status != 200) continue;

                        var tableObject = getTableObjectResponse.Data;
                        tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;

                        // Is it a file?
                        if (tableObject.IsFile)
                        {
                            // Save the table object
                            await tableObject.SaveWithPropertiesAsync();

                            // Download the file
                            fileDownloads.Add(new TableObjectDownload
                            {
                                uuid = tableObject.Uuid
                            });
                        }
                        else
                        {
                            // Save the table object
                            await tableObject.SaveWithPropertiesAsync();

                            ProjectInterface.Callbacks.UpdateTableObject(tableObject, false);
                            tableChanged = true;
                        }
                    }
                }

                ProjectInterface.Callbacks.UpdateAllOfTable(tableId, tableChanged);

                // Check if there is a next page
                currentTablePages[tableId]++;
                if (currentTablePages[tableId] > tablePages[tableId]) continue;

                // Get the next page
                var getTableResult = await TablesController.GetTable(tableId, currentTablePages[tableId]);
                if(getTableResult.Status != 200)
                {
                    tableGetResultsOkay[tableId] = false;
                    continue;
                }

                tableResults[tableId] = getTableResult.Data;
            }

            // RemovedTableObjects now includes all table objects that were deleted on the server but not locally
            // Delete these table objects locally
            foreach (var tableId in tableIds)
            {
                if (!tableGetResultsOkay[tableId]) continue;
                var removedTableObjects = removedTableObjectUuids[tableId];
                var tableChanged = false;

                foreach (var uuid in removedTableObjects)
                {
                    var obj = await Dav.Database.GetTableObjectAsync(uuid);
                    if (
                        obj == null
                        || obj.UploadStatus == TableObjectUploadStatus.New
                    ) continue;

                    await obj.DeleteImmediatelyAsync();

                    ProjectInterface.Callbacks.DeleteTableObject(obj.Uuid, obj.TableId);
                    tableChanged = true;
                }

                ProjectInterface.Callbacks.UpdateAllOfTable(tableId, tableChanged);
            }

            isSyncing = false;

            // Check if the sync was successful for all tables
            foreach(var value in tableGetResultsOkay.Values)
                if (!value) return false;

            return true;
        }

        public static async Task<bool> SyncPush()
        {
            if (string.IsNullOrEmpty(Dav.AccessToken)) return false;
            if (isSyncing)
            {
                syncAgain = true;
                return false;
            }
            isSyncing = true;

            List<TableObject> tableObjects = await Dav.Database.GetAllTableObjectsAsync(true);
            List<TableObject> filteredTableObjects = tableObjects.Where(obj => obj.UploadStatus != TableObjectUploadStatus.UpToDate).Reverse().ToList();

            foreach (var tableObject in tableObjects)
            {
                switch (tableObject.UploadStatus)
                {
                    case TableObjectUploadStatus.New:
                        // Check if the tableObject is a file and if it can be uploaded
                        if (tableObject.IsFile && tableObject.File != null)
                        {
                            var usedStorage = Dav.User.UsedStorage;
                            var totalStorage = Dav.User.TotalStorage;
                            var fileSize = tableObject.File.Length;

                            if (usedStorage + fileSize > totalStorage && totalStorage != 0)
                                continue;
                        }

                        var createResult = await CreateTableObjectOnServer(tableObject);

                        if (createResult.Success)
                        {
                            tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                            tableObject.Etag = createResult.Data.Etag;
                            await tableObject.SaveAsync();
                        }
                        else if (createResult.Errors != null)
                        {
                            // Check the errors
                            var errors = createResult.Errors;

                            // Check if the table object already exists
                            int i = errors.ToList().FindIndex(error => error.Code == ErrorCodes.UuidAlreadyInUse);
                            if(i != -1)
                            {
                                // Set the upload status to UpToDate
                                tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                                await tableObject.SaveAsync();
                            }
                        }
                        break;
                    case TableObjectUploadStatus.Updated:
                        var updateResult = await UpdateTableObjectOnServer(tableObject);

                        if (updateResult.Success)
                        {
                            tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                            tableObject.Etag = updateResult.Data.Etag;
                            await tableObject.SaveAsync();
                        }
                        else if (updateResult.Errors != null)
                        {
                            // Check the errors
                            var errors = updateResult.Errors;

                            // Check if the table object does not exist
                            int i = errors.ToList().FindIndex(error => error.Code == ErrorCodes.TableObjectDoesNotExist);
                            if(i != -1)
                            {
                                // Delete the table object
                                await tableObject.DeleteImmediatelyAsync();
                            }
                        }
                        break;
                    case TableObjectUploadStatus.Deleted:
                        var deleteResult = await DeleteTableObjectOnServer(tableObject);

                        if (deleteResult.Success)
                        {
                            // Delete the table object
                            await tableObject.DeleteImmediatelyAsync();
                        }
                        else
                        {
                            var errors = deleteResult.Errors;

                            int i = errors.ToList().FindIndex(
                                error =>
                                    error.Code == ErrorCodes.TableObjectDoesNotExist
                                    || error.Code == ErrorCodes.ActionNotAllowed
                            );

                            if(i != -1)
                            {
                                // Delete the table object
                                await tableObject.DeleteImmediatelyAsync();
                            }
                        }
                        break;
                }
            }

            isSyncing = false;

            if (syncAgain)
            {
                syncAgain = false;
                return await SyncPush();
            }

            return true;
        }

        internal static async Task StartWebsocketConnection()
        {
            if (
                string.IsNullOrEmpty(Dav.AccessToken)
                || Dav.Environment == Environment.Test
            ) return;

            // Create a WebsocketConnection on the server
            var createWebsocketConnectionResponse = await WebsocketConnectionsController.CreateWebsocketConnection(Dav.AccessToken);
            if (!createWebsocketConnectionResponse.Success) return;

            string token = createWebsocketConnectionResponse.Data.Token;

            websocketConnection = WebSocketFactory.Create();
            websocketConnection.OnOpened += WebsocketConnection_OnOpened;
            websocketConnection.OnMessage += WebsocketConnection_OnMessage;

            websocketConnection.Open($"{Dav.ApiBaseUrl.Replace("http", "ws")}/cable?token={token}");
        }

        private static void WebsocketConnection_OnOpened()
        {
            string json = JsonConvert.SerializeObject(new
            {
                command = "subscribe",
                identifier = "{\"channel\": \"" + Constants.tableObjectUpdateChannelName + "\"}"
            });
            websocketConnection.Send(json);
        }

        private static async void WebsocketConnection_OnMessage(string message)
        {
            TableObjectUpdateResponse json = JsonConvert.DeserializeObject<TableObjectUpdateResponse>(message);

            if (json.type == "ping")
                return;
            else if(json.type == "reject_subscription")
            {
                websocketConnection.Close();
                return;
            }

            var uuid = json.message.uuid;
            var change = json.message.change;
            var accessTokenMd5 = json.message.access_token_md5;
            if (uuid == null || string.IsNullOrEmpty(accessTokenMd5)) return;

            // Don't notify the app if the session is the current session
            if (Utils.CreateMD5(Dav.AccessToken) == accessTokenMd5) return;

            if(change == 0 || change == 1)
            {
                // Get the table object from the server and update it locally
                var getTableObjectResponse = await TableObjectsController.GetTableObject(uuid);
                if (!getTableObjectResponse.Success) return;

                var tableObject = getTableObjectResponse.Data;

                await tableObject.SaveWithPropertiesAsync();
                ProjectInterface.Callbacks.UpdateTableObject(tableObject, false);
            }
            else if(change == 2)
            {
                var tableObject = await Dav.Database.GetTableObjectAsync(uuid);
                if (tableObject == null) return;

                ProjectInterface.Callbacks.DeleteTableObject(tableObject.Uuid, tableObject.TableId);

                // Remove the table object from the database
                await Dav.Database.DeleteTableObjectImmediatelyAsync(uuid);
            }
        }

        internal static void StartFileDownloads()
        {
            _ = DownloadFiles();
        }

        internal static async Task DownloadFiles()
        {
            if (downloadingFiles) return;
            downloadingFiles = true;

            while (fileDownloads.Count > 0)
            {
                var fileDownload = fileDownloads[0];
                fileDownloads.RemoveAt(0);

                var tableObject = await Dav.Database.GetTableObjectAsync(fileDownload.uuid);
                if (tableObject == null || !tableObject.IsFile) continue;

                //tableObject.DownloadFile();

                ProjectInterface.Callbacks.UpdateTableObject(tableObject, true);
            }

            downloadingFiles = false;
        }

        private static async Task<ApiResponse<TableObject>> CreateTableObjectOnServer(TableObject tableObject)
        {
            if (string.IsNullOrEmpty(Dav.AccessToken)) return new ApiResponse<TableObject> { Success = false };

            if (tableObject.IsFile)
            {
                // Create the table object
                var createTableObjectResponse = await TableObjectsController.CreateTableObject(
                    tableObject.Uuid,
                    tableObject.TableId,
                    true,
                    new Dictionary<string, string> { { Constants.extPropertyName, tableObject.GetPropertyValue(Constants.extPropertyName) } }
                );

                if(createTableObjectResponse.Status != 201)
                {
                    // Check if the table object already exists
                    var errorResponse = createTableObjectResponse.Errors;
                    int i = errorResponse.ToList().FindIndex(error => error.Code == ErrorCodes.UuidAlreadyInUse);

                    if(i == -1) return createTableObjectResponse;
                }

                var createTableObjectResponseData = createTableObjectResponse.Data;

                if(tableObject.File != null)
                {
                    // Upload the file
                    var setTableObjectFileResponse = await TableObjectsController.SetTableObjectFile(
                        createTableObjectResponseData.Uuid,
                        tableObject.File.FullName,
                        MimeTypeMap.GetMimeType(tableObject.GetPropertyValue(Constants.extPropertyName))
                    );

                    return setTableObjectFileResponse;
                }
            }
            else
            {
                // Create the table object
                var createTableObjectResponse = await TableObjectsController.CreateTableObject(
                    tableObject.Uuid,
                    tableObject.TableId,
                    false,
                    Utils.ConvertPropertiesListToDictionary(tableObject.Properties)
                );

                return createTableObjectResponse;
            }

            return new ApiResponse<TableObject> { Success = false };
        }

        private static async Task<ApiResponse<TableObject>> UpdateTableObjectOnServer(TableObject tableObject)
        {
            if (string.IsNullOrEmpty(Dav.AccessToken)) return new ApiResponse<TableObject> { Success = false };

            if (tableObject.IsFile && tableObject.File != null)
            {
                // Upload the file
                var setTableObjectFileResponse = await TableObjectsController.SetTableObjectFile(
                    tableObject.Uuid,
                    tableObject.File.FullName,
                    MimeTypeMap.GetMimeType(tableObject.GetPropertyValue(Constants.extPropertyName))
                );

                if (setTableObjectFileResponse.Status != 200)
                {
                    return setTableObjectFileResponse;
                }

                // Check if the ext has changed
                var tableObjectResponseData = setTableObjectFileResponse.Data;
                string tableObjectResponseDataExt = tableObjectResponseData.GetPropertyValue(Constants.extPropertyName);
                string tableObjectExt = tableObject.GetPropertyValue(Constants.extPropertyName);

                if(tableObjectResponseDataExt != tableObjectExt)
                {
                    // Update the table object with the new ext
                    var updateTableObjectResponse = await TableObjectsController.UpdateTableObject(
                        tableObject.Uuid,
                        new Dictionary<string, string> { { Constants.extPropertyName, tableObjectExt } }
                    );

                    return updateTableObjectResponse;
                }

                return setTableObjectFileResponse;
            }
            else if(!tableObject.IsFile)
            {
                // Update the table object
                var updateTableObjectResponse = await TableObjectsController.UpdateTableObject(
                    tableObject.Uuid,
                    Utils.ConvertPropertiesListToDictionary(tableObject.Properties)
                );

                return updateTableObjectResponse;
            }

            return new ApiResponse<TableObject> { Success = false };
        }

        private static async Task<ApiResponse> DeleteTableObjectOnServer(TableObject tableObject)
        {
            if (string.IsNullOrEmpty(Dav.AccessToken)) return new ApiResponse { Success = false };
            return await TableObjectsController.DeleteTableObject(tableObject.Uuid);
        }
    }
}
