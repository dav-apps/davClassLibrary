using davClassLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class TableObjectsController
    {
        public static async Task<ApiResponse<TableObjectResponse>> CreateTableObject(
            Guid uuid,
            int tableId,
            bool file,
            Dictionary<string, string> properties
        )
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            var requestBodyDict = new Dictionary<string, object>
            {
                { "uuid", uuid },
                { "table_id", tableId },
                { "file", file }
            };

            if (properties != null)
            {
                if (properties.Count > Constants.maxPropertiesUploadCount)
                {
                    // Create the table object with the first properties
                    // Get all keys from the properties
                    List<string> keys = new List<string>();

                    foreach (var key in properties.Keys)
                        keys.Add(key);

                    // Add the first keys to the request body
                    var selectedPropertiesDict = new Dictionary<string, string>();

                    foreach (var key in keys.GetRange(0, Constants.maxPropertiesUploadCount))
                        selectedPropertiesDict.Add(key, properties[key]);

                    requestBodyDict.Add("properties", selectedPropertiesDict);
                }
                else
                {
                    requestBodyDict.Add("properties", properties);
                }
            }

            var requestBody = new StringContent(
                JsonConvert.SerializeObject(requestBodyDict),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                response = await httpClient.PostAsync($"{Dav.ApiBaseUrl}/table_object", requestBody);
            }
            catch (Exception)
            {
                return new ApiResponse<TableObjectResponse> { Success = false, Status = 0 };
            }

            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<TableObjectResponse>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(responseData);

                    result.Data = new TableObjectResponse
                    {
                        TableEtag = tableObjectData.table_etag,
                        TableObject = tableObjectData.ToTableObject()
                    };
                }
                catch (Exception)
                {
                    result.Success = false;
                }
            }
            else
            {
                var errorResult = await Utils.HandleApiError(responseData);

                if (errorResult.Success)
                    return await CreateTableObject(uuid, tableId, file, properties);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }

        public static async Task<ApiResponse<TableObjectResponse>> GetTableObject(Guid uuid)
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            try
            {
                response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}");
            }
            catch (Exception)
            {
                return new ApiResponse<TableObjectResponse> { Success = false, Status = 0 };
            }

            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<TableObjectResponse>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(responseData);
                    
                    result.Data = new TableObjectResponse
                    {
                        TableEtag = tableObjectData.table_etag,
                        TableObject = tableObjectData.ToTableObject()
                    };
                }
                catch (Exception)
                {
                    result.Success = false;
                }
            }
            else
            {
                var errorResult = await Utils.HandleApiError(responseData);

                if (errorResult.Success)
                    return await GetTableObject(uuid);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }

        public static async Task<ApiResponse<TableObjectResponse>> UpdateTableObject(
            Guid uuid,
            Dictionary<string, string> properties
        )
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);
            bool multiRequest = properties.Count > Constants.maxPropertiesUploadCount;
            Dictionary<string, object> requestBodyDict = new Dictionary<string, object>();
            List<string> keys = new List<string>();

            if (multiRequest)
            {
                var selectedPropertiesDict = new Dictionary<string, string>();

                // Copy all property keys
                foreach (var key in properties.Keys)
                    keys.Add(key);

                // Add the first keys to the request body
                for (int i = 0; i < Constants.maxPropertiesUploadCount; i++)
                {
                    string key = keys[0];
                    selectedPropertiesDict.Add(key, properties[key]);
                    keys.Remove(key);
                }

                requestBodyDict.Add("properties", selectedPropertiesDict);
            }
            else
            {
                requestBodyDict.Add("properties", properties);
            }

            var requestBody = new StringContent(
                JsonConvert.SerializeObject(requestBodyDict),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                response = await httpClient.PutAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}", requestBody);
            }
            catch (Exception)
            {
                return new ApiResponse<TableObjectResponse> { Success = false, Status = 0 };
            }

            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<TableObjectResponse>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                if (multiRequest)
                {
                    // Send all other properties
                    while (keys.Count > 0)
                    {
                        requestBodyDict = new Dictionary<string, object>();
                        Dictionary<string, string> selectedPropertiesDict = new Dictionary<string, string>();

                        for (int i = 0; i < Constants.maxPropertiesUploadCount; i++)
                        {
                            if (keys.Count == 0) break;

                            string key = keys[0];
                            selectedPropertiesDict.Add(key, properties[key]);
                            keys.Remove(key);
                        }

                        requestBodyDict.Add("properties", selectedPropertiesDict);

                        requestBody = new StringContent(
                            JsonConvert.SerializeObject(requestBodyDict),
                            Encoding.UTF8,
                            "application/json"
                        );

                        try
                        {
                            response = await httpClient.PutAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}", requestBody);
                        }
                        catch (Exception)
                        {
                            return new ApiResponse<TableObjectResponse> { Success = false, Status = 0 };
                        }

                        if (!response.IsSuccessStatusCode) break;
                    }

                    result = new ApiResponse<TableObjectResponse>
                    {
                        Success = response.IsSuccessStatusCode,
                        Status = (int)response.StatusCode,
                    };

                    if (response.IsSuccessStatusCode)
                    {
                        responseData = await response.Content.ReadAsStringAsync();
                        var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(responseData);

                        result.Data = new TableObjectResponse
                        {
                            TableEtag = tableObjectData.table_etag,
                            TableObject = tableObjectData.ToTableObject()
                        };
                    }
                    else
                    {
                        var errorResult = await Utils.HandleApiError(responseData);

                        if (errorResult.Success)
                            return await UpdateTableObject(uuid, properties);
                        else
                            result.Errors = errorResult.Errors;
                    }
                }
                else
                {
                    try
                    {
                        var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(responseData);

                        result.Data = new TableObjectResponse
                        {
                            TableEtag = tableObjectData.table_etag,
                            TableObject = tableObjectData.ToTableObject()
                        };
                    }
                    catch (Exception)
                    {
                        result.Success = false;
                    }
                }
            }
            else
            {
                var errorResult = await Utils.HandleApiError(responseData);

                if (errorResult.Success)
                    return await UpdateTableObject(uuid, properties);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }

        public static async Task<ApiResponse> DeleteTableObject(Guid uuid)
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            try
            {
                response = await httpClient.DeleteAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}");
            }
            catch (Exception)
            {
                return new ApiResponse { Success = false, Status = 0 };
            }

            var result = new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (!response.IsSuccessStatusCode)
            {
                string responseData = await response.Content.ReadAsStringAsync();
                var errorResult = await Utils.HandleApiError(responseData);

                if (errorResult.Success)
                    return await DeleteTableObject(uuid);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }

        public static async Task<ApiResponse<TableObjectResponse>> SetTableObjectFile(
            Guid uuid,
            string filePath,
            string contentType
        )
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            // Read the file
            byte[] data = Utils.ReadFile(filePath);
            if (data == null) return null;

            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            try
            {
                response = await httpClient.PutAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}/file", content);
            }
            catch (Exception)
            {
                return new ApiResponse<TableObjectResponse> { Success = false, Status = 0 };
            }

            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<TableObjectResponse>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(responseData);
                    
                    result.Data = new TableObjectResponse
                    {
                        TableEtag = tableObjectData.table_etag,
                        TableObject = tableObjectData.ToTableObject()
                    };
                }
                catch (Exception)
                {
                    result.Success = false;
                }
            }
            else
            {
                var errorResult = await Utils.HandleApiError(responseData);

                if (errorResult.Success)
                    return await SetTableObjectFile(uuid, filePath, contentType);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }

        public static async Task<ApiResponse> GetTableObjectFile(Guid uuid, string filePath, IProgress<int> progress)
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            try
            {
                response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}/file", HttpCompletionOption.ResponseHeadersRead);
            }
            catch (Exception)
            {
                return new ApiResponse { Success = false, Status = 0 };
            }

            long contentLength = response.Content.Headers.ContentLength.GetValueOrDefault();

            var result = new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                FileStream fileStream = null;

                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        fileStream = File.Create(filePath);
                        var buffer = new byte[8192];
                        int read;
                        long offset = 0;

                        do
                        {
                            read = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                            await fileStream.WriteAsync(buffer, 0, read);
                            offset += read;

                            if (progress != null && offset != 0 && read != 0 && contentLength != 0)
                                progress.Report((int)Math.Floor((double)offset / contentLength * 100));
                        } while (read != 0);

                        await fileStream.FlushAsync();
                        fileStream.Close();
                    }
                }
                catch (Exception)
                {
                    if (fileStream != null) fileStream.Close();
                    result.Success = false;
                }
            }
            else
            {
                string responseData = await response.Content.ReadAsStringAsync();
                var errorResult = await Utils.HandleApiError(responseData);

                if (errorResult.Success)
                    return await GetTableObjectFile(uuid, filePath, progress);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }
    }

    public class TableObjectResponse
    {
        public string TableEtag { get; set; }
        public TableObject TableObject { get; set; }
    }
}
