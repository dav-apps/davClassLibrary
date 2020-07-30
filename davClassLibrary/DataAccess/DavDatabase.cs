using davClassLibrary.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace davClassLibrary.DataAccess
{
    public class DavDatabase
    {
        readonly SQLiteAsyncConnection database;
        bool databaseInitialized = false;
        private readonly string databaseName = "dav.db";

        public DavDatabase()
        {
            database = new SQLiteAsyncConnection(Path.Combine(Dav.DataPath, databaseName));
        }

        public async Task InitAsync()
        {
            if(!databaseInitialized)
            {
                databaseInitialized = true;
                await database.CreateTableAsync<TableObject>();
                await database.CreateTableAsync<Property>();
            }
        }
        
        public async Task DropAsync()
        {
            await InitAsync();
            await database.DropTableAsync<TableObject>();
            await database.DropTableAsync<Property>();
            await database.CreateTableAsync<TableObject>();
            await database.CreateTableAsync<Property>();
        }

        #region CRUD for TableObject
        public async Task<int> CreateTableObjectAsync(TableObject tableObject)
        {
            await InitAsync();
            await database.InsertAsync(tableObject);
            return tableObject.Id;
        }

        public async Task<int> CreateTableObjectWithPropertiesAsync(TableObject tableObject)
        {
            await InitAsync();
            await database.RunInTransactionAsync(tran =>
            {
                tran.Insert(tableObject);

                foreach (Property property in tableObject.Properties)
                {
                    property.TableObjectId = tableObject.Id;
                    tran.Insert(property);
                }
            });

            return tableObject.Id;
        }
        
        public async Task<List<TableObject>> GetAllTableObjectsAsync(bool deleted)
        {
            await InitAsync();
            List<TableObject> tableObjectList = new List<TableObject>();
            List<TableObject> tableObjects = await database.Table<TableObject>().ToListAsync();
            List<Property> properties = await database.Table<Property>().ToListAsync();

            foreach (var tableObject in tableObjects)
            {
                if (!deleted && tableObject.UploadStatus == TableObject.TableObjectUploadStatus.Deleted) continue;

                tableObject.Properties = new List<Property>();
                foreach (var prop in properties.FindAll(p => p.TableObjectId == tableObject.Id))
                    tableObject.Properties.Add(prop);

                tableObject.LoadFile();
                tableObjectList.Add(tableObject);
            }
            
            return tableObjectList;
        }

        public async Task<List<TableObject>> GetAllTableObjectsAsync(int tableId, bool deleted)
        {
            await InitAsync();
            List<TableObject> tableObjectsList = new List<TableObject>();
            List<TableObject> tableObjects = await database.Table<TableObject>().ToListAsync();
            List<Property> properties = await database.Table<Property>().ToListAsync();
            
            // Get the properties of the table objects
            foreach (var tableObject in tableObjects)
            {
                if (
                    (!deleted && tableObject.UploadStatus == TableObject.TableObjectUploadStatus.Deleted)
                    || tableObject.TableId != tableId
                ) continue;

                tableObject.Properties = new List<Property>();
                foreach (var prop in properties.FindAll(p => p.TableObjectId == tableObject.Id))
                    tableObject.Properties.Add(prop);

                tableObject.LoadFile();
                tableObjectsList.Add(tableObject);
            }

            return tableObjectsList;
        }

        public async Task<List<TableObject>> GetTableObjectsByPropertyAsync(string propertyName, string propertyValue)
        {
            await InitAsync();

            // Find all properties with the name and value
            List<TableObject> tableObjects = new List<TableObject>();
            List<int> tableObjectIds = new List<int>();
            List<Property> properties = await database.QueryAsync<Property>("SELECT * FROM Property WHERE name = ? and value = ?", propertyName, propertyValue);

            // Get the table objects of the properties
            foreach(var property in properties)
            {
                // Check if the table objects list already contains the table object of the property
                if (tableObjectIds.Contains(property.TableObjectId)) continue;

                // Get the table object
                var tableObject = await GetTableObjectAsync(property.TableObjectId);
                if (tableObject == null) continue;

                tableObjects.Add(tableObject);
            }

            return tableObjects;
        }
        
        public async Task<TableObject> GetTableObjectAsync(Guid uuid)
        {
            await InitAsync();
            List<TableObject> tableObjects = await database.QueryAsync<TableObject>("SELECT * FROM TableObject WHERE Uuid = ?", uuid);
            if (tableObjects.Count == 0)
                return null;
            else
            {
                var tableObject = tableObjects.First();
                await tableObject.LoadAsync();
                return tableObject;
            }
        }

        public async Task<TableObject> GetTableObjectAsync(int id)
        {
            await InitAsync();
            List<TableObject> tableObjects = await database.QueryAsync<TableObject>("SELECT * FROM TableObject WHERE Id = ?", id);
            if (tableObjects.Count == 0)
                return null;
            else
            {
                var tableObject = tableObjects.First();
                await tableObject.LoadAsync();
                return tableObject;
            }
        }

        public async Task<List<Property>> GetPropertiesOfTableObjectAsync(int tableObjectId)
        {
            await InitAsync();
            List<Property> allProperties = await database.QueryAsync<Property>("SELECT * FROM Property WHERE TableObjectId = ?", tableObjectId);
            return allProperties;
        }

        public async Task<bool> TableObjectExistsAsync(Guid uuid)
        {
            await InitAsync();
            return (await database.QueryAsync<TableObject>("SELECT * FROM TableObject WHERE Uuid = ?", uuid)).Count > 0;
        }

        public async Task UpdateTableObjectAsync(TableObject tableObject)
        {
            await InitAsync();
            await database.UpdateAsync(tableObject);
        }

        public async Task DeleteTableObjectAsync(Guid uuid)
        {
            await InitAsync();
            TableObject tableObject = await GetTableObjectAsync(uuid);
            if(tableObject != null)
                await DeleteTableObjectAsync(tableObject);
        }

        public async Task DeleteTableObjectAsync(TableObject tableObject)
        {
            await InitAsync();
            if (tableObject.UploadStatus == TableObject.TableObjectUploadStatus.Deleted)
            {
                await DeleteTableObjectImmediatelyAsync(tableObject);
            }
            else
            {
                // Set the upload status of the table object to Deleted
                await tableObject.SetUploadStatusAsync(TableObject.TableObjectUploadStatus.Deleted);
            }
        }

        public async Task DeleteTableObjectImmediatelyAsync(Guid uuid)
        {
            await InitAsync();
            TableObject tableObject = await GetTableObjectAsync(uuid);
            if (tableObject != null)
                await DeleteTableObjectImmediatelyAsync(tableObject);
        }

        public async Task DeleteTableObjectImmediatelyAsync(TableObject tableObject)
        {
            await InitAsync();
            await tableObject.LoadAsync();
            await database.RunInTransactionAsync((SQLiteConnection tran) =>
            {
                // Delete the properties of the table object
                foreach (var property in tableObject.Properties)
                    tran.Delete(property);

                tran.Delete(tableObject);
            });
        }
        #endregion

        #region CRUD for Property
        public async Task<int> CreatePropertyAsync(Property property)
        {
            await InitAsync();
            await database.InsertAsync(property);
            return property.Id;
        }

        public async Task CreatePropertiesAsync(List<Property> propertiesToCreate)
        {
            await InitAsync();
            await database.InsertAllAsync(propertiesToCreate);
        }

        public async Task<Property> GetPropertyAsync(int id)
        {
            await InitAsync();
            List<Property> properties = await database.QueryAsync<Property>("SELECT * FROM Property WHERE Id = ?", id);
            
            if (properties.Count == 0)
                return null;
            else
                return properties.First();
        }

        public async Task<bool> PropertyExistsAsync(int id)
        {
            await InitAsync();
            return (await database.QueryAsync<Property>("SELECT * FROM Property WHERE Id = ?", id)).Count > 0;
        }

        public async Task UpdatePropertyAsync(Property property)
        {
            await InitAsync();
            await database.UpdateAsync(property);
        }

        public async Task UpdatePropertiesAsync(List<Property> propertiesToUpdate)
        {
            await InitAsync();
            await database.UpdateAllAsync(propertiesToUpdate);
        }

        public async Task DeletePropertyAsync(int id)
        {
            await InitAsync();
            var property = await GetPropertyAsync(id);
            if (property != null)
                await DeletePropertyAsync(property);
        }

        public async Task DeletePropertyAsync(Property property)
        {
            await InitAsync();
            await database.DeleteAsync(property);
        }
        #endregion
    }
}
