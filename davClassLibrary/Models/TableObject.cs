using davClassLibrary.Common;
using davClassLibrary.DataAccess;
using MimeTypes;
using Newtonsoft.Json;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Text;
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
        public TableObjectUploadStatus UploadStatus { get; set; }
        public string Etag { get; internal set; }

        private IProgress<int> fileDownloadProgress = null;

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
            return Path.Combine(DataManager.GetTableFolder(TableId).FullName, Uuid.ToString());
        }

        public void Load()
        {
            LoadProperties();
            LoadFile();
        }

        internal void Save()
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

        internal void SaveWithProperties()
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

            DataManager.SyncPush();
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
            DataManager.SyncPush();
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
            DataManager.SyncPush();
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
            var tableFolder = DataManager.GetTableFolder(TableId);
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
            string jwt = DavUser.GetJWT();
            if (String.IsNullOrEmpty(jwt))
            {
                DeleteImmediately();
                UploadStatus = TableObjectUploadStatus.Deleted;
            }
            else
            {
                if (IsFile && File.Exists)
                {
                    // Delete the file
                    File.Delete();
                }

                SetUploadStatus(TableObjectUploadStatus.Deleted);
                DataManager.SyncPush();
            }
        }

        public void DeleteImmediately()
        {
            if (IsFile && File != null)
            {
                // Delete the file
                File.Delete();
            }

            Dav.Database.DeleteTableObjectImmediately(Uuid);
        }

        internal void DownloadTableObjectFile()
        {
            if (!IsFile) return;
            string jwt = DavUser.GetJWT();
            if (String.IsNullOrEmpty(jwt)) return;

            // Check if the file is already downloading
            if (DataManager.fileDownloaders.ContainsKey(Uuid))
                return;

            string url = Dav.ApiBaseUrl + "apps/object/" + Uuid + "?file=true";
            WebClient client = new WebClient();
            client.Headers.Add(HttpRequestHeader.Authorization, jwt);
            client.DownloadFileCompleted += Client_DownloadFileCompleted;

            DirectoryInfo tempTableFolder = DataManager.GetTempTableFolder(TableId);
            string tempFilePath = Path.Combine(tempTableFolder.FullName, Uuid.ToString());

            client.DownloadFileAsync(new Uri(url), tempFilePath);
            DataManager.fileDownloaders.Add(Uuid, client);
        }

        private void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null) return;
            CopyDownloadedFile();
        }

        public void DownloadFile(Progress<int> progress)
        {
            fileDownloadProgress = progress;

            if (!IsFile) fileDownloadProgress.Report(-1);
            string jwt = DavUser.GetJWT();
            if (string.IsNullOrEmpty(jwt)) fileDownloadProgress.Report(-1);

            // Check if the file was already downloaded
            if (File.Exists)
                fileDownloadProgress.Report(100);

            // Check if the file is in the downloads list
            if (DataManager.fileDownloads.Exists(t => t.Uuid == Uuid))
            {
                // Remove the file from the downloads
                var tableObject = DataManager.fileDownloads.Find(t => t.Uuid == Uuid);
                if(tableObject != null)
                    DataManager.fileDownloads.Remove(tableObject);
            }

            // Check if the file is already downloading
            var webClient = new WebClient();
            if (DataManager.fileDownloaders.TryGetValue(Uuid, out webClient))
            {
                webClient.DownloadProgressChanged += DownloadFileWebClient_DownloadProgressChanged;
                webClient.DownloadFileCompleted += DownloadFileWebClient_DownloadFileCompleted;
            }
            else
            {
                // Start a new download
                webClient = new WebClient();
                webClient.Headers.Add(HttpRequestHeader.Authorization, jwt);
                webClient.DownloadProgressChanged += DownloadFileWebClient_DownloadProgressChanged;
                webClient.DownloadFileCompleted += DownloadFileWebClient_DownloadFileCompleted;

                string url = Dav.ApiBaseUrl + "apps/object/" + Uuid + "?file=true";
                DirectoryInfo tempTableFolder = DataManager.GetTempTableFolder(TableId);
                string tempFilePath = Path.Combine(tempTableFolder.FullName, Uuid.ToString());
                webClient.DownloadFileAsync(new Uri(url), tempFilePath);
            }
        }

        private void DownloadFileWebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            fileDownloadProgress.Report(e.ProgressPercentage);
        }

        private void DownloadFileWebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null) return;
            CopyDownloadedFile();
            fileDownloadProgress.Report(101);
        }

        private void CopyDownloadedFile()
        {
            DirectoryInfo tempTableFolder = DataManager.GetTempTableFolder(TableId);
            DirectoryInfo tableFolder = DataManager.GetTableFolder(TableId);

            string tempFilePath = Path.Combine(tempTableFolder.FullName, Uuid.ToString());
            string filePath = Path.Combine(tableFolder.FullName, Uuid.ToString());

            // Delete the old file if it exists
            FileInfo oldFile = new FileInfo(filePath);
            if (oldFile.Exists)
                oldFile.Delete();

            // Move the new file into the folder of the table
            FileInfo file = new FileInfo(tempFilePath);
            file.MoveTo(filePath);

            DataManager.fileDownloaders.Remove(Uuid);
            DataManager.fileDownloads.Remove(this);

            // Save the etags of the table object
            DataManager.SetEtagOfTableObject(Uuid, Etag);

            ProjectInterface.TriggerAction.UpdateTableObject(this, true);
        }

        internal async Task<string> CreateTableObjectOnServer()
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
                        byte[] bytesFile = DataManager.FileToByteArray(File.FullName);
                        content = new ByteArrayContent(bytesFile);
                        if (bytesFile == null) return null;

                        if (!String.IsNullOrEmpty(ext))
                            url += "&ext=" + ext;
                    }
                    else
                    {
                        // Upload the properties
                        string json = JsonConvert.SerializeObject(DataManager.ConvertPropertiesListToDictionary(Properties));
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

        internal async Task<string> UpdateTableObjectOnServer()
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
                        byte[] bytesFile = DataManager.FileToByteArray(File.FullName);
                        if (bytesFile == null) return null;
                        content = new ByteArrayContent(bytesFile);
                    }
                    else
                    {
                        // Upload the properties
                        string json = JsonConvert.SerializeObject(DataManager.ConvertPropertiesListToDictionary(Properties));
                        content = new StringContent(json, Encoding.UTF8, "application/json");
                    }
                    // Send the request
                    var httpResponse = await httpClient.PutAsync(requestUri, content);

                    string httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                    if (!httpResponse.IsSuccessStatusCode)
                    {
                        // Check error codes
                        if (httpResponseBody.Contains("2805"))      // Resource does not exist: TableObject
                        {
                            // Delete the table object locally
                            DeleteImmediately();
                        }

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

        internal async Task<bool> DeleteTableObjectOnServer()
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
