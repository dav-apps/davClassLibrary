﻿using davClassLibrary.Common;
using davClassLibrary.Controllers;
using davClassLibrary.Models;
using MimeTypes;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Websockets;

namespace davClassLibrary.DataAccess
{
    public static class SyncManager
    {
        private static bool isSyncing = false;
        private static bool syncAgain = false;
        private static bool syncCompleted = false;

        internal static List<TableObjectDownload> fileDownloads = new List<TableObjectDownload>();
        internal static Guid downloadingFileUuid = Guid.Empty;
        internal static bool downloadingFiles = false;
        internal static Guid currentFileDownloadUuid = Guid.Empty;
        internal static WebClient currentFileDownloadWebClient = null;
        internal static Dictionary<Guid, List<IProgress<(Guid, int)>>> fileDownloadProgressList = new Dictionary<Guid, List<IProgress<(Guid, int)>>>();

        private static IWebSocketConnection websocketConnection;
        private static bool websocketConnectionEstablished = false;

        public static async Task SessionSyncPush()
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

        public static void LoadUser()
        {
            Dav.User.FirstName = SettingsManager.GetFirstName();
            Dav.User.Email = SettingsManager.GetEmail();
            Dav.User.TotalStorage = SettingsManager.GetTotalStorage();
            Dav.User.UsedStorage = SettingsManager.GetUsedStorage();
            Dav.User.Plan = SettingsManager.GetPlan();
            Dav.User.ProfileImageEtag = SettingsManager.GetProfileImageEtag();

            // Load the profile image
            string profileImageFilePath = Path.Combine(Dav.DataPath, Constants.profileImageFileName);
            if (File.Exists(profileImageFilePath))
                Dav.User.ProfileImage = new FileInfo(profileImageFilePath);
        }

        public static async Task<bool> UserSync()
        {
            if (!Dav.IsLoggedIn) return false;

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

            if(
                !File.Exists(Path.Combine(Dav.DataPath, Constants.profileImageFileName))
                || Dav.User.ProfileImageEtag != userResponseData.ProfileImageEtag
            )
            {
                // Download the profile image
                var getProfileImageResult = await UsersController.GetProfileImageOfUser(Path.Combine(Dav.DataPath, Constants.profileImageFileName));

                if (getProfileImageResult.Success)
                    SettingsManager.SetProfileImageEtag(userResponseData.ProfileImageEtag);
            }

            LoadUser();
            ProjectInterface.Callbacks.UserSyncFinished();
            return true;
        }

        public static async Task<bool> Sync()
        {
            if (
                isSyncing
                || !Dav.IsLoggedIn
            ) return false;
            isSyncing = true;

            // Holds the table ids, e.g. 1, 2, 3, 4
            var tableIds = Dav.TableIds;
            // Holds the parallel table ids, e.g. 2, 3
            var parallelTableIds = Dav.ParallelTableIds;
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
            // Holds the new table etags for the tables
            var tableEtags = new Dictionary<int, string>();

            if (tableIds == null || parallelTableIds == null) return false;

            // Get the first page of each table and generate the sorted tableIds list
            foreach (var tableId in tableIds)
            {
                // Get the first page of the table
                var getTableResult = await TablesController.GetTable(tableId);

                tableGetResultsOkay[tableId] = getTableResult.Success;
                if (getTableResult.Status != 200) continue;

                // Check if the table has any changes
                if (getTableResult.Data.Etag == SettingsManager.GetTableEtag(tableId))
                    continue;

                var tableData = getTableResult.Data;
                
                // Save the result
                tableResults[tableId] = tableData;
                tablePages[tableId] = tableResults[tableId].Pages;
                currentTablePages[tableId] = 1;
                tableEtags[tableId] = getTableResult.Data.Etag;
            }

            sortedTableIds = Utils.SortTableIds(tableIds, parallelTableIds, tablePages);

            // Populate removedTableObjectUuids
            foreach (var tableId in sortedTableIds.Distinct())
            {
                removedTableObjectUuids[tableId] = new List<Guid>();

                foreach (var tableObject in await Dav.Database.GetAllTableObjectsAsync(tableId, true))
                    removedTableObjectUuids[tableId].Add(tableObject.Uuid);
            }

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

                            var tableObject = getTableObjectResponse.Data.TableObject;
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

                        var tableObject = getTableObjectResponse.Data.TableObject;
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

                // Check if there is a next page
                currentTablePages[tableId]++;

                if (currentTablePages[tableId] > tablePages[tableId])
                {
                    ProjectInterface.Callbacks.UpdateAllOfTable(tableId, tableChanged, true);

                    // Save the new table etag
                    SettingsManager.SetTableEtag(tableId, tableEtags[tableId]);

                    continue;
                }

                ProjectInterface.Callbacks.UpdateAllOfTable(tableId, tableChanged, false);

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
            foreach (var tableId in removedTableObjectUuids.Keys)
            {
                if (!tableGetResultsOkay[tableId]) continue;
                var removedTableObjects = removedTableObjectUuids[tableId];

                foreach (var uuid in removedTableObjects)
                {
                    var obj = await Dav.Database.GetTableObjectAsync(uuid);
                    if (
                        obj == null
                        || obj.UploadStatus == TableObjectUploadStatus.New
                    ) continue;

                    await obj.DeleteImmediatelyAsync();

                    ProjectInterface.Callbacks.DeleteTableObject(obj.Uuid, obj.TableId);
                }
            }

            isSyncing = false;
            syncCompleted = true;

            // Check if the sync was successful for all tables
            foreach(var value in tableGetResultsOkay.Values)
                if (!value) return false;

            return true;
        }

