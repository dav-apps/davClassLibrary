using davClassLibrary.Common;
using davClassLibrary.Controllers;
using davClassLibrary.Models;
using MimeTypes;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;

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

        private static string retrieveTableQueryData = @"
            id
		    etag
		    tableObjects(
			    limit: $limit
			    offset: $offset
		    ) {
			    total
			    items {
				    uuid
			    }
		    }
        ";
        private static string retrieveTableObjectQueryData = @"
            uuid
		    table {
			    id
		    }
		    user {
			    id
			    email
			    firstName
		    }
		    fileUrl
		    etag
		    properties
        ";

        public static async Task SessionSyncPush()
        {
            var accessToken = SettingsManager.GetAccessToken();
            var sessionUploadStatus = SettingsManager.GetSessionUploadStatus();

            if (string.IsNullOrEmpty(accessToken) || sessionUploadStatus == SessionUploadStatus.UpToDate)
                return;

            // Delete the session on the server
            await SessionsController.DeleteSession("accessToken", accessToken);

            // Remove the session
            SettingsManager.RemoveSession();
        }

        public static void LoadUser()
        {
            Dav.User.Id = SettingsManager.GetId();
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
            var retrieveUserResponse = await UsersController.RetrieveUser($@"
                id
			    email
			    firstName
			    confirmed
			    totalStorage
			    usedStorage
			    stripeCustomerId
			    plan
			    subscriptionStatus
			    periodEnd
			    profileImage {{
				    url
				    etag
			    }}
			    apps {{
				    total
				    items {{
					    id
					    name
					    description
					    published
					    webLink
					    googlePlayLink
					    microsoftStoreLink
				    }}
			    }}
            ");

            if (retrieveUserResponse.Errors != null)
            {
                Dav.Logout();
                return false;
            }

            var userResponseData = retrieveUserResponse.Data;
            var plan = Plan.Free;

            if (userResponseData.plan == "PLUS")
                plan = Plan.Plus;
            else if (userResponseData.plan == "PRO")
                plan = Plan.Pro;

            // Update the values in the local settings
            if (Dav.User.Id != userResponseData.id) SettingsManager.SetId(userResponseData.id);
            if (Dav.User.Email != userResponseData.email) SettingsManager.SetEmail(userResponseData.email);
            if (Dav.User.FirstName != userResponseData.firstName) SettingsManager.SetFirstName(userResponseData.firstName);
            if (Dav.User.TotalStorage != userResponseData.totalStorage) SettingsManager.SetTotalStorage(userResponseData.totalStorage);
            if (Dav.User.UsedStorage != userResponseData.usedStorage) SettingsManager.SetUsedStorage(userResponseData.usedStorage);
            if (Dav.User.Plan != plan) SettingsManager.SetPlan(plan);

            if (
                !File.Exists(Path.Combine(Dav.DataPath, Constants.profileImageFileName))
                || Dav.User.ProfileImageEtag != userResponseData.profileImage.etag
            )
            {
                // Download the profile image
                if (await ApiManager.DownloadFile(userResponseData.profileImage.url, Path.Combine(Dav.DataPath, Constants.profileImageFileName)))
                    SettingsManager.SetProfileImageEtag(userResponseData.profileImage.etag);
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

            // Holds the table names, e.g. 1, 2, 3, 4
            var tableNames = Dav.TableNames;
            // Holds the parallel table names, e.g. 2, 3
            var parallelTableNames = Dav.ParallelTableNames;
            // Holds the order of the table names, sorted by the pages and the parallel table names, e.g. 1, 2, 3, 2, 3, 4
            var sortedTableNames = new List<string>();
            // Mapping for the table names to the table ids
            var tableIds = new Dictionary<string, int>();
            // Holds the pages of the table; in the format <tableName, pages>
            var tablePages = new Dictionary<string, int>();
            // Holds the last downloaded page; in the format <tableName, pages>
            var currentTablePages = new Dictionary<string, int>();
            // Holds the latest table result; in the format <tableName, tableData>
            var tableResults = new Dictionary<string, TableResource>();
            // Holds the uuids of the table objects that were removed on the server but not locally; in the format <tableName, List<Guid>>
            var removedTableObjectUuids = new Dictionary<string, List<Guid>>();
            // Is true if all http calls of the specified table are successful; in the format <tableName, bool>
            var getTableResultsOkay = new Dictionary<string, bool>();
            // Holds the new table etags for the tables
            var tableEtags = new Dictionary<string, string>();

            if (
                tableNames == null
                || tableNames.Count == 0
                || parallelTableNames == null
            ) return false;

            decimal tableObjectsLimit = 100;

            // Get the first page of each table
            foreach (var tableName in tableNames)
            {
                // Get the first page of the table
                var retrieveTableResponse = await TablesController.RetrieveTable(
                    retrieveTableQueryData,
                    tableName
                );

                getTableResultsOkay[tableName] = retrieveTableResponse.Success;
                if (!retrieveTableResponse.Success) continue;

                var table = retrieveTableResponse.Data;
                tableIds[tableName] = table.id;

                // Check if the table has any changes
                if (table.etag == SettingsManager.GetTableEtag(table.id))
                    continue;
                
                // Save the result
                tableResults[tableName] = table;
                tablePages[tableName] = (int)Math.Ceiling(table.tableObjects.total / tableObjectsLimit);
                currentTablePages[tableName] = 1;
                tableEtags[tableName] = table.etag;
            }

            sortedTableNames = Utils.SortTableNames(tableNames, parallelTableNames, tablePages);

            // Populate removedTableObjectUuids
            foreach (var tableName in sortedTableNames.Distinct())
            {
                removedTableObjectUuids[tableName] = new List<Guid>();

                foreach (var tableObject in await Dav.Database.GetAllTableObjectsAsync(tableIds[tableName], true))
                    removedTableObjectUuids[tableName].Add(tableObject.Uuid);
            }

            // Process the table results
            foreach (var tableName in sortedTableNames)
            {
                if (!getTableResultsOkay[tableName]) continue;

                var tableObjects = tableResults[tableName].tableObjects;
                bool tableChanged = false;
                bool saveEtag = true;

                foreach (var obj in tableObjects.items)
                {
                    // Remove the table objects from removedTableObjectUuids
                    removedTableObjectUuids[tableName].Remove(obj.uuid);

                    // Is the table object in the database?
                    var currentTableObject = await Dav.Database.GetTableObjectAsync(obj.uuid);

                    if (currentTableObject != null)
                    {
                        // Has the etag changed?
                        if (Equals(obj.etag, currentTableObject.Etag))
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
                        else if (currentTableObject.UploadStatus == TableObjectUploadStatus.UpToDate)
                        {
                            // Get the updated table object from the server
                            var retrieveTableObjectResponse = await TableObjectsController.RetrieveTableObject(
                                retrieveTableObjectQueryData,
                                currentTableObject.Uuid
                            );

                            if (retrieveTableObjectResponse.Errors != null) continue;

                            var tableObject = retrieveTableObjectResponse.Data.ToTableObject();
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
                                    etag = obj.etag
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
                        var retrieveTableObjectResponse = await TableObjectsController.RetrieveTableObject(
                            retrieveTableObjectQueryData,
                            obj.uuid
                        );

                        if (retrieveTableObjectResponse.Errors != null)
                        {
                            saveEtag = false;
                            continue;
                        }

                        var tableObject = retrieveTableObjectResponse.Data.ToTableObject();
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
                currentTablePages[tableName]++;

                if (currentTablePages[tableName] > tablePages[tableName])
                {
                    ProjectInterface.Callbacks.UpdateAllOfTable(tableIds[tableName], tableChanged, true);

                    // Save the new table etag, if all table objects were saved
                    if (saveEtag) SettingsManager.SetTableEtag(tableIds[tableName], tableEtags[tableName]);

                    continue;
                }

                ProjectInterface.Callbacks.UpdateAllOfTable(tableIds[tableName], tableChanged, false);

                // Get the next page
                var retrieveTableResult = await TablesController.RetrieveTable(
                    retrieveTableQueryData,
                    tableName,
                    (int)tableObjectsLimit,
                    (currentTablePages[tableName] - 1) * (int)tableObjectsLimit
                );

                if (retrieveTableResult.Errors != null)
                {
                    getTableResultsOkay[tableName] = false;
                    continue;
                }

                tableResults[tableName] = retrieveTableResult.Data;
            }

            // RemovedTableObjects now includes all table objects that were deleted on the server but not locally
            // Delete these table objects locally
            foreach (var tableName in removedTableObjectUuids.Keys)
            {
                if (!getTableResultsOkay[tableName]) continue;
                var removedTableObjects = removedTableObjectUuids[tableName];

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
            foreach (var value in getTableResultsOkay.Values)
                if (!value) return false;

            return true;
        }

        public static async Task<bool> SyncPush()
        {
            if (!Dav.IsLoggedIn) return false;
            if (isSyncing || (!syncCompleted && Dav.Environment != Environment.Test))
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
                        if (tableObject.IsFile && tableObject.File != null && tableObject.File.Exists)
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
                            if (tableObject.Properties.Count > Constants.maxPropertiesUploadCount)
                            {
                                tableObject.UploadStatus = TableObjectUploadStatus.Updated;
                                syncAgain = true;
                            }
                            else
                                tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;

                            tableObject.Etag = createResult.Data.Etag;
                            await tableObject.SaveAsync();
                        }
                        else if (createResult.Errors != null)
                        {
                            // Check the errors
                            var errors = createResult.Errors;

                            // Check if the table object already exists
                            if (createResult.Errors.Contains(ErrorCodes.UuidAlreadyInUse))
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
                            if (updateResult.Errors.Contains(ErrorCodes.TableObjectDoesNotExist))
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
                        else if (
                            deleteResult.Errors.Contains(ErrorCodes.ActionNotAllowed)
                            || deleteResult.Errors.Contains(ErrorCodes.TableObjectDoesNotExist)
                        )
                        {
                            // Delete the table object
                            await tableObject.DeleteImmediatelyAsync();
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

        private static async Task<GraphQLApiResponse<TableObject>> CreateTableObjectOnServer(TableObject tableObject)
        {
            if (!Dav.IsLoggedIn) return new GraphQLApiResponse<TableObject> { Success = false };

            if (tableObject.IsFile)
            {
                // Create the table object
                var createTableObjectResponse = await TableObjectsController.CreateTableObject(
                    "uuid",
                    tableObject.Uuid,
                    tableObject.TableId,
                    true,
                    tableObject.GetPropertyValue(Constants.extPropertyName),
                    null
                );

                // Check if the table object already exists
                if (
                    !createTableObjectResponse.Success
                    && createTableObjectResponse.Errors != null
                    && !createTableObjectResponse.Errors.Contains(ErrorCodes.UuidAlreadyInUse)
                )
                {
                    return new GraphQLApiResponse<TableObject>
                    {
                        Success = false,
                        Errors = createTableObjectResponse.Errors
                    };
                }

                if (tableObject.File != null && tableObject.File.Exists)
                {
                    // Upload the file
                    string mimeType = "audio/mpeg";

                    try
                    {
                        mimeType = MimeTypeMap.GetMimeType(tableObject.GetPropertyValue(Constants.extPropertyName));
                    } catch(Exception) { }

                    var uploadTableObjectFileResponse = await TableObjectsController.UploadTableObjectFile(
                        tableObject.Uuid,
                        mimeType,
                        tableObject.File.FullName
                    );

                    if (uploadTableObjectFileResponse.Success)
                    {
                        // Save the new table etag
                        SettingsManager.SetTableEtag(tableObject.TableId, uploadTableObjectFileResponse.Data.table.etag);
                    }

                    return new GraphQLApiResponse<TableObject>
                    {
                        Success = uploadTableObjectFileResponse.Success,
                        Errors = uploadTableObjectFileResponse.Error?.Code != null ? new List<string> { uploadTableObjectFileResponse.Error.Code } : null,
                        Data = new TableObject { Etag = uploadTableObjectFileResponse.Data.etag }
                    };
                }
            }
            else
            {
                // Create the table object
                var createTableObjectResponse = await TableObjectsController.CreateTableObject(
                    $@"
                        etag
                        table {{
                            etag
                        }}
                    ",
                    tableObject.Uuid,
                    tableObject.TableId,
                    false,
                    null,
                    Utils.ConvertPropertiesListToDictionary(tableObject.Properties)
                );

                if (createTableObjectResponse.Errors == null)
                {
                    // Save the new table etag
                    SettingsManager.SetTableEtag(tableObject.TableId, createTableObjectResponse.Data?.table.etag);
                }

                return new GraphQLApiResponse<TableObject>
                {
                    Success = createTableObjectResponse.Success,
                    Errors = createTableObjectResponse.Errors,
                    Data = createTableObjectResponse.Data?.ToTableObject()
                };
            }

            return new GraphQLApiResponse<TableObject> { Success = false };
        }

        private static async Task<GraphQLApiResponse<TableObject>> UpdateTableObjectOnServer(TableObject tableObject)
        {
            if (!Dav.IsLoggedIn) return new GraphQLApiResponse<TableObject> { Success = false };

            if (tableObject.IsFile && tableObject.File != null)
            {
                // Upload the file
                string mimeType = "audio/mpeg";

                try
                {
                    mimeType = MimeTypeMap.GetMimeType(tableObject.GetPropertyValue(Constants.extPropertyName));
                } catch (Exception) { }

                var uploadTableObjectFileResponse = await TableObjectsController.UploadTableObjectFile(
                    tableObject.Uuid,
                    tableObject.File.FullName,
                    mimeType
                );

                if (!uploadTableObjectFileResponse.Success)
                {
                    var result = new GraphQLApiResponse<TableObject> { Success = false };

                    if (uploadTableObjectFileResponse.Error.Code != null)
                        result.Errors = new List<string> { uploadTableObjectFileResponse.Error.Code };

                    return result;
                }

                // Check if the ext has changed
                var tableObjectResponseData = uploadTableObjectFileResponse.Data;
                var properties = tableObjectResponseData.properties.ToList();
                var i = properties.FindIndex(p => p.Key == Constants.extPropertyName);
                string tableObjectResponseDataExt = i != -1 ? (string)properties[i].Value : null;
                string tableObjectExt = tableObject.GetPropertyValue(Constants.extPropertyName);

                // Save the new table etag
                SettingsManager.SetTableEtag(tableObject.TableId, uploadTableObjectFileResponse.Data.table.etag);

                if (tableObjectResponseDataExt != tableObjectExt)
                {
                    // Update the table object with the new ext
                    var updateTableObjectResponse = await TableObjectsController.UpdateTableObject(
                        $@"
                            etag
                            table {{
                                etag
                            }}
                        ",
                        tableObject.Uuid,
                        tableObjectExt,
                        null
                    );

                    if (updateTableObjectResponse.Success)
                    {
                        // Save the new table etag
                        SettingsManager.SetTableEtag(tableObject.TableId, updateTableObjectResponse.Data.table.etag);
                    }

                    return new GraphQLApiResponse<TableObject>
                    {
                        Success = updateTableObjectResponse.Success,
                        Errors = updateTableObjectResponse.Errors,
                        Data = updateTableObjectResponse.Data?.ToTableObject()
                    };
                }

                return new GraphQLApiResponse<TableObject>
                {
                    Success = uploadTableObjectFileResponse.Success,
                    Errors = uploadTableObjectFileResponse.Error?.Code != null ? new List<string> { uploadTableObjectFileResponse.Error.Code } : null,
                    Data = new TableObject { Etag = uploadTableObjectFileResponse.Data.etag }
                };
            }
            else if (!tableObject.IsFile)
            {
                // Update the table object
                var updateTableObjectResponse = await TableObjectsController.UpdateTableObject(
                    $@"
                        etag
                        table {{
                            etag
                        }}
                    ",
                    tableObject.Uuid,
                    null,
                    Utils.ConvertPropertiesListToDictionary(tableObject.Properties)
                );

                if (updateTableObjectResponse.Success)
                {
                    // Save the new table etag
                    SettingsManager.SetTableEtag(tableObject.TableId, updateTableObjectResponse.Data.table.etag);
                }

                return new GraphQLApiResponse<TableObject>
                {
                    Success = updateTableObjectResponse.Success,
                    Errors = updateTableObjectResponse.Errors,
                    Data = updateTableObjectResponse.Data?.ToTableObject()
                };
            }

            return new GraphQLApiResponse<TableObject> { Success = false };
        }

        private static async Task<GraphQLApiResponse> DeleteTableObjectOnServer(TableObject tableObject)
        {
            if (!Dav.IsLoggedIn) return new GraphQLApiResponse { Success = false };
            var deleteTableObjectResponse = await TableObjectsController.DeleteTableObject("uuid", tableObject.Uuid);

            if (deleteTableObjectResponse.Success)
                return new GraphQLApiResponse { Success = true };
            else
            {
                return new GraphQLApiResponse
                {
                    Success = false,
                    Errors = deleteTableObjectResponse.Errors
                };
            }
        }

        internal static void SetDownloadingFileUuid(Guid uuid)
        {
            downloadingFileUuid = uuid;
        }
    }
}
