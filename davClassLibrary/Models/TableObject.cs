using davClassLibrary.Common;
using davClassLibrary.DataAccess;
using MimeTypes;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Threading.Tasks;

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
        private static List<Guid> fileDownloads = new List<Guid>();
        private static Dictionary<Guid, WebClient> fileDownloaders = new Dictionary<Guid, WebClient>();
        private static bool syncAgain = false;

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

                foreach(var property in Properties)
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

            if (UploadStatus == TableObjectUploadStatus.UpToDate)
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

        public static bool TableObjectsAreEqual(TableObject firstTableObject, TableObject secondTableObject)
        {
            if (firstTableObject == null || secondTableObject == null) return false;
            if (!Equals(firstTableObject.Uuid, secondTableObject.Uuid)) return false;
            if (!Equals(firstTableObject.IsFile, secondTableObject.IsFile)) return false;

            // Check the properties
            if (!Equals(firstTableObject.Properties.Count, secondTableObject.Properties.Count)) return false;
            var firstTableObjectDictionary = DavDatabase.ConvertPropertiesListToDictionary(firstTableObject.Properties);
            var secondTableObjectDictionary = DavDatabase.ConvertPropertiesListToDictionary(secondTableObject.Properties);

            return firstTableObjectDictionary.All(secondTableObjectDictionary.Contains);
        }

        public static async Task Sync()
        {
            syncing = true;
            string jwt = DavUser.GetJWT();
            if (String.IsNullOrEmpty(jwt)) return;
            fileDownloads.Clear();
            fileDownloaders.Clear();

            // Get app
            string appInformation = await DavDatabase.HttpGet(DavUser.GetJWT(), "apps/app/" + Dav.AppId);
            if (appInformation != null)
            {
                // Create app object
                var app = JsonConvert.DeserializeObject<AppData>(appInformation);

                // Get tables of the app
                foreach (var tableData in app.tables)
                {
                    string tableInformation = await DavDatabase.HttpGet(jwt, "apps/table/" + tableData.id);
                    var table = JsonConvert.DeserializeObject<TableData>(tableInformation);
                    bool objectsDeleted = false;

                    List<Guid> removedTableObjectUuids = new List<Guid>();
                    foreach (var tableObject in Dav.Database.GetAllTableObjects(table.id, true))
                        removedTableObjectUuids.Add(tableObject.Uuid);

                    // Get the objects of the table
                    foreach (var obj in table.entries)
                    {
                        // Get the proper table object
                        string tableObjectInformation = await DavDatabase.HttpGet(DavUser.GetJWT(), "apps/object/" + obj.uuid);
                        var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(tableObjectInformation);
                        var tableObject = ConvertTableObjectDataToTableObject(tableObjectData);
                        removedTableObjectUuids.Remove(tableObject.Uuid);
                        var currentTableObject = Dav.Database.GetTableObject(tableObject.Uuid);

                        bool downloadFile = tableObject.IsFile;
                        if (tableObject.IsFile)
                        {
                            if (tableObject.FileDownloaded())
                            {
                                // Get the etag of the file and check if it changed
                                if (Dav.Database.TableObjectExists(tableObject.Uuid))
                                {
                                    downloadFile = !Equals(currentTableObject.GetPropertyValue("etag"), tableObject.GetPropertyValue("etag"));
                                }
                            }
                        }

                        tableObject.UploadStatus = TableObjectUploadStatus.UpToDate;

                        // If the tableObject is a file, download the file
                        if (downloadFile)
                        {
                            fileDownloads.Add(tableObject.Uuid);
                        }

                        if (TableObjectsAreEqual(currentTableObject, tableObject))
                            continue;

                        tableObject.SaveWithProperties();

                        ProjectInterface.TriggerAction.UpdateTableObject(tableObject, false);
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

            List<TableObject> tableObjects = Dav.Database.GetAllTableObjects(true).OrderByDescending(obj => obj.Id).ToList();
            foreach (var tableObject in tableObjects)
            {
                if (tableObject.UploadStatus == TableObjectUploadStatus.UpToDate ||
                    tableObject.UploadStatus == TableObjectUploadStatus.NoUpload) continue;

                if (tableObject.UploadStatus == TableObjectUploadStatus.New)
                {
                    // Create the new object on the server
                    if (await tableObject.CreateTableObjectOnServer())
                        tableObject.SetUploadStatus(TableObjectUploadStatus.UpToDate);
                }
                else if (tableObject.UploadStatus == TableObjectUploadStatus.Updated)
                {
                    // Update the object on the server
                    if (await tableObject.UpdateTableObjectOnServer())
                        tableObject.SetUploadStatus(TableObjectUploadStatus.UpToDate);
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

        private async Task<bool> CreateTableObjectOnServer()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                if (IsFile && File == null) return false;
                string jwt = DavUser.GetJWT();
                if (String.IsNullOrEmpty(jwt)) return false;

                string ext = "";
                string url = "apps/object?uuid=" + Uuid + "&app_id=" + Dav.AppId + "&table_id=" + TableId;
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(60);

                var headers = httpClient.DefaultRequestHeaders;
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(jwt);
                if (!IsFile)
                    httpClient.DefaultRequestHeaders.Add("CONTENT_TYPE", "application/json");
                else
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

                    if(!String.IsNullOrEmpty(ext))
                        url += "&ext=" + ext;
                }
                else
                {
                    // Upload the properties
                    string json = JsonConvert.SerializeObject(DavDatabase.ConvertPropertiesListToDictionary(Properties));
                    content = new StringContent(json);
                }

                Uri requestUri = new Uri(Dav.ApiBaseUrl + url);

                // Send the request
                var httpResponse = await httpClient.PostAsync(requestUri, content);
                string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    // Get the properties of the response
                    TableObjectData tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(httpResponseBody);
                    foreach(var property in tableObjectData.properties)
                        SetPropertyValue(property.Key, property.Value);
                }
                else
                {
                    // Check for the error
                    if (httpResponseBody.Contains("2704"))  // Field already taken: uuid
                    {
                        // Set the upload status to UpToDate
                        SetUploadStatus(TableObjectUploadStatus.UpToDate);
                    }
                }

                return httpResponse.IsSuccessStatusCode;
            }
            
            return false;
        }

        private async Task<bool> UpdateTableObjectOnServer()
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                if (IsFile && File == null) return false;
                string jwt = DavUser.GetJWT();
                if (String.IsNullOrEmpty(jwt)) return false;

                string url = "apps/object/" + Uuid;
                HttpClient httpClient = new HttpClient();
                httpClient.Timeout = TimeSpan.FromMinutes(60);

                var headers = httpClient.DefaultRequestHeaders;
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(jwt);
                if (!IsFile)
                    httpClient.DefaultRequestHeaders.Add("CONTENT_TYPE", "application/json");
                Uri requestUri = new Uri(Dav.ApiBaseUrl + url);

                HttpContent content;

                if (IsFile)
                {
                    // Upload the file
                    byte[] bytesFile = DavDatabase.FileToByteArray(File.FullName);
                    content = new ByteArrayContent(bytesFile);
                }
                else
                {
                    // Upload the properties
                    string json = JsonConvert.SerializeObject(DavDatabase.ConvertPropertiesListToDictionary(Properties));
                    content = new StringContent(json);
                }
                // Send the request
                var httpResponse = await httpClient.PutAsync(requestUri, content);

                if (!httpResponse.IsSuccessStatusCode)
                {
                    string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                    // Check the error codes

                }

                return httpResponse.IsSuccessStatusCode;
            }
            
            return false;
        }

        private async Task<bool> DeleteTableObjectOnServer()
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
                    }else if (httpResponseBody.Contains("1102"))    // Action not allowed
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static void DownloadFiles()
        {
            foreach (var uuid in fileDownloads)
            {
                // Download the file
                var tableObject = Dav.Database.GetTableObject(uuid);

                if (tableObject != null)
                    tableObject.DownloadTableObjectFile();
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
            if (e.Cancelled) return;

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
            ProjectInterface.TriggerAction.UpdateTableObject(this, true);
        }

        private static TableObjectVisibility ParseIntToVisibility(int visibility)
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

        private static int ParseVisibilityToInt(TableObjectVisibility visibility)
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
                IsFile = tableObjectData.file
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
    }
}