        public static async Task<bool> SyncPush()
        {
            if (!Dav.IsLoggedIn) return false;
            if (!syncCompleted || isSyncing)
            {
                syncAgain = true;
                return false;
            }
            isSyncing = true;

            List<TableObject> tableObjects = await Dav.Database.GetAllTableObjectsAsync(true);
            List<TableObject> filteredTableObjects = tableObjects.Where(obj => obj.UploadStatus != TableObjectUploadStatus.UpToDate).Reverse().ToList();

            foreach (var tableObject in filteredTableObjects)
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
                !Dav.IsLoggedIn
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

        internal static void CloseWebsocketConnection()
        {
            if (!websocketConnectionEstablished) return;
            websocketConnection.Close();
            websocketConnectionEstablished = false;
        }

        private static void WebsocketConnection_OnOpened()
        {
            websocketConnectionEstablished = true;

            string json = JsonConvert.SerializeObject(new
            {
                command = "subscribe",
                identifier = "{\"channel\": \"" + Constants.tableObjectUpdateChannelName + "\"}"
            });

            websocketConnection.Send(json);
        }

        private static async void WebsocketConnection_OnMessage(string message)
        {
            dynamic json = JsonConvert.DeserializeObject(message);

            if (json.type == "ping" || json.message == null)
                return;
            else if(json.type == "reject_subscription")
            {
                CloseWebsocketConnection();
                return;
            }

            var uuid = (Guid)json.message.uuid;
            var change = (int)json.message.change;
            var accessTokenMd5 = (string)json.message.access_token_md5;
            if (uuid == null || string.IsNullOrEmpty(accessTokenMd5)) return;

            // Don't notify the app if the session is the current session
            if (Utils.CreateMD5(Dav.AccessToken) == accessTokenMd5) return;

            if(change == 0 || change == 1)
            {
                // Get the table object from the server and update it locally
                var getTableObjectResponse = await TableObjectsController.GetTableObject(uuid);
                if (!getTableObjectResponse.Success) return;

                var tableObject = getTableObjectResponse.Data.TableObject;

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

                if (!await tableObject.DownloadFile())
                    continue;

                // Remove the download progress from the list
                fileDownloadProgressList.Remove(fileDownload.uuid);

                // Update the table object with the new etag
                if (fileDownload.etag != null)
                    await tableObject.SetEtagAsync(fileDownload.etag);

                ProjectInterface.Callbacks.UpdateTableObject(tableObject, true);
            }

            downloadingFiles = false;
        }

        internal static void ReportFileDownloadProgress(Guid uuid, int value)
        {
            // Get the list by the uuid
            List<IProgress<(Guid, int)>> progressList = new List<IProgress<(Guid, int)>>();
            if (!fileDownloadProgressList.TryGetValue(uuid, out progressList)) return;

            foreach (IProgress<(Guid, int)> progress in progressList)
                progress.Report((uuid, value));

            ProjectInterface.Callbacks.TableObjectDownloadProgress(uuid, value);
        }

        private static async Task<ApiResponse<TableObject>> CreateTableObjectOnServer(TableObject tableObject)
        {
            if (!Dav.IsLoggedIn) return new ApiResponse<TableObject> { Success = false };

            if (tableObject.IsFile)
            {
                // Create the table object
                var createTableObjectResponse = await TableObjectsController.CreateTableObject(
                    tableObject.Uuid,
                    tableObject.TableId,
                    true,
                    new Dictionary<string, string> { { Constants.extPropertyName, tableObject.GetPropertyValue(Constants.extPropertyName) } }
                );

                if(!createTableObjectResponse.Success)
                {
                    if (createTableObjectResponse.Errors == null)
                        return new ApiResponse<TableObject> { Success = false };

                    // Check if the table object already exists
                    var errorResponse = createTableObjectResponse.Errors;
                    int i = errorResponse.ToList().FindIndex(error => error.Code == ErrorCodes.UuidAlreadyInUse);

                    if (i == -1)
                    {
                        return new ApiResponse<TableObject>
                        {
                            Success = false,
                            Status = createTableObjectResponse.Status,
                            Errors = createTableObjectResponse.Errors
                        };
                    }
                }

                if(tableObject.File != null)
                {
                    // Upload the file
                    string mimeType = "audio/mpeg";

                    try
                    {
                        mimeType = MimeTypeMap.GetMimeType(tableObject.GetPropertyValue(Constants.extPropertyName));
                    } catch(Exception) { }

                    var setTableObjectFileResponse = await TableObjectsController.SetTableObjectFile(
                        tableObject.Uuid,
                        tableObject.File.FullName,
                        mimeType
                    );

                    if (setTableObjectFileResponse.Success)
                    {
                        // Save the new table etag
                        SettingsManager.SetTableEtag(tableObject.TableId, setTableObjectFileResponse.Data.TableEtag);
                    }

                    return new ApiResponse<TableObject>
                    {
                        Success = setTableObjectFileResponse.Success,
                        Status = setTableObjectFileResponse.Status,
                        Errors = setTableObjectFileResponse.Errors,
                        Data = setTableObjectFileResponse.Data?.TableObject
                    };
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

                if (createTableObjectResponse.Success)
                {
                    // Save the new table etag
                    SettingsManager.SetTableEtag(tableObject.TableId, createTableObjectResponse.Data.TableEtag);
                }

                return new ApiResponse<TableObject>
                {
                    Success = createTableObjectResponse.Success,
                    Status = createTableObjectResponse.Status,
                    Errors = createTableObjectResponse.Errors,
                    Data = createTableObjectResponse.Data?.TableObject
                };
            }

            return new ApiResponse<TableObject> { Success = false };
        }

        private static async Task<ApiResponse<TableObject>> UpdateTableObjectOnServer(TableObject tableObject)
        {
            if (!Dav.IsLoggedIn) return new ApiResponse<TableObject> { Success = false };

            if (tableObject.IsFile && tableObject.File != null)
            {
                // Upload the file
                string mimeType = "audio/mpeg";
                try
                {
                    mimeType = MimeTypeMap.GetMimeType(tableObject.GetPropertyValue(Constants.extPropertyName));
                } catch (Exception) { }

                var setTableObjectFileResponse = await TableObjectsController.SetTableObjectFile(
                    tableObject.Uuid,
                    tableObject.File.FullName,
                    mimeType
                );

                if (!setTableObjectFileResponse.Success)
                {
                    return new ApiResponse<TableObject>
                    {
                        Success = false,
                        Status = setTableObjectFileResponse.Status,
                        Errors = setTableObjectFileResponse.Errors
                    };
                }
                
                // Check if the ext has changed
                var tableObjectResponseData = setTableObjectFileResponse.Data;
                string tableObjectResponseDataExt = tableObjectResponseData.TableObject.GetPropertyValue(Constants.extPropertyName);
                string tableObjectExt = tableObject.GetPropertyValue(Constants.extPropertyName);

                if(tableObjectResponseDataExt != tableObjectExt)
                {
                    // Update the table object with the new ext
                    var updateTableObjectResponse = await TableObjectsController.UpdateTableObject(
                        tableObject.Uuid,
                        new Dictionary<string, string> { { Constants.extPropertyName, tableObjectExt } }
                    );

                    if (updateTableObjectResponse.Success)
                    {
                        // Save the new table etag
                        SettingsManager.SetTableEtag(tableObject.TableId, updateTableObjectResponse.Data.TableEtag);
                    }

                    return new ApiResponse<TableObject>
                    {
                        Success = updateTableObjectResponse.Success,
                        Status = updateTableObjectResponse.Status,
                        Errors = updateTableObjectResponse.Errors,
                        Data = updateTableObjectResponse.Data?.TableObject
                    };
                }

                if (setTableObjectFileResponse.Success)
                {
                    // Save the new table etag
                    SettingsManager.SetTableEtag(tableObject.TableId, setTableObjectFileResponse.Data.TableEtag);
                }

                return new ApiResponse<TableObject>
                {
                    Success = setTableObjectFileResponse.Success,
                    Status = setTableObjectFileResponse.Status,
                    Errors = setTableObjectFileResponse.Errors,
                    Data = setTableObjectFileResponse.Data?.TableObject
                };
            }
            else if(!tableObject.IsFile)
            {
                // Update the table object
                var updateTableObjectResponse = await TableObjectsController.UpdateTableObject(
                    tableObject.Uuid,
                    Utils.ConvertPropertiesListToDictionary(tableObject.Properties)
                );

                if (updateTableObjectResponse.Success)
                {
                    // Save the new table etag
                    SettingsManager.SetTableEtag(tableObject.TableId, updateTableObjectResponse.Data.TableEtag);
                }

                return new ApiResponse<TableObject>
                {
                    Success = updateTableObjectResponse.Success,
                    Status = updateTableObjectResponse.Status,
                    Errors = updateTableObjectResponse.Errors,
                    Data = updateTableObjectResponse.Data?.TableObject
                };
            }

            return new ApiResponse<TableObject> { Success = false };
        }

        private static async Task<ApiResponse> DeleteTableObjectOnServer(TableObject tableObject)
        {
            if (!Dav.IsLoggedIn) return new ApiResponse { Success = false };
            return await TableObjectsController.DeleteTableObject(tableObject.Uuid);
        }

        internal static void SetDownloadingFileUuid(Guid uuid)
        {
            downloadingFileUuid = uuid;
        }
    }
}
