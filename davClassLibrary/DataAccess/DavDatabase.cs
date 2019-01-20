using davClassLibrary.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace davClassLibrary.DataAccess
{
    public class DavDatabase
    {
        readonly SQLiteConnection database;
        private readonly string databaseName = "dav.db";

        public DavDatabase()
        {
            database = new SQLiteConnection(Path.Combine(Dav.DataPath, databaseName));
            database.CreateTable<TableObject>();
            database.CreateTable<Property>();
        }

        public void Drop()
        {
            database.DropTable<TableObject>();
            database.DropTable<Property>();
            database.CreateTable<TableObject>();
            database.CreateTable<Property>();
        }

        #region CRUD for TableObject
        public int CreateTableObject(TableObject tableObject)
        {
            database.Insert(tableObject);
            return tableObject.Id;
        }

        public int CreateTableObjectWithProperties(TableObject tableObject)
        {
            database.RunInTransaction(() =>
            {
                database.Insert(tableObject);

                foreach (var property in tableObject.Properties)
                {
                    property.TableObjectId = tableObject.Id;
                    database.Insert(property);
                }
            });

            return tableObject.Id;
        }

        public List<TableObject> GetAllTableObjects(bool deleted)
        {
            List<TableObject> tableObjectList = new List<TableObject>();
            List<TableObject> tableObjects = database.Table<TableObject>().ToList();

            foreach (var tableObject in tableObjects)
            {
                if (!deleted && tableObject.UploadStatus == TableObject.TableObjectUploadStatus.Deleted) continue;

                tableObject.Load();
                tableObjectList.Add(tableObject);
            }
            
            return tableObjectList;
        }

        public List<TableObject> GetAllTableObjects(int tableId, bool deleted)
        {
            List<TableObject> tableObjectsList = new List<TableObject>();
            List<TableObject> tableObjects = database.Table<TableObject>().ToList();
            
            // Get the properties of the table objects
            foreach (var tableObject in tableObjects)
            {
                if ((!deleted &&
                    tableObject.UploadStatus == TableObject.TableObjectUploadStatus.Deleted) ||
                    tableObject.TableId != tableId) continue;

                tableObject.Load();
                tableObjectsList.Add(tableObject);
            }

            return tableObjectsList;
        }
        
        public TableObject GetTableObject(Guid uuid)
        {
            List<TableObject> tableObjects = database.Query<TableObject>("SELECT * FROM TableObject WHERE Uuid = ?", uuid);
            if (tableObjects.Count == 0)
                return null;
            else
            {
                var tableObject = tableObjects.First();
                tableObject.Load();
                return tableObject;
            }
        }

        public List<Property> GetPropertiesOfTableObject(int tableObjectId)
        {
            List<Property> allProperties = database.Query<Property>("SELECT * FROM Property WHERE TableObjectId = ?", tableObjectId);
            return allProperties;
        }

        public bool TableObjectExists(Guid uuid)
        {
            return database.Query<TableObject>("SELECT * FROM TableObject WHERE Uuid = ?", uuid).Count > 0;
        }

        public void UpdateTableObject(TableObject tableObject)
        {
            database.Update(tableObject);
        }

        public void DeleteTableObject(Guid uuid)
        {
            TableObject tableObject = GetTableObject(uuid);
            if(tableObject != null)
            {
                DeleteTableObject(tableObject);
            }
        }

        public void DeleteTableObject(TableObject tableObject)
        {
            if (tableObject.UploadStatus == TableObject.TableObjectUploadStatus.Deleted)
            {
                DeleteTableObjectImmediately(tableObject);
            }
            else
            {
                // Set the upload status of the table object to Deleted
                tableObject.SetUploadStatus(TableObject.TableObjectUploadStatus.Deleted);
            }
        }

        public void DeleteTableObjectImmediately(Guid uuid)
        {
            TableObject tableObject = GetTableObject(uuid);
            if (tableObject != null)
            {
                DeleteTableObjectImmediately(tableObject);
            }
        }

        public void DeleteTableObjectImmediately(TableObject tableObject)
        {
            database.RunInTransaction(() =>
            {
                // Delete the properties of the table object
                tableObject.Load();
                foreach (var property in tableObject.Properties)
                {
                    database.Delete(property);
                }
                database.Delete(tableObject);
            });
        }
        #endregion

        #region CRUD for Property
        public int CreateProperty(Property property)
        {
            database.Insert(property);
            return property.Id;
        }

        public Property GetProperty(int id)
        {
            List<Property> properties = database.Query<Property>("SELECT * FROM Property WHERE Id = ?", id);
            
            if (properties.Count == 0)
                return null;
            else
                return properties.First();
        }

        public bool PropertyExists(int id)
        {
            return database.Query<Property>("SELECT * FROM Property WHERE Id = ?", id).Count > 0;
        }

        public void UpdateProperty(Property property)
        {
            database.Update(property);
        }

        public void DeleteProperty(int id)
        {
            var property = GetProperty(id);
            if (property != null)
                DeleteProperty(property);
        }

        public void DeleteProperty(Property property)
        {
            database.Delete(property);
        }
        #endregion
    }
}
