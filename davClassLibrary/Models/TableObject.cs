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
        [Ignore]
        public TableObjectDownloadStatus DownloadStatus
        {
            get => GetDownloadStatus();
        }

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
        public enum TableObjectDownloadStatus
        {
            NoFileOrNotLoggedIn = 0,
            NotDownloaded = 1,
            Downloading = 2,
            Downloaded = 3
        }

        public TableObject(){}
        
        public TableObject(int tableId)
        {
            Uuid = Guid.NewGuid();
            TableId = tableId;
            Properties = new List<Property>();
            UploadStatus = TableObjectUploadStatus.New;
        }

        public TableObject(Guid uuid, int tableId)
        {
            Uuid = uuid;
            TableId = tableId;
            Properties = new List<Property>();
            UploadStatus = TableObjectUploadStatus.New;
        }
        
        public TableObject(Guid uuid, int tableId, List<Property> properties)
        {
            Uuid = uuid;
            TableId = tableId;
            Properties = properties;
            UploadStatus = TableObjectUploadStatus.New;
        }

        public static async Task<TableObject> CreateAsync(int tableId)
        {
            var tableObject = new TableObject(tableId);
            await tableObject.SaveAsync();
            return tableObject;
        }

        public static async Task<TableObject> CreateAsync(Guid uuid, int tableId)
        {
            var tableObject = new TableObject(uuid, tableId);
            await tableObject.SaveAsync();
            return tableObject;
        }

        public static async Task<TableObject> CreateAsync(Guid uuid, int tableId, FileInfo file)
        {
            var tableObject = new TableObject(uuid, tableId) { IsFile = true };
            await tableObject.SaveAsync();
            await tableObject.SetFileAsync(file);
            return tableObject;
        }

        public static async Task<TableObject> CreateAsync(Guid uuid, int tableId, List<Property> properties)
        {
            var tableObject = new TableObject(uuid, tableId, properties);
            await tableObject.SaveWithPropertiesAsync();
            return tableObject;
        }
        
        public async Task SetVisibilityAsync(TableObjectVisibility visibility)
        {
            Visibility = visibility;
            await SaveAsync();
        }
        
        public Uri GetFileUri()
        {
            if (!IsFile) return null;
            string jwt = DavUser.GetJWT();
            if (String.IsNullOrEmpty(jwt)) return null;

            return new Uri(Dav.ApiBaseUrl + "apps/object/" + Uuid + "?file=true&jwt=" + jwt);
        }

        public async Task<MemoryStream> GetFileStreamAsync()
        {
            if (!IsFile) return null;
            string jwt = DavUser.GetJWT();
            if (string.IsNullOrEmpty(jwt)) return null;

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

        public async Task LoadAsync()
        {
            await LoadPropertiesAsync();
            LoadFile();
        }

        internal async Task SaveAsync()
        {
            // Check if the tableObject already exists
            if (!await Dav.Database.TableObjectExistsAsync(Uuid))
            {
                UploadStatus = TableObjectUploadStatus.New;
                Id = await Dav.Database.CreateTableObjectAsync(this);
            }
            else
            {
                await Dav.Database.UpdateTableObjectAsync(this);
            }
        }

        internal async Task SaveWithPropertiesAsync()
        {
            // Check if the tableObject already exists
            if (!await Dav.Database.TableObjectExistsAsync(Uuid))
            {
                await Dav.Database.CreateTableObjectWithPropertiesAsync(this);
            }
            else
            {
                var tableObject = await Dav.Database.GetTableObjectAsync(Uuid);
                Id = tableObject.Id;

                // Save the table object
                await Dav.Database.UpdateTableObjectAsync(this);

                // Save the properties
                Dictionary<string, string> propertiesDict = new Dictionary<string, string>();
                foreach (var property in Properties)
                    propertiesDict.Add(property.Name, property.Value);
                await tableObject.SetPropertyValuesAsync(propertiesDict);
            }

            var x = DataManager.SyncPush();
        }

        private async Task LoadPropertiesAsync()
        {
            Properties = await Dav.Database.GetPropertiesOfTableObjectAsync(Id);
        }

        public async Task SetPropertyValueAsync(string name, string value)
        {
            var property = Properties.Find(prop => prop.Name == name);

            if(property != null)
            {
                // Update the property
                if (property.Value == value) return;
                await property.SetValue(value);
            }
            else
            {
                // Create a new property
                Properties.Add(await Property.Create(Id, name, value));
            }

            if (UploadStatus == TableObjectUploadStatus.UpToDate && !IsFile)
                UploadStatus = TableObjectUploadStatus.Updated;

            await SaveAsync();
            var x = DataManager.SyncPush();
        }

        public async Task SetPropertyValuesAsync(Dictionary<string, string> properties)
        {
            bool propertiesChanged = false;
            List<Property> propertiesToCreate = new List<Property>();
            List<Property> propertiesToUpdate = new List<Property>();

            foreach(var p in properties)
            {
                var property = Properties.Find(prop => prop.Name == p.Key);

                if(property != null)
                {
                    // Update the property
                    if (property.Value == p.Value) continue;
                    property.Value = p.Value;
                    propertiesToUpdate.Add(property);
                    propertiesChanged = true;
                }
                else
                {
                    // Create a new property
                    var newProperty = new Property(Id, p.Key, p.Value);
                    Properties.Add(newProperty);
                    propertiesToCreate.Add(newProperty);
                    propertiesChanged = true;
                }
            }

            // Save the properties in transaction
            await Dav.Database.CreatePropertiesAsync(propertiesToCreate);
            await Dav.Database.UpdatePropertiesAsync(propertiesToUpdate);

            if (!propertiesChanged) return;

            if (UploadStatus == TableObjectUploadStatus.UpToDate && !IsFile)
                UploadStatus = TableObjectUploadStatus.Updated;

            await SaveAsync();
            var x = DataManager.SyncPush();
        }

        public string GetPropertyValue(string name)
        {
            var property = Properties.Find(prop => prop.Name == name);
            if (property != null)
                return property.Value;
            else
                return null;
        }

        public async Task RemovePropertyAsync(string name)
        {
            var property = Properties.Find(prop => prop.Name == name);
            if (property == null) return;
            Properties.Remove(property);

            if (UploadStatus == TableObjectUploadStatus.UpToDate)
                UploadStatus = TableObjectUploadStatus.Updated;

            await Dav.Database.DeletePropertyAsync(property);
            var x = DataManager.SyncPush();
        }

        public async Task RemoveAllPropertiesAsync()
        {
            await LoadPropertiesAsync();
            foreach (var property in Properties)
                await Dav.Database.DeletePropertyAsync(property);

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

        public async Task<FileInfo> SetFileAsync(FileInfo file)
        {
            if (!IsFile) return null;
            if (File == file) return File;

            if (UploadStatus == TableObjectUploadStatus.UpToDate)
                UploadStatus = TableObjectUploadStatus.Updated;

            // Save the file in the data folder with the uuid as name (without extension)
            string filename = Uuid.ToString();
            var tableFolder = DataManager.GetTableFolder(TableId);
            File = file.CopyTo(Path.Combine(tableFolder.FullName, filename), true);

            if (!string.IsNullOrEmpty(file.Extension))
                await SetPropertyValueAsync("ext", file.Extension.Replace(".", ""));

            await SaveAsync();
            return File;
        }

        public async Task SetUploadStatusAsync(TableObjectUploadStatus newUploadStatus)
        {
            if (UploadStatus == newUploadStatus) return;

            UploadStatus = newUploadStatus;
            await SaveAsync();
        }

        private TableObjectDownloadStatus GetDownloadStatus()
        {
            if (!IsFile) return TableObjectDownloadStatus.NoFileOrNotLoggedIn;

            if(File != null)
                if (File.Exists) return TableObjectDownloadStatus.Downloaded;

            string jwt = DavUser.GetJWT();
            if (string.IsNullOrEmpty(jwt)) return TableObjectDownloadStatus.NoFileOrNotLoggedIn;

            if (DataManager.fileDownloaders.ContainsKey(Uuid)) return TableObjectDownloadStatus.Downloading;
            return TableObjectDownloadStatus.NotDownloaded;
        }

        public async Task DeleteAsync()
        {
            string jwt = DavUser.GetJWT();
            if (string.IsNullOrEmpty(jwt))
            {
                await DeleteImmediatelyAsync();
                UploadStatus = TableObjectUploadStatus.Deleted;
            }
            else
            {
                // Delete the file
                if (IsFile && File.Exists)
                    File.Delete();

                await SetUploadStatusAsync(TableObjectUploadStatus.Deleted);
                var x = DataManager.SyncPush();
            }
        }

        public async Task DeleteImmediatelyAsync()
        {
            if (IsFile && File != null)
            {
                // Delete the file
                File.Delete();
            }

            await Dav.Database.DeleteTableObjectImmediatelyAsync(Uuid);
        }
        
        private async void Client_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null) return;
            await CopyDownloadedFileAsync();
        }

        public void DownloadFile(IProgress<int> progress)
        {
            if (progress != null)
            {
                // Add the progress to the progress list
                if (!DataManager.fileDownloadProgressList.ContainsKey(Uuid))
                {
                    // Add a new list to the dictionary
                    DataManager.fileDownloadProgressList.Add(Uuid, new List<IProgress<int>>());
                }

                DataManager.fileDownloadProgressList[Uuid].Add(progress);
            }
                
            if (DownloadStatus == TableObjectDownloadStatus.Downloading) return;

            string jwt = DavUser.GetJWT();
            if (DownloadStatus == TableObjectDownloadStatus.NoFileOrNotLoggedIn || string.IsNullOrEmpty(jwt))
            {
                DataManager.ReportFileDownloadProgress(Uuid, -1);
                return;
            }

            if(DownloadStatus == TableObjectDownloadStatus.Downloaded)
            {
                DataManager.ReportFileDownloadProgress(Uuid, -1);
                return;
            }

            // DownloadStatus is NotDownloaded
            // Start the download
            WebClient webClient = new WebClient();
            webClient.Headers.Add(HttpRequestHeader.Authorization, jwt);
            webClient.DownloadProgressChanged += DownloadFileWebClient_DownloadProgressChanged;
            webClient.DownloadFileCompleted += DownloadFileWebClient_DownloadFileCompleted;

            string url = Dav.ApiBaseUrl + "apps/object/" + Uuid + "?file=true";
            DirectoryInfo tempTableFolder = DataManager.GetTempTableFolder(TableId);
            string tempFilePath = Path.Combine(tempTableFolder.FullName, Uuid.ToString());
            webClient.DownloadFileAsync(new Uri(url), tempFilePath);

            // Remove the tableObject from the fileDownloads and add it to the fileDownloaders
            DataManager.fileDownloads.Remove(this);
            DataManager.fileDownloaders.Add(Uuid, webClient);
        }

        private void DownloadFileWebClient_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            DataManager.ReportFileDownloadProgress(Uuid, e.ProgressPercentage);
        }

        private async void DownloadFileWebClient_DownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            if (e.Cancelled || e.Error != null)
                DataManager.ReportFileDownloadProgress(Uuid, -1);
            else
            {
                await CopyDownloadedFileAsync();
                DataManager.ReportFileDownloadProgress(Uuid, 101);
            }

            // Remove the file download progress list from the dictionary
            DataManager.fileDownloadProgressList.Remove(Uuid);
        }

        private async Task CopyDownloadedFileAsync()
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
            if (file.Exists)
                file.MoveTo(filePath);

            DataManager.fileDownloaders.Remove(Uuid);

            // Save the etags of the table object
            await DataManager.SetEtagOfTableObject(Uuid, Etag);

            ProjectInterface.TriggerAction.UpdateTableObject(this, true);
        }

        internal async Task<string> CreateOnServerAsync()
        {
            if (!ProjectInterface.GeneralMethods.IsNetworkAvailable()) return null;

            try
            {
                if (IsFile && File == null) return null;
                string jwt = DavUser.GetJWT();
                if (string.IsNullOrEmpty(jwt)) return null;
                
                string url = "apps/object?uuid=" + Uuid + "&app_id=" + Dav.AppId + "&table_id=" + TableId;
                HttpClient httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(60)
                };

                var headers = httpClient.DefaultRequestHeaders;
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(jwt);

                HttpContent content;

                if (IsFile)
                {
                    // Set the Content-Type header
                    string ext = GetPropertyValue("ext");
                    httpClient.DefaultRequestHeaders.Add("CONTENT_TYPE", MimeTypeMap.GetMimeType(ext));

                    // Upload the file
                    byte[] bytesFile = DataManager.FileToByteArray(File.FullName);
                    content = new ByteArrayContent(bytesFile);
                    if (bytesFile == null) return null;

                    if (!string.IsNullOrEmpty(ext))
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
                        await SetPropertyValueAsync(property.Key, property.Value);

                    return tableObjectData.etag;
                }
                else
                {
                    // Check for the error
                    if (httpResponseBody.Contains("2704"))  // Field already taken: uuid
                    {
                        // Set the upload status to UpToDate
                        await SetUploadStatusAsync(TableObjectUploadStatus.UpToDate);
                    }

                    return null;
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        internal async Task<string> UpdateOnServerAsync()
        {
            if (!ProjectInterface.GeneralMethods.IsNetworkAvailable()) return null;

            try
            {
                if (IsFile && File == null) return null;
                string jwt = DavUser.GetJWT();
                if (string.IsNullOrEmpty(jwt)) return null;
                
                string url = "apps/object/" + Uuid;
                HttpClient httpClient = new HttpClient
                {
                    Timeout = TimeSpan.FromMinutes(60)
                };

                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(jwt);
                Uri requestUri = new Uri(Dav.ApiBaseUrl + url);

                HttpContent content;

                if (IsFile)
                {
                    // Set the Content-Type header
                    string ext = GetPropertyValue("ext");
                    httpClient.DefaultRequestHeaders.Add("CONTENT_TYPE", MimeTypeMap.GetMimeType(ext));

                    // Upload the file
                    byte[] bytesFile = DataManager.FileToByteArray(File.FullName);
                    if (bytesFile == null) return null;
                    content = new ByteArrayContent(bytesFile);

                    if (!string.IsNullOrEmpty(ext))
                        url += "&ext=" + ext;
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
                        await DeleteImmediatelyAsync();
                    }

                    return null;
                }
                else
                {
                    TableObjectData tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(httpResponseBody);
                    return tableObjectData.etag;
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return null;
            }
        }

        internal async Task<bool> DeleteOnServerAsync()
        {
            if (!ProjectInterface.GeneralMethods.IsNetworkAvailable()) return false;

            try
            {
                string jwt = DavUser.GetJWT();
                if (string.IsNullOrEmpty(jwt)) return false;

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
                        return true;
                    else if (httpResponseBody.Contains("1102"))    // Action not allowed
                        return true;
                    else
                        return false;
                }
            }
            catch(Exception e)
            {
                Debug.WriteLine(e.Message);
                return false;
            }
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
                tableObjectData.properties.Add(property.Name, property.Value);

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
