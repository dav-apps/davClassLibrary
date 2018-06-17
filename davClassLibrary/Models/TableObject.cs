using davClassLibrary.Common;
using davClassLibrary.DataAccess;
using MimeTypes;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace davClassLibrary.Models
{
    public class TableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; private set; }
        public int TableId { get; private set; }
        public TableObjectVisibility Visibility { get; private set; }
        [NotNull]
        public Guid Uuid { get; private set; }
        public bool IsFile { get; private set; }
        [Ignore]
        public FileInfo File { get; private set; }
        [Ignore]
        public List<Property> Properties { get; private set; }
        public TableObjectUploadStatus UploadStatus { get; private set; }
        public string Etag { get; private set; }

        public enum TableObjectVisibility
        {
            Private = 0,
            Protected = 1,
            Public = 2
        }
        public enum TableObjectUploadStatus
        {
            UpToDate = 0,
            New = 1,
            Updated = 2,
            Deleted = 3,
            NoUpload = 4
        }
        private static bool syncing = false;
        private static List<TableObject> fileDownloads = new List<TableObject>();
        private static Dictionary<Guid, WebClient> fileDownloaders = new Dictionary<Guid, WebClient>();
        private static bool syncAgain = false;
        private const int downloadFilesSimultaneously = 2;

        public TableObject(){}
        
        public TableObject(int tableId)
        {
            Uuid = Guid.NewGuid();
            TableId = tableId;
            Properties = new List<Property>();
            UploadStatus = TableObjectUploadStatus.New;

            Save();
        }

        public TableObject(Guid uuid, int tableId)
        {
            Uuid = uuid;
            TableId = tableId;
            Properties = new List<Property>();
            UploadStatus = TableObjectUploadStatus.New;

            Save();
        }

        public TableObject(Guid uuid, int tableId, FileInfo file)
        {
            Uuid = uuid;
            TableId = tableId;
            Properties = new List<Property>();
            UploadStatus = TableObjectUploadStatus.New;

            IsFile = true;
            Save();

            SaveFile(file);
        }

        public TableObject(Guid uuid, int tableId, List<Property> properties)
        {
            Uuid = uuid;
            TableId = tableId;
            Properties = properties;
            UploadStatus = TableObjectUploadStatus.New;

            SaveWithProperties();
        }
        
        public void SetVisibility(TableObjectVisibility visibility)
        {
            Visibility = visibility;
            Save();
        }

        public void SetFile(FileInfo file)
        {
            SaveFile(file);
        }

        public FileInfo GetFile()
        {
            // Check if the file was downloaded
            if (!IsFile) return null;
            if(File == null)
            {
                if (FileDownloaded())
                {
                    // Get the file
                    string path = Path.Combine(Dav.DataPath, TableId.ToString(), Uuid.ToString());
                    return new FileInfo(path);
                }
                else
                {
                    // Download the file
                    DownloadTableObjectFile();
                    return File;
                }
            }
            else
            {
                return File;
            }
        }

        public Uri GetFileUri()
        {
            if (!IsFile) return null;
            string jwt = DavUser.GetJWT();
            if (String.IsNullOrEmpty(jwt)) return null;

            return new Uri(Dav.ApiBaseUrl + "apps/object/" + Uuid + "?file=true&jwt=" + jwt);
        }

        public async Task<MemoryStream> GetFileStream()
        {
            if (!IsFile) return null;
            string jwt = DavUser.GetJWT();
            if (String.IsNullOrEmpty(jwt)) return null;

            string url = Dav.ApiBaseUrl + "apps/object/" + Uuid + "?file=true&jwt=" + jwt;

            WebClient client = new WebClient();
            var stream = await client.OpenReadTaskAsync(url);

            MemoryStream streamCopy = new MemoryStream();
            await stream.CopyToAsync(streamCopy);
            return streamCopy;
        }

        public bool FileDownloaded()
        {
            return System.IO.File.Exists(GetFilePath());
        }
        
        private string GetFilePath()
        {
            return Path.Combine(DavDatabase.GetTableFolder(TableId).FullName, Uuid.ToString());
        }

        public void Load()
        {
            LoadProperties();
            LoadFile();
        }

        private void Save()
        {
            // Check if the tableObject already exists
            if (!Dav.Database.TableObjectExists(Uuid))
            {
                UploadStatus = TableObjectUploadStatus.New;
                Id = Dav.Database.CreateTableObject(this);
            }
            else
            {
                Dav.Database.UpdateTableObject(this);
            }
        }

        private void SaveWithProperties()
        {
            // Check if the tableObject already exists
            if (!Dav.Database.TableObjectExists(Uuid))
            {
                Dav.Database.CreateTableObjectWithProperties(this);
            }
            else
            {
                var tableObject = Dav.Database.GetTableObject(Uuid);
                Id = tableObject.Id;

                Dav.Database.UpdateTableObject(this);
                foreach (var property in Properties)
                    tableObject.SetPropertyValue(property.Name, property.Value);
            }

            SyncPush();
        }

        private void LoadProperties()
        {
            Properties = Dav.Database.GetPropertiesOfTableObject(Id);
        }

        public void SetPropertyValue(string name, string value)
        {
            var property = Properties.Find(prop => prop.Name == name);

            if(property != null)
            {
                // Update the property
                if (property.Value == value) return;

                property.SetValue(value);
            }
            else
            {
                // Create a new property
                Properties.Add(new Property(Id, name, value));
            }

            if (UploadStatus == TableObjectUploadStatus.UpToDate && !IsFile)
                UploadStatus = TableObjectUploadStatus.Updated;

            Save();
            SyncPush();
        }

        public string GetPropertyValue(string name)
        {
            var property = Properties.Find(prop => prop.Name == name);
            if (property != null)
                return property.Value;
            else
                return null;
        }

        public void RemoveProperty(string name)
        {
            var property = Properties.Find(prop => prop.Name == name);
            if (property == null) return;
            Properties.Remove(property);

            if (UploadStatus == TableObjectUploadStatus.UpToDate)
                UploadStatus = TableObjectUploadStatus.Updated;

            Dav.Database.DeleteProperty(property);
            SyncPush();
        }

        public void RemoveAllProperties()
        {
            LoadProperties();
            foreach (var property in Properties)
                Dav.Database.DeleteProperty(property);

            Properties.Clear();

            if (UploadStatus == TableObjectUploadStatus.UpToDate)
                UploadStatus = TableObjectUploadStatus.Updated;
        }

        private void LoadFile()
        {
            if (!IsFile) return;

            string filePath = Path.Combine(Dav.DataPath, TableId.ToString(), Uuid.ToString());
            var file = new FileInfo(filePath);

            if (file != null)
                File = file;
        }

        private FileInfo SaveFile(FileInfo file)
        {
            if (!IsFile) return null;
            if (File == file) return File;

            if (UploadStatus == TableObjectUploadStatus.UpToDate)
                UploadStatus = TableObjectUploadStatus.Updated;

            // Save the file in the data folder with the uuid as name (without extension)
            string filename = Uuid.ToString();
            var tableFolder = DavDatabase.GetTableFolder(TableId);
            File = file.CopyTo(Path.Combine(tableFolder.FullName, filename), true);

            if (!String.IsNullOrEmpty(file.Extension))
                SetPropertyValue("ext", file.Extension.Replace(".", ""));

            Save();
            return File;
        }

        public void SetUploadStatus(TableObjectUploadStatus newUploadStatus)
        {
            if (UploadStatus == newUploadStatus) return;

            UploadStatus = newUploadStatus;
            Save();
        }

        public void Delete()
        {
            if(IsFile && File != null)
            {
                // Delete the file
                File.Delete();
            }

            SetUploadStatus(TableObjectUploadStatus.Deleted);
            SyncPush();
        }
        
        private static void SetEtagOfTableObject(Guid uuid, string etag)
        {
            // Get the table object
            var tableObject = Dav.Database.GetTableObject(uuid);
            if(tableObject != null)
            {
                tableObject.Etag = etag;
                tableObject.Save();
            }
        }

        public static async Task Sync()
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
                var tableGetResult = await DavDatabase.HttpGet(jwt, "apps/table/" + tableId);
                if (!tableGetResult.Key) continue;

                var table = JsonConvert.DeserializeObject<TableData>(tableGetResult.Value);
                bool objectsDeleted = false;

                List<Guid> removedTableObjectUuids = new List<Guid>();
                foreach (var tableObject in Dav.Database.GetAllTableObjects(table.id, true))
                    removedTableObjectUuids.Add(tableObject.Uuid);

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
                            // Download the file
                            fileDownloads.Add(tableObject);

                            // Save the table object without properties and etag
                            tableObject.Etag = "";
                            tableObject.Save();
                            tableObject.SetUploadStatus(TableObjectUploadStatus.UpToDate);
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

                    Dav.Database.DeleteTableObject(obj);
                    objectsDeleted = true;
                }

                if (objectsDeleted)
                    ProjectInterface.TriggerAction.UpdateAllOfTable(table.id);
            }

            syncing = false;

            // Push changes
            await SyncPush();
            DownloadFiles();
        }

        public static async Task SyncPush()
        {
            if (syncing)
            {
                syncAgain = true;
                return;
            }

            syncing = true;

            List<TableObject> tableObjects = Dav.Database.GetAllTableObjects(true)
                                            .Where(obj => obj.UploadStatus != TableObjectUploadStatus.NoUpload && 
                                                    obj.UploadStatus != TableObjectUploadStatus.UpToDate)
                                                    .OrderByDescending(obj => obj.Id).ToList();
            foreach (var tableObject in tableObjects)
            {
                if (tableObject.UploadStatus == TableObjectUploadStatus.New)
                {
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

            if(syncAgain)
            {
                syncAgain = false;
                await SyncPush();
            }
        }

        private static async Task<TableObject> DownloadTableObject(Guid uuid)
        {
            var getResult = await DavDatabase.HttpGet(DavUser.GetJWT(), "apps/object/" + uuid);
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

        private async Task<string> CreateTableObjectOnServer()
        {
            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    if (IsFile && File == null) return null;
                    string jwt = DavUser.GetJWT();
                    if (String.IsNullOrEmpty(jwt)) return null;

                    string ext = "";
                    string url = "apps/object?uuid=" + Uuid + "&app_id=" + Dav.AppId + "&table_id=" + TableId;
                    HttpClient httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromMinutes(60);

                    var headers = httpClient.DefaultRequestHeaders;
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(jwt);
                    if (IsFile)
                    {
                        ext = GetPropertyValue("ext");
                        httpClient.DefaultRequestHeaders.Add("CONTENT_TYPE", MimeTypeMap.GetMimeType(ext));
                    }

                    HttpContent content;

                    if (IsFile)
                    {
                        // Upload the file
                        byte[] bytesFile = DavDatabase.FileToByteArray(File.FullName);
                        content = new ByteArrayContent(bytesFile);
                        if (bytesFile == null) return null;

                        if (!String.IsNullOrEmpty(ext))
                            url += "&ext=" + ext;
                    }
                    else
                    {
                        // Upload the properties
                        string json = JsonConvert.SerializeObject(DavDatabase.ConvertPropertiesListToDictionary(Properties));
                        content = new StringContent(json, Encoding.UTF8, "application/json");
                    }

                    Uri requestUri = new Uri(Dav.ApiBaseUrl + url);

                    // Send the request
                    var httpResponse = await httpClient.PostAsync(requestUri, content);
                    string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        // Get the properties of the response
                        TableObjectData tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(httpResponseBody);
                        Etag = tableObjectData.etag;

                        foreach (var property in tableObjectData.properties)
                            SetPropertyValue(property.Key, property.Value);

                        return tableObjectData.etag;
                    }
                    else
                    {
                        // Check for the error
                        if (httpResponseBody.Contains("2704"))  // Field already taken: uuid
                        {
                            // Set the upload status to UpToDate
                            SetUploadStatus(TableObjectUploadStatus.UpToDate);
                        }

                        return null;
                    }
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
            
            return null;
        }

        private async Task<string> UpdateTableObjectOnServer()
        {
            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    if (IsFile && File == null) return null;
                    string jwt = DavUser.GetJWT();
                    if (String.IsNullOrEmpty(jwt)) return null;

                    string url = "apps/object/" + Uuid;
                    HttpClient httpClient = new HttpClient();
                    httpClient.Timeout = TimeSpan.FromMinutes(60);
                    
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(jwt);
                    Uri requestUri = new Uri(Dav.ApiBaseUrl + url);

                    HttpContent content;

                    if (IsFile)
                    {
                        // Upload the file
                        byte[] bytesFile = DavDatabase.FileToByteArray(File.FullName);
                        if (bytesFile == null) return null;
                        content = new ByteArrayContent(bytesFile);
                    }
                    else
                    {
                        // Upload the properties
                        string json = JsonConvert.SerializeObject(DavDatabase.ConvertPropertiesListToDictionary(Properties));
                        content = new StringContent(json, Encoding.UTF8, "application/json");
                    }
                    // Send the request
                    var httpResponse = await httpClient.PutAsync(requestUri, content);

                    string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        // Check error codes
                        // TODO

                        return null;
                    }
                    else
                    {
                        TableObjectData tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(httpResponseBody);
                        return tableObjectData.etag;
                    }
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }

            return null;
        }

        private async Task<bool> DeleteTableObjectOnServer()
        {
            try
            {
                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    string jwt = DavUser.GetJWT();
                    if (String.IsNullOrEmpty(jwt)) return false;

                    string url = "apps/object/" + Uuid;
                    HttpClient httpClient = new HttpClient();
                    var headers = httpClient.DefaultRequestHeaders;
                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(jwt);
                    Uri requestUri = new Uri(Dav.ApiBaseUrl + url);

                    var httpResponse = await httpClient.DeleteAsync(requestUri);

                    if (httpResponse.IsSuccessStatusCode)
                        return true;
                    else
                    {
                        string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                        // Check the error code
                        if (httpResponseBody.Contains("2805"))  // Resource does not exist: TableObject
                        {
                            return true;
                        }
                        else if (httpResponseBody.Contains("1102"))    // Action not allowed
                        {
                            return true;
                        }
                    }
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }

            return false;
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
            var timer = new Timer();
            timer.Elapsed += DownloadFileTimer_Elapsed;
            timer.Interval = 5000;
            timer.Start();
        }

        private static void DownloadFileTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            // Check if fileDownloads list is greater than downloadFilesSimultaneously
            if(fileDownloaders.Count < downloadFilesSimultaneously && 
                fileDownloads.Count > 0)
            {
                // Get a file that is still not being downloaded
                foreach(var tableObject in fileDownloads)
                {
                    WebClient client;
                    if(!fileDownloaders.TryGetValue(tableObject.Uuid, out client))
                    {
                        tableObject.DownloadTableObjectFile();
                        break;
                    }
                }
            }
        }

        public void DownloadTableObjectFile()
        {
            if (!IsFile) return;
            string jwt = DavUser.GetJWT();
            if (String.IsNullOrEmpty(jwt)) return;

            // Check if the file is already downloading
            if (fileDownloaders.ContainsKey(Uuid))
                return;

            string url = Dav.ApiBaseUrl + "apps/object/" + Uuid + "?file=true";
            WebClient client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Authorization, jwt);
            client.DownloadFileCompleted += Client_DownloadFileCompleted;

            DirectoryInfo tempTableFolder = DavDatabase.GetTempTableFolder(TableId);
            string tempFilePath = Path.Combine(tempTableFolder.FullName, Uuid.ToString());

            client.DownloadFileAsync(new Uri(url), tempFilePath);
            fileDownloaders.Add(Uuid, client);
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null) return;

            DirectoryInfo tempTableFolder = DavDatabase.GetTempTableFolder(TableId);
            DirectoryInfo tableFolder = DavDatabase.GetTableFolder(TableId);

            string tempFilePath = Path.Combine(tempTableFolder.FullName, Uuid.ToString());
            string filePath = Path.Combine(tableFolder.FullName, Uuid.ToString());

            // Delete the old file if it exists
            FileInfo oldFile = new FileInfo(filePath);
            if (oldFile.Exists)
                oldFile.Delete();

            // Move the new file into the folder of the table
            FileInfo file = new FileInfo(tempFilePath);
            file.MoveTo(filePath);

            fileDownloaders.Remove(Uuid);
            fileDownloads.Remove(this);

            // Save the etags of the table object
            SetEtagOfTableObject(Uuid, Etag);

            ProjectInterface.TriggerAction.UpdateTableObject(this, true);
        }

        public static TableObjectVisibility ParseIntToVisibility(int visibility)
        {
            switch (visibility)
            {
                case 1:
                    return TableObjectVisibility.Protected;
                case 2:
                    return TableObjectVisibility.Public;
                default:
                    return TableObjectVisibility.Private;
            }
        }

        public static int ParseVisibilityToInt(TableObjectVisibility visibility)
        {
            switch (visibility)
            {
                case TableObjectVisibility.Protected:
                    return 1;
                case TableObjectVisibility.Public:
                    return 2;
                default:
                    return 0;

            }
        }

        public TableObjectData ToTableObjectData()
        {
            var tableObjectData = new TableObjectData
            {
                id = Id,
                table_id = TableId,
                visibility = ParseVisibilityToInt(Visibility),
                uuid = Uuid,
                file = IsFile,
                etag = Etag,
                properties = new Dictionary<string, string>()
            };
            
            foreach(var property in Properties)
            {
                tableObjectData.properties.Add(property.Name, property.Value);
            }
            
            return tableObjectData;
        }

        public static TableObject ConvertTableObjectDataToTableObject(TableObjectData tableObjectData)
        {
            TableObject tableObject = new TableObject
            {
                Id = tableObjectData.id,
                TableId = tableObjectData.table_id,
                Visibility = ParseIntToVisibility(tableObjectData.visibility),
                Uuid = tableObjectData.uuid,
                IsFile = tableObjectData.file,
                Etag = tableObjectData.etag
            };

            List<Property> properties = new List<Property>();

            if(tableObjectData.properties != null)
            {
                foreach (var propertyData in tableObjectData.properties)
                {
                    properties.Add(new Property
                    {
                        Name = propertyData.Key,
                        Value = propertyData.Value
                    });
                }
            }
            
            tableObject.Properties = properties;
            return tableObject;
        }
    }

    public class TableObjectData
    {
        public int id { get; set; }
        public int table_id { get; set; }
        public int visibility { get; set; }
        public Guid uuid { get; set; }
        public bool file { get; set; }
        public Dictionary<string, string> properties { get; set; }
        public string etag { get; set; }
    }
}
