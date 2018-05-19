using PCLStorage;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace davClassLibrary.Models
{
    public class TableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; }
        public int TableId { get; }
        private TableObjectVisibility _visibility;
        public TableObjectVisibility Visibility
        {
            get => _visibility;
            set { UpdateVisibility(value); }
        }
        public Guid Uuid { get; }
        public bool IsFile { get; }
        private IFile _file;
        public IFile File
        {
            get => _file;
            set { UpdateFile(value); }
        }
        public List<Property> Properties { get; }

        public enum TableObjectVisibility
        {
            Public,
            Protected,
            Private
        }

        public TableObject()
        {
            Properties = new List<Property>();
        }

        public TableObject(int id, int tableId, TableObjectVisibility visibility, Guid uuid, bool isFile)
        {
            Id = id;
            TableId = tableId;
            Visibility = visibility;
            Uuid = uuid;
            IsFile = isFile;
            Properties = new List<Property>();
        }


        private void UpdateVisibility(TableObjectVisibility visibility)
        {
            // Update the property
            _visibility = visibility;

            // Add object to sync table if it is not there
            AddToSyncTable();
        }

        private void UpdateFile(IFile file)
        {
            _file = file;

            // Upload the new file
            // TODO
        }

        private void AddToSyncTable()
        {

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
