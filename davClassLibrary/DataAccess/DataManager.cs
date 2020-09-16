using davClassLibrary.Common;
using davClassLibrary.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Websockets;
using static davClassLibrary.Models.TableObject;

namespace davClassLibrary.DataAccess
{
    public static class DataManager
    {
        private static bool isSyncing = false;
        internal static List<TableObject> fileDownloads = new List<TableObject>();
        internal static Dictionary<Guid, WebClient> fileDownloaders = new Dictionary<Guid, WebClient>();
        internal static Dictionary<Guid, List<IProgress<(Guid, int)>>> fileDownloadProgressList = new Dictionary<Guid, List<IProgress<(Guid, int)>>>();
        private static Timer fileDownloadTimer;
        private static bool syncAgain = false;
        private const int maxFileDownloads = 1;
        private const string extPropertyName = "ext";
        private static IWebSocketConnection webSocketConnection;

        public static async Task Sync()
        {
            if (isSyncing) return;

            string jwt = DavUser.GetJWT();
            if (string.IsNullOrEmpty(jwt)) return;
            isSyncing = true;

            // Holds the table ids, e.g. 1, 2, 3, 4
            var tableIds = ProjectInterface.RetrieveConstants.GetTableIds();
            // Holds the parallel table ids, e.g. 2, 3
            var parallelTableIds = ProjectInterface.RetrieveConstants.GetParallelTableIds();
            // Holds the order of the table ids, sorted by the pages and the parallel table ids, e.g. 1, 2, 3, 2, 3, 4
            var sortedTableIds = new List<int>();
            // Holds the pages of the table; in the format <tableId, pages>
            var tablePages = new Dictionary<int, int>();
            // Holds the last downloaded page; in the format <tableId, pages>
            var currentTablePages = new Dictionary<int, int>();
            // Holds the latest table result; in the format <tableId, tableData>
            var tableResults = new Dictionary<int, TableData>();
            // Holds the uuids of the table objects that were removed on the server but not locally; in the format <tableId, List<Guid>>
            var removedTableObjectUuids = new Dictionary<int, List<Guid>>();
            // Is true if all http calls of the specified table are successful; in the format <tableId, bool>
            var tableGetResultsOkay = new Dictionary<int, bool>();

            if (tableIds == null || parallelTableIds == null)
                return;

            // Populate removedTableObjectUuids
            foreach (var tableId in tableIds)
            {
                removedTableObjectUuids[tableId] = new List<Guid>();

                foreach(var tableObject in await Dav.Database.GetAllTableObjectsAsync(tableId, true))
                    removedTableObjectUuids[tableId].Add(tableObject.Uuid);
            }

            // Get the first page of each table and generate the sorted tableIds list
            foreach(var tableId in tableIds)
            {
                // Get the first page of the table
                var tableGetResult = await HttpGetAsync(jwt, string.Format("apps/table/{0}?page=1", tableId));

                tableGetResultsOkay[tableId] = tableGetResult.Success;
                if (!tableGetResult.Success) continue;

                // Save the result
                var table = JsonConvert.DeserializeObject<TableData>(tableGetResult.Data);
                tableResults[tableId] = table;
                tablePages[tableId] = tableResults[tableId].pages;
                currentTablePages[tableId] = 1;
            }

            sortedTableIds = SortTableIds(tableIds, parallelTableIds, tablePages);

            // Process the table results
            foreach (var tableId in sortedTableIds)
            {
                var tableObjects = tableResults[tableId].table_objects;
                bool tableChanged = false;

                if (!tableGetResultsOkay[tableId]) continue;

                // Get the objects of the table
                foreach (var obj in tableObjects)
                {
                    removedTableObjectUuids[tableId].Remove(obj.uuid);

                    /*
                     *  Ist obj lokal gespeichert?
                     *      ja: Stimmt Etag überein?
                     *          ja: Ist es eine Datei?
                     *              ja: Ist die Datei heruntergeladen?
                     *                  ja: continue!
                     *                  nein: Herunterladen!
                     *              nein: continue!
                     *          nein: GET table object! Ist es eine Datei?
                     *              ja: Herunterladen!
                     *              nein: Save Table object!
                     *      nein: GET tableObject! Ist es eine Datei?
                     *          ja: Herunterladen!
                     *          nein: Save Table object!
                     * 
                     * (Bei Herunterladen: Etag und etag der Datei erst speichern, wenn die Datei heruntergeladen wurde)
                     * 
                    */

                    // Is obj in the database?
                    var currentTableObject = await Dav.Database.GetTableObjectAsync(obj.uuid);
                    if (currentTableObject != null)
                    {
                        // Is the etag correct?
                        if (Equals(obj.etag, currentTableObject.Etag))
                        {
                            // Is it a file?
                            if (currentTableObject.IsFile)
                            {
                                // Was the file downloaded?
                                if (!currentTableObject.FileDownloaded())
                                {
                                    // Download the file
                                    fileDownloads.Add(currentTableObject);
                                }
                            }
                        }
                        else
                        {
                            // GET the table object
                            var tableObject = await DownloadTableObjectAsync(currentTableObject.Uuid);

                            if (tableObject == null) continue;
                            tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;

                            // Is it a file?
                            if (tableObject.IsFile)
                            {
                                // Remove all properties except ext
                                var removingProperties = new List<Property>();
                                foreach (var p in tableObject.Properties)
                                    if (p.Name != extPropertyName) removingProperties.Add(p);

                                foreach (var property in removingProperties)
                                    tableObject.Properties.Remove(property);

                                // Save the ext property
                                await tableObject.SaveWithPropertiesAsync();

                                // Download the file
                                fileDownloads.Add(tableObject);
                            }
                            else
                            {
                                // Save the table object
                                await tableObject.SaveWithPropertiesAsync();
                                ProjectInterface.TriggerAction.UpdateTableObject(tableObject, false);
                                tableChanged = true;
                            }
                        }
                    }
                    else
                    {
                        // GET the table object
                        var tableObject = await DownloadTableObjectAsync(obj.uuid);
                        if (tableObject == null) continue;

                        // Is it a file?
                        if (tableObject.IsFile)
                        {
                            string etag = tableObject.Etag;

                            // Remove all properties except ext
                            var removingProperties = new List<Property>();
                            foreach (var p in tableObject.Properties)
                                if (p.Name != extPropertyName) removingProperties.Add(p);

                            foreach (var property in removingProperties)
                                tableObject.Properties.Remove(property);

                            // Save the table object without properties and etag (the etag will be saved later when the file was downloaded)
                            tableObject.Etag = "";
                            await tableObject.SaveWithPropertiesAsync();
                            await tableObject.SetUploadStatusAsync(TableObjectUploadStatus.UpToDate);

                            // Download the file
                            tableObject.Etag = etag;
                            fileDownloads.Add(tableObject);

                            ProjectInterface.TriggerAction.UpdateTableObject(tableObject, false);
                            tableChanged = true;
                        }
                        else
                        {
                            // Save the table object
                            tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                            await tableObject.SaveWithPropertiesAsync();
                            ProjectInterface.TriggerAction.UpdateTableObject(tableObject, false);
                            tableChanged = true;
                        }
                    }
                }

                if (tableChanged)
                    ProjectInterface.TriggerAction.UpdateAllOfTable(tableId);

                // Check if there is a next page
                currentTablePages[tableId]++;
                if (currentTablePages[tableId] > tablePages[tableId])
                    continue;

                // Get the data of the next page
                var tableGetResult = await HttpGetAsync(jwt, string.Format("apps/table/{0}?page={1}", tableId, currentTablePages[tableId]));
                if (!tableGetResult.Success)
                {
                    tableGetResultsOkay[tableId] = false;
                    continue;
                }
                
                tableResults[tableId] = JsonConvert.DeserializeObject<TableData>(tableGetResult.Data);
            }
            
            // RemovedTableObjects now includes all objects that were deleted on the server but not locally
            // Delete those objects locally
            foreach(var tableId in tableIds)
            {
                if (!tableGetResultsOkay[tableId]) continue;
                var removedTableObjects = removedTableObjectUuids[tableId];
                var tableChanged = false;

                foreach (var objUuid in removedTableObjects)
                {
                    var obj = await Dav.Database.GetTableObjectAsync(objUuid);
                    if (obj == null) continue;

                    if (obj.UploadStatus == TableObjectUploadStatus.New && obj.IsFile)
                    {
                        if (obj.FileDownloaded())
                            continue;
                    }
                    else if (obj.UploadStatus == TableObjectUploadStatus.New ||
                            obj.UploadStatus == TableObjectUploadStatus.NoUpload ||
                            obj.UploadStatus == TableObjectUploadStatus.Deleted)
                        continue;

                    await obj.DeleteImmediatelyAsync();
                    ProjectInterface.TriggerAction.DeleteTableObject(obj);
                    tableChanged = true;
                }

                if (tableChanged)
                    ProjectInterface.TriggerAction.UpdateAllOfTable(tableId);
            }

            isSyncing = false;

            // Push changes
            await SyncPush();
            StartFileDownloads();

            // Check if all tables were synced
            bool allTableGetResultsOkay = true;
            foreach (var tableGetResult in tableGetResultsOkay)
            {
                if (!tableGetResult.Value)
                {
                    allTableGetResultsOkay = false;
                    break;
                }
            }

            if (allTableGetResultsOkay)
            {
                ProjectInterface.TriggerAction.SyncFinished();
                EstablishWebsocketConnection();
            }
        }

