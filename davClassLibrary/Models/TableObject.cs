﻿using davClassLibrary.DataAccess;
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
        private List<Property> Properties { get; set; }

        public enum TableObjectVisibility
        {
            Public = 2,
            Protected = 1,
            Private = 0
        }

        public TableObject(){}
        /*
        public TableObject(Guid uuid)
        {
            if (Equals(uuid, Guid.Empty))
            {
                Uuid = Guid.NewGuid();
                
            }
            else
            {
                Uuid = uuid;
            }
            Properties = new List<Property>();
        }
        */
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
        /*
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
        }*/
        /*
        public TableObject(Guid uuid, int tableId, FileInfo file)
        {
            if (uuid == null)
                Uuid = Guid.NewGuid();
            else
                Uuid = uuid;

            TableId = tableId;
            IsFile = true;
            _file = file;
            Properties = new List<Property>();

            Save();

            // Copy file into the data folder
            _file = SaveFile(file);
        }
        */
        public void SetVisibility(TableObjectVisibility visibility)
        {
            Visibility = visibility;
            Save();
        }

        public void SetFile(FileInfo file)
        {
            File = file;
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
                Id = Dav.Database.CreateTableObject(this);
            else
                Dav.Database.UpdateTableObject(this);
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
                property.Value = value;
            }
            else
            {
                // Create a new property
                Properties.Add(new Property(Id, name, value));
            }
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

            if(property != null)
            {
                Dav.Database.DeleteProperty(property);
            }
        }

        public void RemoveAllProperties()
        {
            LoadProperties();
            foreach (var property in Properties)
                Dav.Database.DeleteProperty(property);
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
            SetPropertyValue("ext", file.Extension.Replace(".", ""));
            IsFile = true;
            Save();

            // Save the file in the data folder with the uuid as name (without extension)
            string filename = Uuid.ToString();
            var tableFolder = DavDatabase.GetTableFolder(TableId);
            return file.CopyTo(Path.Combine(tableFolder.FullName, filename), true);
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
