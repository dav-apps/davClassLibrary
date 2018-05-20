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
        public int TableId { get; }
        private TableObjectVisibility _visibility;
        public TableObjectVisibility Visibility
        {
            get => _visibility;
            set
            {
                if (_visibility == value)
                    return;

                _visibility = value;
                Save();
            }
        }
        public Guid Uuid { get; }
        public bool IsFile { get; }
        private FileInfo _file;
        public FileInfo File
        {
            get => _file;
            set
            {
                if (_file == value)
                    return;

                _file = value;
                SaveFile(value);
                Save();
            }
        }
        [Ignore]
        public List<Property> Properties { get; }

        public enum TableObjectVisibility
        {
            Public,
            Protected,
            Private
        }

        public TableObject()
        {
            Uuid = Guid.NewGuid();
            Properties = new List<Property>();
        }

        public TableObject(Guid uuid)
        {
            if (uuid == null)
                Uuid = Guid.NewGuid();
            else
                Uuid = uuid;
        }

        public TableObject(Guid uuid, int tableId)
        {
            if (uuid == null)
                Uuid = Guid.NewGuid();
            else
                Uuid = uuid;

            TableId = tableId;
            Properties = new List<Property>();

            Save();
        }

        public TableObject(Guid uuid, int tableId, List<Property> properties)
        {
            if (uuid == null)
                Uuid = Guid.NewGuid();
            else
                Uuid = uuid;

            TableId = tableId;
            Properties = new List<Property>();
            foreach (var property in properties)
                Properties.Add(property);

            Save();
        }

        public TableObject(Guid uuid, int tableId, FileInfo file)
        {
            if (uuid == null)
                Uuid = Guid.NewGuid();
            else
                Uuid = uuid;

            TableId = tableId;
            _file = file;
            Properties = new List<Property>();

            // Copy file into the data folder
            _file = SaveFile(file);

            Save();
        }

        public TableObject(Guid uuid, int id, int tableId, TableObjectVisibility visibility, bool isFile)
        {
            if (uuid == null)
                Uuid = Guid.NewGuid();
            else
                Uuid = uuid;

            Id = id;
            TableId = tableId;
            Visibility = visibility;
            IsFile = isFile;
            Properties = new List<Property>();

            Save();
        }

        private void Save()
        {
            // Check if the tableObject already exists
            if (Dav.Database.GetTableObject(Uuid) == null)
                Id = Dav.Database.CreateTableObject(this);
            else
                Dav.Database.UpdateTableObject(this);
        }

        public void GetProperties(SQLiteConnection connection)
        {
            foreach(var property in connection.Table<Property>().Where(prop => prop.TableObjectId == Id))
            {
                if(!Properties.Contains(property))
                    Properties.Add(property);
            }
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
                    string tableObjectInformation = await DavDatabase.HttpGet(DavUser.GetJWT(), "apps/object/" + obj.id);
                    var tableObject = DavDatabase.DeserializeJsonToTableObject(tableObjectInformation);

                    // Save the table object in the database
                    foreach(var property in tableObject.properties)
                    {
                        
                    }
                }
            }

            // Get table objects of the tables

        }

        private FileInfo SaveFile(FileInfo file)
        {
            // Save the file in the data folder with the name uuid.ext
            string filename = Uuid.ToString() + file.Extension;
            var tableFolder = DavDatabase.GetTableFolder(TableId);
            return file.CopyTo(Path.Combine(tableFolder.FullName, filename), true);
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
    }

    public class TableObjectData
    {
        public int id { get; set; }
        public int table_id { get; set; }
        public int user_id { get; set; }
        public int visibility { get; set; }
        public string uuid { get; set; }
        public bool file { get; set; }
        public List<PropertyData> properties { get; set; }
    }
}