        public static async Task SyncPush()
        {
            if (isSyncing)
            {
                syncAgain = true;
                return;
            }

            string jwt = DavUser.GetJWT();
            if (string.IsNullOrEmpty(jwt)) return;

            isSyncing = true;

            List<TableObject> tableObjects = (await Dav.Database.GetAllTableObjectsAsync(true))
                                            .Where(obj => obj.UploadStatus != TableObjectUploadStatus.NoUpload &&
                                                    obj.UploadStatus != TableObjectUploadStatus.UpToDate)
                                                    .OrderByDescending(obj => obj.Id).ToList();
            foreach (var tableObject in tableObjects)
            {
                if (tableObject.UploadStatus == TableObjectUploadStatus.New)
                {
                    // Check if the tableObject is a file and if it can be uploaded
                    if (tableObject.IsFile && tableObject.File != null)
                    {
                        var usedStorage = DavUser.GetUsedStorage();
                        var totalStorage = DavUser.GetTotalStorage();
                        var fileSize = tableObject.File.Length;

                        if (usedStorage + fileSize > totalStorage && totalStorage != 0)
                            continue;
                    }

                    // Create the new object on the server
                    var etag = await tableObject.CreateOnServerAsync();
                    if (!string.IsNullOrEmpty(etag))
                    {
                        tableObject.Etag = etag;
                        tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                        await tableObject.SaveAsync();
                    }
                }
                else if (tableObject.UploadStatus == TableObjectUploadStatus.Updated)
                {
                    // Update the object on the server
                    var etag = await tableObject.UpdateOnServerAsync();
                    if (!string.IsNullOrEmpty(etag))
                    {
                        tableObject.Etag = etag;
                        tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                        await tableObject.SaveAsync();
                    }
                }
                else if (tableObject.UploadStatus == TableObjectUploadStatus.Deleted)
                {
                    // Delete the object on the server
                    if (await tableObject.DeleteOnServerAsync())
                        await Dav.Database.DeleteTableObjectAsync(tableObject.Uuid);
                }
            }

            isSyncing = false;

            if (syncAgain)
            {
                syncAgain = false;
                await SyncPush();
            }
        }

