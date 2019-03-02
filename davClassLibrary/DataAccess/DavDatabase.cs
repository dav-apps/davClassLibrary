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
        readonly SQLiteConnection syncDatabase;
        private readonly string databaseName = "dav.db";

        public DavDatabase()
        {
            database = new SQLiteAsyncConnection(Path.Combine(Dav.DataPath, databaseName));
            syncDatabase = new SQLiteConnection(Path.Combine(Dav.DataPath, databaseName));
            syncDatabase.CreateTable<TableObject>();
            syncDatabase.CreateTable<Property>();
        }
        
        public async Task DropAsync()
        {
            await database.DropTableAsync<TableObject>();
            await database.DropTableAsync<Property>();
            await database.CreateTableAsync<TableObject>();
            await database.CreateTableAsync<Property>();
        }

        #region CRUD for TableObject
        public async Task<int> CreateTableObjectAsync(TableObject tableObject)
        {
            await database.InsertAsync(tableObject);
            return tableObject.Id;
        }

        public async Task<int> CreateTableObjectWithPropertiesAsync(TableObject tableObject)
        {
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
            List<TableObject> tableObjectList = new List<TableObject>();
            List<TableObject> tableObjects = await database.Table<TableObject>().ToListAsync();

            foreach (var tableObject in tableObjects)
            {
                if (!deleted && tableObject.UploadStatus == TableObject.TableObjectUploadStatus.Deleted) continue;

                await tableObject.LoadAsync();
                tableObjectList.Add(tableObject);
            }
            
            return tableObjectList;
        }

        public async Task<List<TableObject>> GetAllTableObjectsAsync(int tableId, bool deleted)
        {
            List<TableObject> tableObjectsList = new List<TableObject>();
            List<TableObject> tableObjects = await database.Table<TableObject>().ToListAsync();
            
            // Get the properties of the table objects
            foreach (var tableObject in tableObjects)
            {
                if ((!deleted &&
                    tableObject.UploadStatus == TableObject.TableObjectUploadStatus.Deleted) ||
                    tableObject.TableId != tableId) continue;

                await tableObject.LoadAsync();
                tableObjectsList.Add(tableObject);
            }

            return tableObjectsList;
        }
        
        public async Task<TableObject> GetTableObjectAsync(Guid uuid)
        {
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

        public async Task<List<Property>> GetPropertiesOfTableObjectAsync(int tableObjectId)
        {
            List<Property> allProperties = await database.QueryAsync<Property>("SELECT * FROM Property WHERE TableObjectId = ?", tableObjectId);
            return allProperties;
        }

        public async Task<bool> TableObjectExistsAsync(Guid uuid)
        {
            return (await database.QueryAsync<TableObject>("SELECT * FROM TableObject WHERE Uuid = ?", uuid)).Count > 0;
        }

        public async Task UpdateTableObjectAsync(TableObject tableObject)
        {
            await database.UpdateAsync(tableObject);
        }

        public async Task DeleteTableObjectAsync(Guid uuid)
        {
            TableObject tableObject = await GetTableObjectAsync(uuid);
            if(tableObject != null)
                await DeleteTableObjectAsync(tableObject);
        }

        public async Task DeleteTableObjectAsync(TableObject tableObject)
        {
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
            TableObject tableObject = await GetTableObjectAsync(uuid);
            if (tableObject != null)
                await DeleteTableObjectImmediatelyAsync(tableObject);
        }

        public async Task DeleteTableObjectImmediatelyAsync(TableObject tableObject)
        {
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
            await database.InsertAsync(property);
            return property.Id;
        }

        public async Task CreatePropertiesAsync(List<Property> propertiesToCreate)
        {
            await database.InsertAllAsync(propertiesToCreate);
        }

        public async Task<Property> GetPropertyAsync(int id)
        {
            List<Property> properties = await database.QueryAsync<Property>("SELECT * FROM Property WHERE Id = ?", id);
            
            if (properties.Count == 0)
                return null;
            else
                return properties.First();
        }

        public async Task<bool> PropertyExistsAsync(int id)
        {
            return (await database.QueryAsync<Property>("SELECT * FROM Property WHERE Id = ?", id)).Count > 0;
        }

        public async Task UpdatePropertyAsync(Property property)
        {
            await database.UpdateAsync(property);
        }

        public async Task UpdatePropertiesAsync(List<Property> propertiesToUpdate)
        {
            await database.UpdateAllAsync(propertiesToUpdate);
        }

        public async Task DeletePropertyAsync(int id)
        {
            var property = await GetPropertyAsync(id);
            if (property != null)
                await DeletePropertyAsync(property);
        }

        public async Task DeletePropertyAsync(Property property)
        {
            await database.DeleteAsync(property);
        }
        #endregion
    }
}
