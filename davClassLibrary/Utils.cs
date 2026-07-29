using davClassLibrary.Controllers;
using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using GraphQL;
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

        public static List<string> GetErrorCodesOfGraphQLError(GraphQLError[] errors)
        {
            List<string> errorCodes = new List<string>();

            foreach (var error in errors)
            {
                object code = null;
                error.Extensions?.TryGetValue("code", out code);

                if (code.Equals("VALIDATION_FAILED"))
                {
                    object validationErrors = null;
                    error.Extensions?.TryGetValue("errors", out validationErrors);
                    List<string> validationErrorsList = (List<string>)validationErrors;
                }
                else if (code != null)
                {
                    errorCodes.Add((string)code);
                }
            }

            return errorCodes;
        }

        internal static async Task<List<string>> HandleGraphQLApiErrors(List<string> errorCodes)
        {
            if (errorCodes.Contains(ErrorCodesNew.SessionExpired))
            {
                // Renew the session
                var renewSessionResult = await SessionsController.RenewSession("accessToken", Dav.AccessToken);

                if (renewSessionResult.Errors == null)
                {
                    // Update the access token and save it in the local settings
                    Dav.AccessToken = renewSessionResult.Data.accessToken;
                    SettingsManager.SetAccessToken(Dav.AccessToken);
                    return null;
                }
                else
                {
                    return renewSessionResult.Errors;
                }

            }
            else
            {
                return errorCodes;
            }
        }

        internal static async Task<HandleApiErrorResult> HandleApiError(string responseData)
        {
            try
            {
                var json = JsonConvert.DeserializeObject<ApiErrorRaw>(responseData);

                if (json == null || json.code == null)
                    return new HandleApiErrorResult { Success = false, Errors = null };

                if (json.code == ErrorCodesNew.SessionExpired)
                {
                    // Renew the session
                    var renewSessionResult = await SessionsController.RenewSession("accessToken", Dav.AccessToken);

                    if (renewSessionResult.Errors == null)
                    {
                        // Update the access token and save it in the local settings
                        Dav.AccessToken = renewSessionResult.Data.accessToken;
                        SettingsManager.SetAccessToken(Dav.AccessToken);

                        return new HandleApiErrorResult { Success = true, Errors = null };
                    }
                    else
                    {
                        return new HandleApiErrorResult {
                            Success = false,
                            Errors = renewSessionResult.Errors
                        };
                    }
                }

                return new HandleApiErrorResult { Success = false, Errors = new List<string> { json.code } };
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

        public static List<string> SortTableNames(
            List<string> tableNames,
            List<string> parallelTableNames,
            Dictionary<string, int> tableNamePages
        )
        {
            // Clone tableIdPages
            Dictionary<string, int> TableNamePagesCopy = new Dictionary<string, int>();

            foreach(var key in tableNamePages.Keys)
                if (tableNames.Contains(key))
                    TableNamePagesCopy[key] = tableNamePages[key];

            // Remove all entries in tableIdPages with value = 0
            foreach (var key in TableNamePagesCopy.Keys)
                if (TableNamePagesCopy[key] == 0)
                    TableNamePagesCopy.Remove(key);

            List<string> sortedTableNames = new List<string>();
            int currentTableNameIndex = 0;

            while (GetSumOfValuesInDict(TableNamePagesCopy) > 0)
            {
                if (currentTableNameIndex >= tableNames.Count)
                    currentTableNameIndex = 0;

                string currentTableName = tableNames[currentTableNameIndex];

                if (!TableNamePagesCopy.ContainsKey(currentTableName))
                {
                    currentTableNameIndex++;
                    continue;
                }

                if (parallelTableNames.Contains(currentTableName) && parallelTableNames.Count > 1)
                {
                    // Add just one page of the current table
                    sortedTableNames.Add(currentTableName);
                    TableNamePagesCopy[currentTableName]--;

                    // Remove the table id from the pages if there are no pages left
                    if (TableNamePagesCopy[currentTableName] <= 0)
                        TableNamePagesCopy.Remove(currentTableName);

                    // Check if this was the last table of parallelTableIds
                    int i = parallelTableNames.IndexOf(currentTableName);
                    bool isLastParallelTable = i == parallelTableNames.Count - 1;

                    if (isLastParallelTable)
                    {
                        // Move to the start of the array
                        currentTableNameIndex = 0;
                    }
                    else
                    {
                        currentTableNameIndex++;
                    }
                }
                else
                {
                    // Add all pages of the current table
                    for (var i = 0; i < TableNamePagesCopy[currentTableName]; i++)
                        sortedTableNames.Add(currentTableName);

                    // Clear the pages of the current table
                    TableNamePagesCopy.Remove(currentTableName);

                    // Go to the next table
                    currentTableNameIndex++;
                }
            }

            return sortedTableNames;
        }

        private static int GetSumOfValuesInDict(Dictionary<string, int> dict)
        {
            int sum = 0;

            foreach(var key in dict.Keys)
                sum += dict[key];

            return sum;
        }
    }
}