        internal static void EstablishWebsocketConnection()
        {
            if (Dav.Environment == DavEnvironment.Test) return;

            webSocketConnection = WebSocketFactory.Create();
            webSocketConnection.OnOpened += Connection_OnOpened;
            webSocketConnection.OnMessage += Connection_OnMessage;

            webSocketConnection.Open($"{Dav.ApiBaseUrl.Replace("http", "ws")}/cable?app_id={Dav.AppId}&jwt={DavUser.GetJWT()}");
        }

        internal static void CloseWebsocketConnection()
        {
            if(webSocketConnection != null)
                webSocketConnection.Close();
        }

        private static async void Connection_OnMessage(string message)
        {
            TableObjectUpdateResponse json = JsonConvert.DeserializeObject<TableObjectUpdateResponse>(message);
            
            if(json.Type == "reject_subscription")
            {
                // Close the connection
                webSocketConnection.Close();
                return;
            }else if(json.Type == "ping")
                return;

            JObject jsonMessage = json.Message as JObject;
            if (jsonMessage == null) return;
            if (json.Message.GetType() != typeof(JObject)) return;

            // Get the uuid
            if (jsonMessage.ContainsKey(Dav.uuidKey) && jsonMessage.ContainsKey(Dav.changeKey))
            {
                if (!Guid.TryParse(jsonMessage[Dav.uuidKey].ToString(), out Guid uuid)) return;
                int change = (int)jsonMessage[Dav.changeKey];
                int sessionId = (int)jsonMessage[Dav.sessionIdKey];

                // Don't notify the app if the session is the current session or 0
                if (sessionId == 0) return;

                string[] jwtSplit = DavUser.GetJWT().Split('.');
                if (jwtSplit.Length <= 3) return;

                string currentSessionIdString = jwtSplit[3];
                if(!int.TryParse(currentSessionIdString, out int currentSessionId) || currentSessionId == sessionId)
                    return;

                if (change == 0 || change == 1)
                {
                    // Get the new or updated table object
                    await UpdateLocalTableObjectAsync(uuid);
                }
                else if(change == 2)
                {
                    var tableObject = await Dav.Database.GetTableObjectAsync(uuid);
                    if (tableObject == null) return;

                    // Delete the table object
                    await Dav.Database.DeleteTableObjectAsync(uuid);
                    ProjectInterface.TriggerAction.DeleteTableObject(tableObject);
                }
            }
        }

