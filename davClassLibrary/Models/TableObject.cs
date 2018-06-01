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

        private bool FileDownloaded()
        {
            string path = Path.Combine(DavDatabase.GetTableFolder(TableId).FullName, Uuid.ToString());
            return System.IO.File.Exists(path);
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
            if (File == file) return File;

            IsFile = true;
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
            
            List<TableObject> tableObjectsList = new List<TableObject>();

            // Get app
            string appInformation = await DavDatabase.HttpGet(DavUser.GetJWT(), "apps/app/" + Dav.AppId);
            if (appInformation == null) return;

            // Create app object
            var app = JsonConvert.DeserializeObject<AppData>(appInformation);
            bool objectsDeleted = false;

            // Get tables of the app
            foreach (var tableData in app.tables)
            {
                string tableInformation = await DavDatabase.HttpGet(jwt, "apps/table/" + tableData.id);
                var table = JsonConvert.DeserializeObject<TableData>(tableInformation);

                List<Guid> removedTableObjectUuids = new List<Guid>();
                foreach (var tableObject in Dav.Database.GetAllTableObjects(table.id, true))
                    removedTableObjectUuids.Add(tableObject.Uuid);
                bool tableObjectsOfTableUpdated = false;

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

                    if (TableObjectsAreEqual(currentTableObject, tableObject))
                        continue;
                    tableObjectsOfTableUpdated = true;

                    tableObject.SaveWithProperties();

                    // If the tableObject is a file, download the file
                    if (downloadFile)
                    {
                        tableObject.DownloadTableObjectFile();
                    }

                    ProjectInterface.TriggerAction.UpdateTableObject(tableObject.Uuid);
                }

                // RemovedTableObjects now includes all objects that were deleted on the server but not locally
                // Delete those objects locally
                foreach(var objUuid in removedTableObjectUuids)
                {
                    var obj = Dav.Database.GetTableObject(objUuid);
                    if (obj == null) continue;

                    if(obj.UploadStatus != TableObjectUploadStatus.New && 
                        obj.UploadStatus != TableObjectUploadStatus.NoUpload)
                    {
                        Dav.Database.DeleteTableObject(obj);
                        objectsDeleted = true;
                    }
                }

                if(tableObjectsOfTableUpdated || objectsDeleted)
                    ProjectInterface.TriggerAction.UpdateAllOfTable(table.id);
            }

            if(objectsDeleted)
                ProjectInterface.TriggerAction.UpdateAll();
            syncing = false;

            // Push changes
            await SyncPush();
        }

        public static async Task SyncPush()
        {
            if (syncing) return;
            syncing = true;

            List<TableObject> tableObjects = Dav.Database.GetAllTableObjects(true);
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
                    ProcessErrorCodes(httpResponseBody);
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
                    ProcessErrorCodes(httpResponseBody);
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
                    ProcessErrorCodes(httpResponseBody);

                    if (httpResponseBody.Contains("2805"))  // Resource does not exist: TableObject
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private void ProcessErrorCodes(string errorMessage)
        {
            if (errorMessage.Contains("2704"))  // Field already taken: uuid
            {
                // Set the upload status to UpToDate
                SetUploadStatus(TableObjectUploadStatus.UpToDate);
            }
        }

        private void DownloadTableObjectFile()
        {
            if (!IsFile) return;
            string jwt = DavUser.GetJWT();
            if (String.IsNullOrEmpty(jwt)) return;

            string url = Dav.ApiBaseUrl + "apps/object/" + Uuid + "?file=true";
            WebClient client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Authorization, jwt);
            client.DownloadProgressChanged += Client_DownloadProgressChanged;
            string path = Path.Combine(DavDatabase.GetTableFolder(TableId).FullName, Uuid.ToString());
            client.DownloadFileAsync(new Uri(url), path);
        }

        private void Client_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            Debug.WriteLine(e.ProgressPercentage);
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

            foreach(var propertyData in tableObjectData.properties)
            {
                properties.Add(new Property
                {
                    Name = propertyData.Key,
                    Value = propertyData.Value
                });
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
