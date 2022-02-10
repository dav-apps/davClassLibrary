using davClassLibrary.Controllers;
using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace davClassLibrary
{
    public static class Utils
    {
        internal static byte[] ReadFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            byte[] fileData = null;

            using (FileStream fs = File.OpenRead(filePath))
            {
                var binaryReader = new BinaryReader(fs);
                fileData = binaryReader.ReadBytes((int)fs.Length);
            }
            return fileData;
        }

        internal static DirectoryInfo GetTableFolder(int tableId)
        {
            string tableFolderPath = Path.Combine(Dav.DataPath, tableId.ToString());
            return Directory.CreateDirectory(tableFolderPath);
        }

        internal static DirectoryInfo GetTempTableFolder(int tableId)
        {
            string tableFolderPath = Path.Combine(Path.GetTempPath(), "dav", tableId.ToString());
            return Directory.CreateDirectory(tableFolderPath);
        }

        // https://stackoverflow.com/questions/11454004/calculate-a-md5-hash-from-a-string
        internal static string CreateMD5(string input)
        {
            // Use input string to calculate MD5 hash
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = Encoding.ASCII.GetBytes(input);
                byte[] hashBytes = md5.ComputeHash(inputBytes);

                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                    sb.Append(hashBytes[i].ToString("x2"));

                return sb.ToString();
            }
        }

        internal static Dictionary<string, string> ConvertPropertiesListToDictionary(List<Property> properties)
        {
            Dictionary<string, string> dictionary = new Dictionary<string, string>();

            foreach (var property in properties)
                dictionary[property.Name] = property.Value;

            return dictionary;
        }

        internal static async Task<HandleApiErrorResult> HandleApiError(string responseData)
        {
            try
            {
                var json = JsonConvert.DeserializeObject<ApiErrors>(responseData);
                if (json == null) return new HandleApiErrorResult { Success = false, Errors = null };

                if (json.Errors.Length > 0 && json.Errors[0].Code == ErrorCodes.AccessTokenMustBeRenewed)
                {
                    // Renew the session
                    var renewSessionResult = await SessionsController.RenewSession(Dav.AccessToken);

                    if (renewSessionResult.Status == 200)
                    {
                        // Update the access token and save it in the local settings
                        Dav.AccessToken = renewSessionResult.Data.AccessToken;
                        SettingsManager.SetAccessToken(Dav.AccessToken);

                        return new HandleApiErrorResult { Success = true, Errors = null };
                    }
                    else
                    {
                        return new HandleApiErrorResult { Success = false, Errors = renewSessionResult.Errors };
                    }
                }

                return new HandleApiErrorResult { Success = false, Errors = json.Errors };
            }
            catch (Exception)
            {
                return new HandleApiErrorResult
                {
                    Success = false,
                    Errors = null
                };
            }
        }

        public static List<int> SortTableIds(List<int> tableIds, List<int> parallelTableIds, Dictionary<int, int> tableIdPages)
        {
            // Clone tableIdPages
            Dictionary<int, int> TableIdPagesCopy = new Dictionary<int, int>();

            foreach(var key in tableIdPages.Keys)
                if (tableIds.Contains(key))
                    TableIdPagesCopy[key] = tableIdPages[key];

            // Remove all entries in tableIdPages with value = 0
            foreach (var key in TableIdPagesCopy.Keys)
                if (TableIdPagesCopy[key] == 0)
                    TableIdPagesCopy.Remove(key);

            List<int> sortedTableIds = new List<int>();
            int currentTableIdIndex = 0;

            while (GetSumOfValuesInDict(TableIdPagesCopy) > 0)
            {
                if (currentTableIdIndex >= tableIds.Count)
                    currentTableIdIndex = 0;

                int currentTableId = tableIds[currentTableIdIndex];

                if (!TableIdPagesCopy.ContainsKey(currentTableId))
                {
                    currentTableIdIndex++;
                    continue;
                }

                if (parallelTableIds.Contains(currentTableId) && parallelTableIds.Count > 1)
                {
                    // Add just one page of the current table
                    sortedTableIds.Add(currentTableId);
                    TableIdPagesCopy[currentTableId]--;

                    // Remove the table id from the pages if there are no pages left
                    if (TableIdPagesCopy[currentTableId] <= 0)
                        TableIdPagesCopy.Remove(currentTableId);

                    // Check if this was the last table of parallelTableIds
                    int i = parallelTableIds.IndexOf(currentTableId);
                    bool isLastParallelTable = i == parallelTableIds.Count - 1;

                    if (isLastParallelTable)
                    {
                        // Move to the start of the array
                        currentTableIdIndex = 0;
                    }
                    else
                    {
                        currentTableIdIndex++;
                    }
                }
                else
                {
                    // Add all pages of the current table
                    for (var i = 0; i < TableIdPagesCopy[currentTableId]; i++)
                        sortedTableIds.Add(currentTableId);

                    // Clear the pages of the current table
                    TableIdPagesCopy.Remove(currentTableId);

                    // Go to the next table
                    currentTableIdIndex++;
                }
            }

            return sortedTableIds;
        }

        private static int GetSumOfValuesInDict(Dictionary<int, int> dict)
        {
            int sum = 0;

            foreach(var key in dict.Keys)
                sum += dict[key];

            return sum;
        }
    }
}