        private static void Connection_OnOpened()
        {
            string channelName = "TableObjectUpdateChannel";

            // Subscribe to the channel
            string json = JsonConvert.SerializeObject(new
            {
                command = "subscribe",
                identifier = "{\"channel\": \"" + channelName + "\"}"
            });
            webSocketConnection.Send(json);
        }

        public static List<int> SortTableIds(List<int> tableIds, List<int> parallelTableIds, Dictionary<int, int> tableIdPages)
        {
            List<int> preparedTableIds = new List<int>();

            // Remove all table ids in parallelTableIds that do not occur in tableIds
            List<int> removeParallelTableIds = new List<int>();
            for (int i = 0; i < parallelTableIds.Count; i++)
            {
                var value = parallelTableIds[i];
                if (!tableIds.Contains(value))
                    removeParallelTableIds.Add(value);
            }
            parallelTableIds.RemoveAll((int t) => { return removeParallelTableIds.Contains(t); });

            // Prepare pagesOfParallelTable
            var pagesOfParallelTable = new Dictionary<int, int>();
            foreach (var table in tableIdPages)
            {
                if (parallelTableIds.Contains(table.Key))
                    pagesOfParallelTable[table.Key] = table.Value;
            }

            // Count the pages
            int pagesSum = 0;
            foreach (var table in tableIdPages)
            {
                pagesSum += table.Value;

                if (parallelTableIds.Contains(table.Key))
                    pagesOfParallelTable[table.Key] = table.Value - 1;
            }

            int index = 0;
            int currentTableIdIndex = 0;
            bool parallelTableIdsInserted = false;

            while (index < pagesSum)
            {
                int currentTableId = tableIds[currentTableIdIndex];
                int currentTablePages = tableIdPages[currentTableId];

                if (parallelTableIds.Contains(currentTableId))
                {
                    // Add the table id once as it belongs to parallel table ids
                    preparedTableIds.Add(currentTableId);
                    index++;
                }
                else
                {
                    // Add it for all pages
                    for (var j = 0; j < currentTablePages; j++)
                    {
                        preparedTableIds.Add(currentTableId);
                        index++;
                    }
                }

                // Check if all parallel table ids are in prepared table ids
                bool hasAll = true;
                foreach (var tableId in parallelTableIds)
                    if (!preparedTableIds.Contains(tableId))
                        hasAll = false;

                if (hasAll && !parallelTableIdsInserted)
                {
                    parallelTableIdsInserted = true;
                    int pagesOfParallelTableSum = 0;

                    // Update pagesOfParallelTableSum
                    foreach (var table in pagesOfParallelTable)
                        pagesOfParallelTableSum += table.Value;

                    // Append the parallel table ids in the right order
                    while (pagesOfParallelTableSum > 0)
                    {
                        foreach (var parallelTableId in parallelTableIds)
                        {
                            if (pagesOfParallelTable[parallelTableId] > 0)
                            {
                                preparedTableIds.Add(parallelTableId);
                                pagesOfParallelTableSum--;
                                pagesOfParallelTable[parallelTableId]--;

                                index++;
                            }
                        }
                    }
                }

                currentTableIdIndex++;
            }

            return preparedTableIds;
        }

