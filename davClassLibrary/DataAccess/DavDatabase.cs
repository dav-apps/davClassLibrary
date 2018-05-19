using davClassLibrary.Models;
using SQLite;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;
using static davClassLibrary.DavDatabase;
using static davClassLibrary.Models.SyncObject;

namespace davClassLibrary
{
    public class DavDatabase
    {
        readonly SQLiteAsyncConnection database;
        private string databaseName = "dav.db";

        public DavDatabase()
        {
            database = new SQLiteAsyncConnection(Dav.DataPath + databaseName);
            database.CreateTableAsync<TableObject>().Wait();
            database.CreateTableAsync<Property>().Wait();
            database.CreateTableAsync<SyncTableObject>().Wait();
            database.CreateTableAsync<SyncProperty>().Wait();
        }

        #region CRUD for TableObject
        public async Task<int> CreateTableObject(TableObject tableObject)
        {
            int id = await database.InsertAsync(tableObject);
            await CreateSyncTableObject(new SyncTableObject(tableObject.Uuid, SyncOperation.Create));
            return id;
        }

        public async Task<TableObject> GetTableObject(int id)
        {
            return await database.GetAsync<TableObject>(id);
        }

        public async Task UpdateTableObject(TableObject tableObject)
        {
            await database.UpdateAsync(tableObject);
            await CreateSyncTableObject(new SyncTableObject(tableObject.Uuid, SyncOperation.Update));
        }

        public async Task DeleteTableObject(TableObject tableObject)
        {
            await database.DeleteAsync(tableObject);
            await CreateSyncTableObject(new SyncTableObject(tableObject.Uuid, SyncOperation.Delete));
        }
        #endregion

        #region CRUD for Property
        public async Task<int> CreateProperty(Property property)
        {
            int id = await database.InsertAsync(property);
            await CreateSyncProperty(new SyncProperty(property.Id, SyncOperation.Create));
            return id;
        }

        public async Task<Property> GetProperty(int id)
        {
            return await database.GetAsync<Property>(id);
        }

        public async Task UpdateProperty(Property property)
        {
            await database.UpdateAsync(property);
            await CreateSyncProperty(new SyncProperty(property.Id, SyncOperation.Update));
        }

        public async Task DeleteProperty(Property property)
        {
            await database.DeleteAsync(property);
            await CreateSyncProperty(new SyncProperty(property.Id, SyncOperation.Delete));
        }
        #endregion

        #region CRD for SyncTableObject
        public async Task<int> CreateSyncTableObject(SyncTableObject syncTableObject)
        {
            return await database.InsertAsync(syncTableObject);
        }

        public async Task<SyncTableObject> GetSyncTableObject(int id)
        {
            return await database.GetAsync<SyncTableObject>(id);
        }

        public async Task DeleteSyncTableObject(int id)
        {
            // Get SyncTableObject from the database
            var syncTableObject = GetSyncTableObject(id);

            if(syncTableObject != null)
                await database.DeleteAsync(syncTableObject);
        }

        public void DeleteSyncTableObject(SyncTableObject syncTableObject)
        {
            database.DeleteAsync(syncTableObject);
        }
        #endregion

        #region CRD for SyncProperty
        public async Task<int> CreateSyncProperty(SyncProperty syncProperty)
        {
            return await database.InsertAsync(syncProperty);
        }

        public async Task<SyncProperty> GetSyncProperty(int id)
        {
            return await database.GetAsync<SyncProperty>(id);
        }

        public async Task DeleteSyncProperty(int id)
        {
            var syncProperty = await GetSyncProperty(id);

            if (syncProperty != null)
                await database.DeleteAsync(syncProperty);
        }

        public void DeleteSyncProperty(SyncProperty syncProperty)
        {
            database.DeleteAsync(syncProperty);
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
        #endregion
    }
}
