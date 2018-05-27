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
            Updated = 1,
            New = 2,
            NoUpload
        }

        public TableObject(){}
        
        public TableObject(int tableId)
        {
            Uuid = Guid.NewGuid();
            TableId = tableId;
            Properties = new List<Property>();

            Save();
        }

        public TableObject(Guid uuid, int tableId)
        {
            Uuid = uuid;
            TableId = tableId;
            Properties = new List<Property>();

            Save();
        }

        public TableObject(Guid uuid, int tableId, List<Property> properties)
        {
            Uuid = uuid;
            TableId = tableId;
            Properties = properties;

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
            UploadStatus = TableObjectUploadStatus.New;
            Dav.Database.CreateTableObjectWithProperties(this);
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

            if(!String.IsNullOrEmpty(file.Extension))
                SetPropertyValue("ext", file.Extension.Replace(".", ""));
            IsFile = true;
            if (UploadStatus == TableObjectUploadStatus.UpToDate)
                UploadStatus = TableObjectUploadStatus.Updated;
            Save();

            // Save the file in the data folder with the uuid as name (without extension)
            string filename = Uuid.ToString();
            var tableFolder = DavDatabase.GetTableFolder(TableId);
            File = file.CopyTo(Path.Combine(tableFolder.FullName, filename), true);
            return File;
        }

        public void SetUploadStatus(TableObjectUploadStatus newUploadStatus)
        {
            if (UploadStatus == newUploadStatus) return;

            UploadStatus = newUploadStatus;
            Save();
        }

        public static async Task Sync()
        {
            List<TableObject> tableObjectsList = new List<TableObject>();

            // Get app
            string appInformation = await DavDatabase.HttpGet(DavUser.GetJWT(), "apps/app/" + Dav.AppId);

            // Create app object
            var app = DavDatabase.DeserializeJsonToApp(appInformation);

            // Get tables of the app
            foreach(var tableData in app.tables)
            {
                string tableInformation = await DavDatabase.HttpGet(DavUser.GetJWT(), "apps/table?app_id=" + app.id + "&table_name=" + tableData.name);
                var table = DavDatabase.DeserializeJsonToTable(tableInformation);

                // Get the objects of the table
                foreach(var obj in table.entries)
                {
                    // Get the proper table object
                    string tableObjectInformation = await DavDatabase.HttpGet(DavUser.GetJWT(), "apps/object?uuid=" + obj.uuid);
                    var tableObject = DavDatabase.DeserializeJsonToTableObject(tableObjectInformation);

                    // Save the table object in the database
                    foreach(var property in tableObject.properties)
                    {
                        
                    }
                }
            }

            // Get table objects of the tables

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
                uuid = Uuid.ToString(),
                file = IsFile,
                properties = new List<PropertyData>()
            };

            foreach(var property in Properties)
            {
                tableObjectData.properties.Add(property.ToPropertyData());
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
                Uuid = DavDatabase.ConvertStringToGuid(tableObjectData.uuid),
                IsFile = tableObjectData.file
            };

            List<Property> properties = new List<Property>();

            foreach(var propertyData in tableObjectData.properties)
            {
                properties.Add(Property.ConvertPropertyDataToProperty(propertyData));
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
        public string uuid { get; set; }
        public bool file { get; set; }
        public List<PropertyData> properties { get; set; }
    }
}
