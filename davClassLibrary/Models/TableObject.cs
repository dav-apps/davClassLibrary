using davClassLibrary.Controllers;
using davClassLibrary.DataAccess;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace davClassLibrary.Models
{
    public class TableObject
    {
        #region Variables
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TableId { get; set; }
        [NotNull]
        public Guid Uuid { get; set; }
        public bool IsFile { get; set; }
        [Ignore]
        public FileInfo File { get; set; }
        [Ignore]
        public List<Property> Properties { get; set; }
        public TableObjectUploadStatus UploadStatus { get; set; }
        public string Etag { get; set; }
        [Ignore]
        public TableObjectFileDownloadStatus FileDownloadStatus { get => GetFileDownloadStatus(); }
        #endregion

        #region Constructors & Create methods
        public TableObject() { }

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

        public TableObject(Guid uuid, int tableId, List<Property> properties, TableObjectUploadStatus uploadStatus)
        {
            Uuid = uuid;
            TableId = tableId;
            Properties = properties;
            UploadStatus = uploadStatus;
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
        #endregion

        #region UploadStatus methods
        public async Task SetUploadStatusAsync(TableObjectUploadStatus newUploadStatus)
        {
            if (UploadStatus == newUploadStatus) return;

            UploadStatus = newUploadStatus;
            await SaveAsync();
        }
        #endregion

        #region Etag methods
        internal async Task SetEtagAsync(string etag)
        {
            if (Etag == etag) return;

            Etag = etag;
            await SaveAsync();
        }
        #endregion

        #region Properties methods
        private async Task LoadPropertiesAsync()
        {
            Properties = await Dav.Database.GetPropertiesOfTableObjectAsync(Id);
        }

        public async Task SetPropertyValueAsync(string name, string value)
        {
            var property = Properties.Find(prop => prop.Name == name);

            if (property != null)
            {
                // Update the property
                if (property.Value == value) return;
                await property.SetValueAsync(value);
            }
            else
            {
                // Create a new property
                Properties.Add(await Property.CreateAsync(Id, name, value));
            }

            if (UploadStatus == TableObjectUploadStatus.UpToDate && !IsFile)
                UploadStatus = TableObjectUploadStatus.Updated;

            await SaveAsync();
            _ = SyncManager.SyncPush();
        }

        public async Task SetPropertyValuesAsync(Dictionary<string, string> properties)
        {
            bool propertiesChanged = false;
            List<Property> propertiesToCreate = new List<Property>();
            List<Property> propertiesToUpdate = new List<Property>();

            foreach (var p in properties)
            {
                var property = Properties.Find(prop => prop.Name == p.Key);

                if (property != null)
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
            _ = SyncManager.SyncPush();
        }

        public string GetPropertyValue(string name)
        {
            var property = Properties.Find(prop => prop.Name == name);
            return property?.Value;
        }

        public async Task RemovePropertyAsync(string name)
        {
            var property = Properties.Find(prop => prop.Name == name);
            if (property == null) return;
            Properties.Remove(property);

            if (UploadStatus == TableObjectUploadStatus.UpToDate)
                UploadStatus = TableObjectUploadStatus.Updated;

            await Dav.Database.DeletePropertyAsync(property);
            _ = SyncManager.SyncPush();
        }
        #endregion

        #region File methods
        public bool FileDownloaded()
        {
            return System.IO.File.Exists(GetFilePath());
        }

        private string GetFilePath()
        {
            return Path.Combine(Utils.GetTableFolder(TableId).FullName, Uuid.ToString());
        }

        private string GetTempFilePath()
        {
            return Path.Combine(Utils.GetTempTableFolder(TableId).FullName, Uuid.ToString());
        }

        internal void LoadFile()
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
            var tableFolder = Utils.GetTableFolder(TableId);
            File = file.CopyTo(Path.Combine(tableFolder.FullName, filename), true);

            if (!string.IsNullOrEmpty(file.Extension))
                await SetPropertyValueAsync(Constants.extPropertyName, file.Extension.Replace(".", ""));

            await SaveAsync();
            return File;
        }

        internal void DeleteFile()
        {
            if (!IsFile || File == null) return;

            // Delete the file
            try
            {
                File.Delete();
            }
            catch (Exception) { }
        }

        private TableObjectFileDownloadStatus GetFileDownloadStatus()
        {
            if (!IsFile) return TableObjectFileDownloadStatus.NoFileOrNotLoggedIn;
            if (File != null && System.IO.File.Exists(File.FullName)) return TableObjectFileDownloadStatus.Downloaded;
            if (!Dav.IsLoggedIn) return TableObjectFileDownloadStatus.NoFileOrNotLoggedIn;

            if (SyncManager.downloadingFileUuid.Equals(Uuid)) return TableObjectFileDownloadStatus.Downloading;
            return TableObjectFileDownloadStatus.NotDownloaded;
        }

        public void ScheduleFileDownload(IProgress<(Guid, int)> progress)
        {
            var downloadStatus = GetFileDownloadStatus();

            // Check the download status
            if (
                downloadStatus == TableObjectFileDownloadStatus.NoFileOrNotLoggedIn
                || downloadStatus == TableObjectFileDownloadStatus.Downloaded
            ) return;

            if (progress != null)
            {
                if (!SyncManager.fileDownloadProgressList.ContainsKey(Uuid))
                {
                    // Add the progress to the progress list
                    SyncManager.fileDownloadProgressList.Add(Uuid, new List<IProgress<(Guid, int)>>());
                }

                SyncManager.fileDownloadProgressList[Uuid].Add(progress);
            }

            if (downloadStatus == TableObjectFileDownloadStatus.Downloading) return;

            // DownloadStatus is NotDownloaded
            // Check if fileDownloads contains this TableObject
            int i = SyncManager.fileDownloads.FindIndex(obj => obj.uuid.Equals(Uuid));
            if (i != -1)
            {
                // Move the item to the beginning of the list
                var item = SyncManager.fileDownloads[i];

                SyncManager.fileDownloads.RemoveAt(i);
                SyncManager.fileDownloads.Insert(0, item);
            }
            else
            {
                // Add the object at the beginning of the list
                SyncManager.fileDownloads.Insert(0, new TableObjectDownload { uuid = Uuid });
            }

            SyncManager.StartFileDownloads();
        }

        public async Task<bool> DownloadFile()
        {
            if (GetFileDownloadStatus() != TableObjectFileDownloadStatus.NotDownloaded)
                return false;

            if (!SyncManager.downloadingFileUuid.Equals(Guid.Empty)) return false;
            SyncManager.SetDownloadingFileUuid(Uuid);

            var progress = new Progress<int>((int value) => SyncManager.ReportFileDownloadProgress(Uuid, value));
            var response = new ApiResponse();
            await Task.Run(async () =>
            {
                response = await TableObjectsController.GetTableObjectFile(Uuid, GetTempFilePath(), progress);
            });

            if (response.Success)
            {
                try
                {
                    // Move the file into the table folder
                    var tempFile = new FileInfo(GetTempFilePath());
                    tempFile.MoveTo(GetFilePath());

                    File = new FileInfo(GetFilePath());

                    SyncManager.ReportFileDownloadProgress(Uuid, 101);
                    SyncManager.SetDownloadingFileUuid(Guid.Empty);
                    return true;
                }
                catch (Exception)
                {
                    SyncManager.ReportFileDownloadProgress(Uuid, -1);
                }
            }
            else
            {
                SyncManager.ReportFileDownloadProgress(Uuid, -1);
            }

            SyncManager.SetDownloadingFileUuid(Guid.Empty);
            return false;
        }
        #endregion

        #region General methods
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

            _ = SyncManager.SyncPush();
        }

        public async Task DeleteAsync()
        {
            if (!Dav.IsLoggedIn)
            {
                await DeleteImmediatelyAsync();
                UploadStatus = TableObjectUploadStatus.Deleted;
            }
            else
            {
                await SetUploadStatusAsync(TableObjectUploadStatus.Deleted);
                _ = SyncManager.SyncPush();
            }
        }

        public async Task DeleteImmediatelyAsync()
        {
            DeleteFile();
            await Dav.Database.DeleteTableObjectImmediatelyAsync(Uuid);
        }

        public TableObjectData ToTableObjectData()
        {
            var tableObjectData = new TableObjectData
            {
                table_id = TableId,
                uuid = Uuid,
                file = IsFile,
                etag = Etag,
                properties = new Dictionary<string, string>()
            };

            foreach (var property in Properties)
                tableObjectData.properties.Add(property.Name, property.Value);

            return tableObjectData;
        }
        #endregion
    }

    public class TableObjectData
    {
        public int table_id { get; set; }
        public Guid uuid { get; set; }
        public bool file { get; set; }
        public string etag { get; set; }
        public string table_etag { get; set; }
        public Dictionary<string, string> properties { get; set; }

        public TableObject ToTableObject()
        {
            TableObject tableObject = new TableObject
            {
                TableId = table_id,
                Uuid = uuid,
                IsFile = file,
                Etag = etag,
                Properties = new List<Property>()
            };

            if (properties != null)
            {
                foreach (var propertyData in properties)
                {
                    tableObject.Properties.Add(new Property
                    {
                        Name = propertyData.Key,
                        Value = propertyData.Value
                    });
                }
            }

            return tableObject;
        }
    }
}
