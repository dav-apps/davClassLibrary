using davClassLibrary.Controllers;
using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                {
                    sb.Append(hashBytes[i].ToString("X2"));
                }
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
            List<int> preparedTableIds = new List<int>();

            // Remove all table ids in parallelTableIds that do not occur in tableIds
            List<int> removeParallelTableIds = new List<int>();
            for (int i = 0; i < parallelTableIds.Count; i++)
            {
                var value = parallelTableIds[i];
                if (!tableIds.Contains(value))
                    removeParallelTableIds.Add(value);
            }
            parallelTableIds.RemoveAll((int t) => { return removeParallelTableIds.Contains(t); });

            // Prepare pagesOfParallelTable
            var pagesOfParallelTable = new Dictionary<int, int>();
            foreach (var table in tableIdPages)
            {
                if (parallelTableIds.Contains(table.Key))
                    pagesOfParallelTable[table.Key] = table.Value;
            }

            // Count the pages
            int pagesSum = 0;
            foreach (var table in tableIdPages)
            {
                pagesSum += table.Value;

                if (parallelTableIds.Contains(table.Key))
                    pagesOfParallelTable[table.Key] = table.Value - 1;
            }

            int index = 0;
            int currentTableIdIndex = 0;
            bool parallelTableIdsInserted = false;

            while (index < pagesSum)
            {
                int currentTableId = tableIds[currentTableIdIndex];
                int currentTablePages = tableIdPages[currentTableId];

                if (parallelTableIds.Contains(currentTableId))
                {
                    // Add the table id once as it belongs to parallel table ids
                    preparedTableIds.Add(currentTableId);
                    index++;
                }
                else
                {
                    // Add it for all pages
                    for (var j = 0; j < currentTablePages; j++)
                    {
                        preparedTableIds.Add(currentTableId);
                        index++;
                    }
                }

                // Check if all parallel table ids are in prepared table ids
                bool hasAll = true;
                foreach (var tableId in parallelTableIds)
                    if (!preparedTableIds.Contains(tableId))
                        hasAll = false;

                if (hasAll && !parallelTableIdsInserted)
                {
                    parallelTableIdsInserted = true;
                    int pagesOfParallelTableSum = 0;

                    // Update pagesOfParallelTableSum
                    foreach (var table in pagesOfParallelTable)
                        pagesOfParallelTableSum += table.Value;

                    // Append the parallel table ids in the right order
                    while (pagesOfParallelTableSum > 0)
                    {
                        foreach (var parallelTableId in parallelTableIds)
                        {
                            if (pagesOfParallelTable[parallelTableId] > 0)
                            {
                                preparedTableIds.Add(parallelTableId);
                                pagesOfParallelTableSum--;
                                pagesOfParallelTable[parallelTableId]--;

                                index++;
                            }
                        }
                    }
                }

                currentTableIdIndex++;
            }

            return preparedTableIds;
        }
    }
}
