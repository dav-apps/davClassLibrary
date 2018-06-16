using davClassLibrary.Models;
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
            else
            {
                // Set the upload status of the table object to Deleted
                tableObject.SetUploadStatus(TableObject.TableObjectUploadStatus.Deleted);
            }
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


        #region Static things
        public static async Task<KeyValuePair<bool, string>> HttpGet(string jwt, string url)
        {
            if (NetworkInterface.GetIsNetworkAvailable())
            {
                HttpClient httpClient = new HttpClient();
                var headers = httpClient.DefaultRequestHeaders;
                headers.Authorization = new AuthenticationHeaderValue(jwt);
                Uri requestUri = new Uri(Dav.ApiBaseUrl + url);

                HttpResponseMessage httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";

                //Send the GET request
                httpResponse = await httpClient.GetAsync(requestUri);
                httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                if (httpResponse.IsSuccessStatusCode)
                {
                    return new KeyValuePair<bool, string>(true, httpResponseBody);
                }
                else
                {
                    // Return error message
                    return new KeyValuePair<bool, string>(false, "There was an error");
                }
            }
            else
            {
                // Return error message
                return new KeyValuePair<bool, string>(false, "No internet connection");
            }
        }
        
        public static DirectoryInfo GetTableFolder(int tableId)
        {
            string tableFolderPath = Path.Combine(Dav.DataPath, tableId.ToString());
            return Directory.CreateDirectory(tableFolderPath);
        }

        public static DirectoryInfo GetTempTableFolder(int tableId)
        {
            string tableFolderPath = Path.Combine(Path.GetTempPath(), "dav", tableId.ToString());
            return Directory.CreateDirectory(tableFolderPath);
        }

        public static async Task ExportData(DirectoryInfo exportFolder, IProgress<int> progress)
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
                var tableObjects = Dav.Database.GetAllTableObjects(false);
                int i = 0;

                foreach(var tableObject in tableObjects)
                {
                    tableObjectDataList.Add(tableObject.ToTableObjectData());

                    if (tableObject.IsFile)
                    {
                        string tablePath = Path.Combine(exportFolder.FullName, tableObject.TableId.ToString());
                        Directory.CreateDirectory(tablePath);

                        string tableObjectFilePath = Path.Combine(tablePath, tableObject.File.Name);
                        tableObject.File.CopyTo(tableObjectFilePath, true);
                    }

                    i++;
                    progress.Report((int)Math.Round(100.0 / tableObjects.Count * i));
                }

                // Write the list of tableObjects as json
                string dataFilePath = Path.Combine(exportFolder.FullName, "data.json");
                WriteFile(dataFilePath, tableObjectDataList);

                // Create a zip file of the export folder and copy it into the destination folder
                //string destinationFilePath = Path.Combine(exportFolder.Parent.FullName, "export.zip");
                //ZipFile.CreateFromDirectory(exportFolder.FullName, destinationFilePath);
                //return new FileInfo(destinationFilePath);

                /*
                 * Alternative approach with dotNetZip, which still does not support .NET Standard
                 * 
                Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
                using (ZipFile zip = new ZipFile())
                {
                    zip.AddDirectory(exportFolder.FullName);

                    foreach (var tableObject in tableObjects)
                    {
                        // Create a folder for the table
                        //Directory.CreateDirectory(Path.Combine(exportFolder.FullName, tableObject.TableId.ToString()));
                        string directoryName = tableObject.TableId.ToString();

                        if (!zip.ContainsEntry(directoryName))
                        {
                            string tablePath = Path.Combine(exportFolder.FullName, directoryName);
                            Directory.CreateDirectory(tablePath);
                            zip.AddDirectory(tablePath);
                        }

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
                */
            });
        }

        public static void ImportData(DirectoryInfo importFolder, IProgress<int> progress)
        {
            string dataFilePath = Path.Combine(importFolder.FullName, "data.json");
            FileInfo dataFile = new FileInfo(dataFilePath);
            List<TableObjectData> tableObjects = GetDataFromFile(dataFile);
            int i = 0;

            foreach(var tableObjectData in tableObjects)
            {
                TableObject tableObject = TableObject.ConvertTableObjectDataToTableObject(tableObjectData);
                Dav.Database.CreateTableObjectWithProperties(tableObject);

                // If the tableObject is a file, get the file from the appropriate folder
                if (tableObject.IsFile)
                {
                    try
                    {
                        string tablePath = Path.Combine(importFolder.FullName, tableObject.TableId.ToString());
                        string filePath = Path.Combine(tablePath, tableObject.Uuid.ToString());
                        FileInfo tableObjectFile = new FileInfo(filePath);
                        tableObject.SetFile(tableObjectFile);
                    }
                    catch(Exception e)
                    {
                        Debug.WriteLine(e.Message);
                    }
                }

                i++;
                progress.Report((int)Math.Round(100.0 / tableObjects.Count * i));
            }
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

        public static List<TableObjectData> GetDataFromFile(FileInfo dataFile)
        {
            string data = File.ReadAllText(dataFile.FullName);

            //Deserialize Json
            var serializer = new DataContractJsonSerializer(typeof(List<TableObjectData>));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(data));
            var dataReader = (List<TableObjectData>)serializer.ReadObject(ms);

            return dataReader;
        }

        public static Guid ConvertStringToGuid(string uuidString)
        {
            Guid uuid = Guid.Empty;
            Guid.TryParse(uuidString, out uuid);
            return uuid;
        }

        public static Dictionary<string, string> ConvertPropertiesListToDictionary(List<Property> properties)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            foreach (var property in properties)
                dictionary.Add(property.Name, property.Value);

            return dictionary;
        }

        public static byte[] FileToByteArray(string fileName)
        {
            if (!File.Exists(fileName)) return null;
            byte[] fileData = null;

            using (FileStream fs = File.OpenRead(fileName))
            {
                var binaryReader = new BinaryReader(fs);
                fileData = binaryReader.ReadBytes((int)fs.Length);
            }
            return fileData;
        }
        #endregion
    }
}