        private static async Task UpdateLocalTableObjectAsync(Guid uuid)
        {
            // Get the table object from the server and update it locally
            var tableObject = await DownloadTableObjectAsync(uuid);
            if (tableObject == null) return;

            if (tableObject.IsFile)
            {
                // Remove all properties except ext
                var removingProperties = new List<Property>();
                foreach (var p in tableObject.Properties)
                    if (p.Name != extPropertyName) removingProperties.Add(p);

                foreach (var property in removingProperties)
                    tableObject.Properties.Remove(property);

                // Save the ext property
                await tableObject.SaveWithPropertiesAsync();

                // Download the file
                tableObject.DownloadFile(null);
            }
            else
            {
                // Save the table object
                tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                await tableObject.SaveWithPropertiesAsync();
                ProjectInterface.TriggerAction.UpdateTableObject(tableObject, false);
            }
        }

        internal static async Task SetEtagOfTableObjectAsync(Guid uuid, string etag)
        {
            // Get the table object
            var tableObject = await Dav.Database.GetTableObjectAsync(uuid);
            if (tableObject != null)
            {
                tableObject.Etag = etag;
                await tableObject.SaveAsync();
            }
        }

        private static async Task<TableObject> DownloadTableObjectAsync(Guid uuid)
        {
            var getResult = await HttpGetAsync(DavUser.GetJWT(), $"apps/object/{uuid}");
            if (getResult.Success)
            {
                var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(getResult.Data);
                tableObjectData.id = 0;
                return ConvertTableObjectDataToTableObject(tableObjectData);
            }
            else
            {
                HandleErrorCodes(getResult.Data);
                return null;
            }
        }

        private static void HandleErrorCodes(string errorMessage)
        {
            if (errorMessage.Contains("1301") || errorMessage.Contains("1302") || errorMessage.Contains("1303"))
            {
                // JWT is invalid or has expired. Log out the user
                DavUser.SetJWT(null);
            }
        }

        private static void StartFileDownloads()
        {
            // Do not download more than downloadFilesSimultaneously files at the same time
            fileDownloadTimer = new Timer();
            fileDownloadTimer.Elapsed += DownloadFileTimer_Elapsed;
            fileDownloadTimer.Interval = 5000;
            fileDownloadTimer.Start();
        }

        private static void DownloadFileTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Check the network connection
            if (!ProjectInterface.GeneralMethods.IsNetworkAvailable()) return;

            // Check if fileDownloads list is greater than downloadFilesSimultaneously
            if (
                fileDownloaders.Count < maxFileDownloads &&
                fileDownloads.Count > 0
            )
            {
                // Get a file that is still not being downloaded
                if (fileDownloads.First().FileDownloadStatus == TableObjectFileDownloadStatus.NotDownloaded)
                    fileDownloads.First().DownloadFile(null);
            }
            else if (fileDownloads.Count == 0)
            {
                // Stop the timer
                fileDownloadTimer.Stop();
            }
        }

