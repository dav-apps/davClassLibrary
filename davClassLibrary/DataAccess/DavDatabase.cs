using davClassLibrary.Models;
using Ionic.Zip;
using SQLite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
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
            database = new SQLiteConnection(Path.Combine(Dav.DataPath, databaseName));
            database.CreateTable<TableObject>();
            database.CreateTable<Property>();
            database.CreateTable<SyncTableObject>();
            database.CreateTable<SyncProperty>();
        }

        #region CRUD for TableObject
        public int CreateTableObject(TableObject tableObject)
        {
            database.Insert(tableObject);
            CreateSyncTableObject(new SyncTableObject(tableObject.Uuid, SyncOperation.Create));
            return tableObject.Id;
        }

        private List<TableObject> GetAllTableObjects()
        {
            List<TableObject> tableObjectList = new List<TableObject>();
            List<TableObject> tableObjects = database.Table<TableObject>().ToList();

            foreach (var tableObject in tableObjects)
            {
                tableObject.Load();
                tableObjectList.Add(tableObject);
            }
            
            return tableObjectList;
        }

        public List<TableObject> GetAllTableObjects(int tableId)
        {
            List<TableObject> tableObjectsList = new List<TableObject>();
            List<TableObject> tableObjects = database.Table<TableObject>().ToList();
            
            // Get the properties of the table objects
            foreach (var tableObject in tableObjects)
            {
                if(tableObject.TableId == tableId)
                {
                    tableObject.Load();
                    tableObjectsList.Add(tableObject);
                }
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
            CreateSyncTableObject(new SyncTableObject(tableObject.Uuid, SyncOperation.Update));
        }

        public void DeleteTableObject(Guid uuid)
        {
            TableObject tableObject = GetTableObject(uuid);
            if(tableObject != null)
                DeleteTableObject(tableObject);
        }

        public void DeleteTableObject(TableObject tableObject)
        {
            // Delete the properties of the table object
            tableObject.RemoveAllProperties();
            
            database.Delete(tableObject);
            CreateSyncTableObject(new SyncTableObject(tableObject.Uuid, SyncOperation.Delete));
        }
        #endregion

        #region CRUD for Property
        public int CreateProperty(Property property)
        {
            database.Insert(property);
            CreateSyncProperty(new SyncProperty(property.Id, SyncOperation.Create));
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
            var syncTableObject = database.Get<SyncTableObject>(id);

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
            string tableFolderPath = Path.Combine(dataFolder.FullName, tableId.ToString());

            return Directory.CreateDirectory(tableFolderPath);
        }

        public static async Task ExportData(DirectoryInfo exportFolder, DirectoryInfo destinationFolder, string fileName, IProgress<int> progress)
        {
            // 1. foreach all table object
            // 1.1 Create a folder for every table in the export folder
            // 1.2 Write the list as json to a file in the export folder
            // 2. If the tableObject is a file, copy the file into the appropriate folder
            // 3. Zip the export folder and copy it to the destination
            // 4. Delete the export folder and the zip file in the local storage

            await Task.Run(() =>
            {
                List<TableObjectData> tableObjectDataList = new List<TableObjectData>();
                var tableObjects = Dav.Database.GetAllTableObjects();
                int i = 1;

                using (var zip = new ZipFile())
                {
                    zip.AddDirectory(exportFolder.FullName);
                    zip.Save(fileName + ".zip");

                    foreach (var tableObject in tableObjects)
                    {
                        // Create a folder for the table
                        //Directory.CreateDirectory(Path.Combine(exportFolder.FullName, tableObject.TableId.ToString()));
                        string directoryName = tableObject.TableId.ToString();

                        if (!zip.ContainsEntry(directoryName))
                            zip.AddDirectory(directoryName);

                        if (tableObject.IsFile)
                        {
                            //tableObject.File.CopyTo(Path.Combine(exportFolder.FullName, tableObject.TableId.ToString(), tableObject.File.Name));
                            zip.AddFile(tableObject.File.FullName, directoryName);
                        }

                        tableObjectDataList.Add(tableObject.ToTableObjectData());

                        progress.Report((int)Math.Round(100.0 / tableObjects.Count * i));
                        i++;
                    }

                    // Write the list of tableObjects as json
                    string dataFilePath = Path.Combine(exportFolder.FullName, "data.json");
                    WriteFile(dataFilePath, tableObjectDataList);

                    zip.AddFile(dataFilePath);

                    zip.Save(Path.Combine(exportFolder.FullName, fileName + ".zip"));
                }
            });
        }

        private static void WriteFile(string path, Object objectToWrite)
        {
            DataContractJsonSerializer js = new DataContractJsonSerializer(objectToWrite.GetType());
            MemoryStream ms = new MemoryStream();
            js.WriteObject(ms, objectToWrite);

            ms.Position = 0;
            StreamReader sr = new StreamReader(ms);
            string data = sr.ReadToEnd();

            File.WriteAllText(path, data);
        }
        #endregion
    }
}
