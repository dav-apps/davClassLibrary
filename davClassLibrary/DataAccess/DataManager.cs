using davClassLibrary.Common;
using davClassLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using static davClassLibrary.Models.TableObject;

namespace davClassLibrary.DataAccess
{
    public static class DataManager
    {
        private static bool syncing = false;
        internal static List<TableObject> fileDownloads = new List<TableObject>();
        internal static Dictionary<Guid, WebClient> fileDownloaders = new Dictionary<Guid, WebClient>();
        private static Timer fileDownloadTimer;
        private static bool syncAgain = false;
        private const int downloadFilesSimultaneously = 2;
        private const string extPropertyName = "ext";

        internal static async Task Sync()
        {
            if (syncing) return;

            syncing = true;
            string jwt = DavUser.GetJWT();
            if (String.IsNullOrEmpty(jwt)) return;
            fileDownloads.Clear();
            fileDownloaders.Clear();

            // Get the specified tables
            var tableIds = ProjectInterface.RetrieveConstants.GetTableIds();
            foreach (var tableId in tableIds)
            {
                KeyValuePair<bool, string> tableGetResult;
                TableData table;
                var pages = 1;
                bool tableGetResultsOkay = true;

                List<Guid> removedTableObjectUuids = new List<Guid>();
                foreach (var tableObject in Dav.Database.GetAllTableObjects(tableId, true))
                    removedTableObjectUuids.Add(tableObject.Uuid);

                for (int i = 1; i < pages + 1; i++)
                {
                    // Get the next page of the table
                    tableGetResult = await HttpGet(jwt, "apps/table/" + tableId + "?page=" + i);
                    if (!tableGetResult.Key)
                    {
                        tableGetResultsOkay = false;
                        continue;
                    }

                    table = JsonConvert.DeserializeObject<TableData>(tableGetResult.Value);
                    pages = table.pages;

                    // Get the objects of the table
                    foreach (var obj in table.table_objects)
                    {
                        removedTableObjectUuids.Remove(obj.uuid);

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
                        var currentTableObject = Dav.Database.GetTableObject(obj.uuid);
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
                                var tableObject = await DownloadTableObject(currentTableObject.Uuid);

                                if (tableObject == null) continue;

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
                                    tableObject.SaveWithProperties();

                                    // Download the file
                                    fileDownloads.Add(tableObject);
                                }
                                else
                                {
                                    // Save the table object
                                    tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                                    tableObject.SaveWithProperties();
                                    ProjectInterface.TriggerAction.UpdateTableObject(tableObject, false);
                                }
                            }
                        }
                        else
                        {
                            // GET the table object
                            var tableObject = await DownloadTableObject(obj.uuid);

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
                                tableObject.SaveWithProperties();
                                tableObject.SetUploadStatus(TableObjectUploadStatus.UpToDate);

                                // Download the file
                                tableObject.Etag = etag;
                                fileDownloads.Add(tableObject);

                                ProjectInterface.TriggerAction.UpdateTableObject(tableObject, false);
                            }
                            else
                            {
                                // Save the table object
                                tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                                tableObject.SaveWithProperties();
                                ProjectInterface.TriggerAction.UpdateTableObject(tableObject, false);
                            }
                        }
                    }
                }

                if (!tableGetResultsOkay) continue;
                bool objectsDeleted = false;

                // RemovedTableObjects now includes all objects that were deleted on the server but not locally
                // Delete those objects locally
                foreach (var objUuid in removedTableObjectUuids)
                {
                    var obj = Dav.Database.GetTableObject(objUuid);
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

                    obj.DeleteImmediately();
                    objectsDeleted = true;
                }

                if (objectsDeleted)
                    ProjectInterface.TriggerAction.UpdateAllOfTable(tableId);
            }

            syncing = false;

            // Push changes
            await SyncPush();
            DownloadFiles();
        }

        internal static async Task SyncPush()
        {
            if (syncing)
            {
                syncAgain = true;
                return;
            }

            string jwt = DavUser.GetJWT();
            if (String.IsNullOrEmpty(jwt)) return;

            syncing = true;

            List<TableObject> tableObjects = Dav.Database.GetAllTableObjects(true)
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
                    var etag = await tableObject.CreateTableObjectOnServer();
                    if (!String.IsNullOrEmpty(etag))
                    {
                        tableObject.Etag = etag;
                        tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                        tableObject.Save();
                    }
                }
                else if (tableObject.UploadStatus == TableObjectUploadStatus.Updated)
                {
                    // Update the object on the server
                    var etag = await tableObject.UpdateTableObjectOnServer();
                    if (!String.IsNullOrEmpty(etag))
                    {
                        tableObject.Etag = etag;
                        tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;
                        tableObject.Save();
                    }
                }
                else if (tableObject.UploadStatus == TableObjectUploadStatus.Deleted)
                {
                    // Delete the object on the server
                    if (await tableObject.DeleteTableObjectOnServer())
                        Dav.Database.DeleteTableObject(tableObject.Uuid);
                }
            }

            syncing = false;

            if (syncAgain)
            {
                syncAgain = false;
                await SyncPush();
            }
        }

        internal static void SetEtagOfTableObject(Guid uuid, string etag)
        {
            // Get the table object
            var tableObject = Dav.Database.GetTableObject(uuid);
            if (tableObject != null)
            {
                tableObject.Etag = etag;
                tableObject.Save();
            }
        }

        private static async Task<TableObject> DownloadTableObject(Guid uuid)
        {
            var getResult = await HttpGet(DavUser.GetJWT(), "apps/object/" + uuid);
            if (getResult.Key)
            {
                var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(getResult.Value);
                tableObjectData.id = 0;
                return ConvertTableObjectDataToTableObject(tableObjectData);
            }
            else
            {
                HandleOtherErrorCodes(getResult.Value);
                return null;
            }
        }

        private static void HandleOtherErrorCodes(string errorMessage)
        {
            if (errorMessage.Contains("1301") || errorMessage.Contains("1302") || errorMessage.Contains("1303"))
            {
                // JWT is invalid or has expired. Log out the user
                DavUser.SetJWT(null);
            }
        }

        private static void DownloadFiles()
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
            if (fileDownloaders.Count < downloadFilesSimultaneously &&
                fileDownloads.Count > 0)
            {
                // Get a file that is still not being downloaded
                foreach (var tableObject in fileDownloads)
                {
                    WebClient client;
                    if (!fileDownloaders.TryGetValue(tableObject.Uuid, out client))
                    {
                        tableObject.DownloadTableObjectFile();
                        break;
                    }
                }
            }
            else if (fileDownloads.Count == 0)
            {
                // Stop the timer
                fileDownloadTimer.Stop();
            }
        }

        internal static async Task<KeyValuePair<bool, string>> HttpGet(string jwt, string url)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                HttpClient httpClient = new HttpClient();
                var headers = httpClient.DefaultRequestHeaders;
                headers.Authorization = new AuthenticationHeaderValue(jwt);
                Uri requestUri = new Uri(Dav.ApiBaseUrl + url);

                HttpResponseMessage httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";

                //Send the GET request
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    return new KeyValuePair<bool, string>(true, httpResponseBody);
                }
                else
                {
                    // Return error message
                    return new KeyValuePair<bool, string>(false, "There was an error");
                }
            }
            else
            {
                // Return error message
                return new KeyValuePair<bool, string>(false, "No internet connection");
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

        public static async Task ExportData(DirectoryInfo exportFolder, IProgress<int> progress)
        {
            // 1. foreach all table object
            // 1.1 Create a folder for every table in the export folder
            // 1.2 Write the list as json to a file in the export folder
            // 2. If the tableObject is a file, copy the file into the appropriate folder
            // 3. Zip the export folder and copy it to the destination
            // 4. Delete the export folder and the zip file in the local storage

            await Task.Run(() =>
            {
                List<TableObjectData> tableObjectDataList = new List<TableObjectData>();
                var tableObjects = Dav.Database.GetAllTableObjects(false);
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

        public static void ImportData(DirectoryInfo importFolder, IProgress<int> progress)
        {
            string dataFilePath = Path.Combine(importFolder.FullName, Dav.ExportDataFileName);
            FileInfo dataFile = new FileInfo(dataFilePath);
            List<TableObjectData> tableObjects = GetDataFromFile(dataFile);
            int i = 0;

            foreach (var tableObjectData in tableObjects)
            {
                TableObject tableObject = ConvertTableObjectDataToTableObject(tableObjectData);
                tableObject.UploadStatus = TableObjectUploadStatus.New;

                if (!Dav.Database.TableObjectExists(tableObject.Uuid))
                {
                    Dav.Database.CreateTableObjectWithProperties(tableObject);

                    // If the tableObject is a file, get the file from the appropriate folder
                    if (tableObject.IsFile)
                    {
                        try
                        {
                            string tablePath = Path.Combine(importFolder.FullName, tableObject.TableId.ToString());
                            string filePath = Path.Combine(tablePath, tableObject.Uuid.ToString());
                            FileInfo tableObjectFile = new FileInfo(filePath);
                            tableObject.SetFile(tableObjectFile);
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

        internal static void WriteFile(string path, Object objectToWrite)
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(objectToWrite.GetType());
            MemoryStream ms = new MemoryStream();
            js.WriteObject(ms, objectToWrite);

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string data = sr.ReadToEnd();

            File.WriteAllText(path, data);
        }

        internal static List<TableObjectData> GetDataFromFile(FileInfo dataFile)
        {
            string data = File.ReadAllText(dataFile.FullName);

            //Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(List<TableObjectData>));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (List<TableObjectData>)serializer.ReadObject(ms);

            return dataReader;
        }

        internal static Guid ConvertStringToGuid(string uuidString)
        {
            Guid uuid = Guid.Empty;
            Guid.TryParse(uuidString, out uuid);
            return uuid;
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
    }
}