        internal static void ReportFileDownloadProgress(Guid uuid, int value)
        {
            // Get the list by the uuid
            List<IProgress<(Guid, int)>> progressList = new List<IProgress<(Guid, int)>>();
            if (!fileDownloadProgressList.TryGetValue(uuid, out progressList)) return;

            foreach(IProgress<(Guid, int)> progress in progressList)
                progress.Report((uuid, value));
        }

        public static async Task<ApiResponse> HttpGetAsync(string jwt, string url)
        {
            if (ProjectInterface.GeneralMethods.IsNetworkAvailable())
            {
                HttpClient httpClient = new HttpClient();
                var headers = httpClient.DefaultRequestHeaders;
                headers.Authorization = new AuthenticationHeaderValue(jwt);
                Uri requestUri = new Uri($"{Dav.ApiBaseUrl}/{url}");

                HttpResponseMessage httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";

                // Send the GET request
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                return new ApiResponse {
                    Success = httpResponse.IsSuccessStatusCode,
                    Status = (int)httpResponse.StatusCode,
                    Data = httpResponseBody
                };
            }
            else
            {
                // Return error message
                //return new KeyValuePair<bool, string>(false, "No internet connection");
                return new ApiResponse {
                    Success = false,
                    Status = 0,
                    Data = null
                };
            }
        }

        internal static DirectoryInfo GetTableFolder(int tableId)
        {
            string tableFolderPath = Path.Combine(Dav.DataPath, tableId.ToString());
            return Directory.CreateDirectory(tableFolderPath);
        }

        internal static DirectoryInfo GetTempTableFolder(int tableId)
        {
            string tableFolderPath = Path.Combine(Path.GetTempPath(), "dav", tableId.ToString());
            return Directory.CreateDirectory(tableFolderPath);
        }

        public static async Task ExportDataAsync(DirectoryInfo exportFolder, IProgress<int> progress)
        {
            // 1. foreach all table object
            // 1.1 Create a folder for every table in the export folder
            // 1.2 Write the list as json to a file in the export folder
            // 2. If the tableObject is a file, copy the file into the appropriate folder
            // 3. Zip the export folder and copy it to the destination
            // 4. Delete the export folder and the zip file in the local storage

            await Task.Run(async () =>
            {
                List<TableObjectData> tableObjectDataList = new List<TableObjectData>();
                var tableObjects = await Dav.Database.GetAllTableObjectsAsync(false);
                int i = 0;

                foreach (var tableObject in tableObjects)
                {
                    tableObjectDataList.Add(tableObject.ToTableObjectData());

                    if (tableObject.IsFile && File.Exists(tableObject.File.FullName))
                    {
                        string tablePath = Path.Combine(exportFolder.FullName, tableObject.TableId.ToString());
                        Directory.CreateDirectory(tablePath);

                        string tableObjectFilePath = Path.Combine(tablePath, tableObject.File.Name);
                        tableObject.File.CopyTo(tableObjectFilePath, true);
                    }

                    i++;
                    progress.Report((int)Math.Round(100.0 / tableObjects.Count * i));
                }

                // Write the list of tableObjects as json
                string dataFilePath = Path.Combine(exportFolder.FullName, Dav.ExportDataFileName);
                WriteFile(dataFilePath, tableObjectDataList);

                // Create a zip file of the export folder and copy it into the destination folder
                //string destinationFilePath = Path.Combine(exportFolder.Parent.FullName, "export.zip");
                //ZipFile.CreateFromDirectory(exportFolder.FullName, destinationFilePath);
                //return new FileInfo(destinationFilePath);

                /*
                 * Alternative approach with dotNetZip, which still does not support .NET Standard
                 * 
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using (ZipFile zip = new ZipFile())
                {
                    zip.AddDirectory(exportFolder.FullName);

                    foreach (var tableObject in tableObjects)
                    {
                        // Create a folder for the table
                        //Directory.CreateDirectory(Path.Combine(exportFolder.FullName, tableObject.TableId.ToString()));
                        string directoryName = tableObject.TableId.ToString();

                        if (!zip.ContainsEntry(directoryName))
                        {
                            string tablePath = Path.Combine(exportFolder.FullName, directoryName);
                            Directory.CreateDirectory(tablePath);
                            zip.AddDirectory(tablePath);
                        }

                        if (tableObject.IsFile)
                        {
                            //tableObject.File.CopyTo(Path.Combine(exportFolder.FullName, tableObject.TableId.ToString(), tableObject.File.Name));
                            zip.AddFile(tableObject.File.FullName, directoryName);
                        }

                        tableObjectDataList.Add(tableObject.ToTableObjectData());

                        progress.Report((int)Math.Round(100.0 / tableObjects.Count * i));
                        i++;
                    }

                    // Write the list of tableObjects as json
                    string dataFilePath = Path.Combine(exportFolder.FullName, "data.json");
                    WriteFile(dataFilePath, tableObjectDataList);

                    zip.AddFile(dataFilePath);
                    zip.Save(Path.Combine(exportFolder.FullName, fileName + ".zip"));
                }
                */
            });
        }

