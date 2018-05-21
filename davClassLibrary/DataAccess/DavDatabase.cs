using davClassLibrary.Models;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using static davClassLibrary.Models.SyncObject;

namespace davClassLibrary.DataAccess
{
    public class DavDatabase
    {
        readonly SQLiteConnection database;
        private readonly string databaseName = "dav.db";

        public DavDatabase()
        {
            database = new SQLiteConnection(Dav.DataPath + databaseName);
            database.CreateTable<TableObject>();
            database.CreateTable<Property>();
            database.CreateTable<SyncTableObject>();
            database.CreateTable<SyncProperty>();
        }

        #region CRUD for TableObject
        public int CreateTableObject(TableObject tableObject)
        {
            int id = database.Insert(tableObject);
            CreateSyncTableObject(new SyncTableObject(tableObject.Uuid, SyncOperation.Create));

            // Save the properties
            foreach (var property in tableObject.Properties)
                CreateProperty(property);

            return id;
        }

        public List<TableObject> GetAllTableObjects(int tableId)
        {
            var tableObjects = database.Table<TableObject>().Where(obj => obj.TableId == tableId);
            List<TableObject> tableObjectsList = new List<TableObject>();

            // Get the properties of the table objects
            foreach(var tableObject in tableObjects)
            {
                tableObject.GetProperties(database);
                tableObjectsList.Add(tableObject);
            }

            return tableObjectsList;
        }

        public TableObject GetTableObject(int id)
        {
            var tableObject = database.Get<TableObject>(id);
            tableObject.GetProperties(database);
            return tableObject;
        }

        public TableObject GetTableObject(Guid uuid)
        {
            var tableObject = database.Get<TableObject>(obj => obj.Uuid == uuid);
            if(tableObject != null)
                tableObject.GetProperties(database);

            return tableObject;
        }

        public void UpdateTableObject(TableObject tableObject)
        {
            database.Update(tableObject);
            CreateSyncTableObject(new SyncTableObject(tableObject.Uuid, SyncOperation.Update));
        }

        public void DeleteTableObject(Guid uuid)
        {
            var tableObject = GetTableObject(uuid);
            if(tableObject != null)
                DeleteTableObject(tableObject);
        }

        public void DeleteTableObject(TableObject tableObject)
        {
            // Delete the properties of the table object
            foreach(var property in tableObject.Properties)
                DeleteProperty(property);

            database.Delete(tableObject);
            CreateSyncTableObject(new SyncTableObject(tableObject.Uuid, SyncOperation.Delete));
        }
        #endregion

        #region CRUD for Property
        public int CreateProperty(Property property)
        {
            int id = database.Insert(property);
            CreateSyncProperty(new SyncProperty(property.Id, SyncOperation.Create));
            return id;
        }

        public Property GetProperty(int id)
        {
            return database.Get<Property>(id);
        }

        public void UpdateProperty(Property property)
        {
            database.Update(property);
            CreateSyncProperty(new SyncProperty(property.Id, SyncOperation.Update));
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
            CreateSyncProperty(new SyncProperty(property.Id, SyncOperation.Delete));
        }
        #endregion

        #region CRD for SyncTableObject
        public int CreateSyncTableObject(SyncTableObject syncTableObject)
        {
            return database.Insert(syncTableObject);
        }

        public SyncTableObject GetSyncTableObject(int id)
        {
            return database.Get<SyncTableObject>(id);
        }

        public void DeleteSyncTableObject(int id)
        {
            // Get SyncTableObject from the database
            var syncTableObject = GetSyncTableObject(id);

            if(syncTableObject != null)
                database.Delete(syncTableObject);
        }

        public void DeleteSyncTableObject(SyncTableObject syncTableObject)
        {
            database.Delete(syncTableObject);
        }
        #endregion

        #region CRD for SyncProperty
        public int CreateSyncProperty(SyncProperty syncProperty)
        {
            return database.Insert(syncProperty);
        }

        public SyncProperty GetSyncProperty(int id)
        {
            return database.Get<SyncProperty>(id);
        }

        public void DeleteSyncProperty(int id)
        {
            var syncProperty = GetSyncProperty(id);

            if (syncProperty != null)
                database.Delete(syncProperty);
        }

        public void DeleteSyncProperty(SyncProperty syncProperty)
        {
            database.Delete(syncProperty);
        }
        #endregion


        #region Static things
        public static async Task<string> HttpGet(string jwt, string serverUrl)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                HttpClient httpClient = new HttpClient();
                var headers = httpClient.DefaultRequestHeaders;
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(jwt);
                Uri requestUri = new Uri(Dav.ApiBaseUrl + serverUrl);

                HttpResponseMessage httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";

                //Send the GET request
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    return httpResponseBody;
                }
                else
                {
                    // Return error message / Throw exception
                    return null;
                }
            }
            else
            {
                // Return error message / Throw exception
                return null;
            }
        }

        public static AppData DeserializeJsonToApp(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(AppData));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return (AppData)serializer.ReadObject(ms);
        }

        public static TableData DeserializeJsonToTable(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(TableData));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return (TableData)serializer.ReadObject(ms);
        }

        public static TableObjectData DeserializeJsonToTableObject(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(TableObjectData));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return (TableObjectData)serializer.ReadObject(ms);
        }

        public static DirectoryInfo GetTableFolder(int tableId)
        {
            DirectoryInfo dataFolder = new DirectoryInfo(Dav.DataPath);
            if(Directory.Exists(Path.Combine(dataFolder.FullName, tableId.ToString())))
            {
                // Return the folder
                return Directory.GetParent(Path.Combine(dataFolder.FullName, tableId.ToString()));
            }
            else
            {
                // Create the folder and return it
                return dataFolder.CreateSubdirectory(tableId.ToString());
            }
        }
        #endregion
    }
}