        public static async Task ImportDataAsync(DirectoryInfo importFolder, IProgress<int> progress)
        {
            string dataFilePath = Path.Combine(importFolder.FullName, Dav.ExportDataFileName);
            FileInfo dataFile = new FileInfo(dataFilePath);
            List<TableObjectData> tableObjects = GetDataFromFile(dataFile);
            int i = 0;

            foreach (var tableObjectData in tableObjects)
            {
                TableObject tableObject = ConvertTableObjectDataToTableObject(tableObjectData);
                tableObject.UploadStatus = TableObjectUploadStatus.New;

                if (!await Dav.Database.TableObjectExistsAsync(tableObject.Uuid))
                {
                    await Dav.Database.CreateTableObjectWithPropertiesAsync(tableObject);

                    // If the tableObject is a file, get the file from the appropriate folder
                    if (tableObject.IsFile)
                    {
                        try
                        {
                            string tablePath = Path.Combine(importFolder.FullName, tableObject.TableId.ToString());
                            string filePath = Path.Combine(tablePath, tableObject.Uuid.ToString());
                            FileInfo tableObjectFile = new FileInfo(filePath);
                            await tableObject.SetFileAsync(tableObjectFile);
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.Message);
                        }
                    }
                }

                i++;
                progress.Report((int)Math.Round(100.0 / tableObjects.Count * i));
            }
        }

        internal static void WriteFile(string path, object objectToWrite)
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(objectToWrite.GetType());
            MemoryStream ms = new MemoryStream();
            js.WriteObject(ms, objectToWrite);

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string data = sr.ReadToEnd();

            File.WriteAllText(path, data);
        }

        public static List<TableObjectData> GetDataFromFile(FileInfo dataFile)
        {
            string data = File.ReadAllText(dataFile.FullName);

            //Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(List<TableObjectData>));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (List<TableObjectData>)serializer.ReadObject(ms);

            return dataReader;
        }
        
        internal static Dictionary<string, string> ConvertPropertiesListToDictionary(List<Property> properties)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            foreach (var property in properties)
                dictionary.Add(property.Name, property.Value);

            return dictionary;
        }

        internal static byte[] FileToByteArray(string fileName)
        {
            if (!File.Exists(fileName)) return null;
            byte[] fileData = null;

            using (FileStream fs = File.OpenRead(fileName))
            {
                var binaryReader = new BinaryReader(fs);
                fileData = binaryReader.ReadBytes((int)fs.Length);
            }
            return fileData;
        }

        internal static async Task DeleteSessionOnServerAsync(string jwt)
        {
            if (!ProjectInterface.GeneralMethods.IsNetworkAvailable()) return;

            HttpClient httpClient = new HttpClient();
            var headers = httpClient.DefaultRequestHeaders;
            headers.Authorization = new AuthenticationHeaderValue(jwt);
            Uri requestUri = new Uri($"{Dav.ApiBaseUrl}/auth/session");

            await httpClient.DeleteAsync(requestUri);
        }
    }

    internal class TableObjectUpdateResponse
    {
        public string Type { get; set; }
        public object Message { get; set; }
    }

    public struct ApiResponse
    {
        public bool Success;
        public int Status;
        public string Data;
    }

    public enum DavEnvironment
    {
        Development,
        Test,
        Production
    }
}
