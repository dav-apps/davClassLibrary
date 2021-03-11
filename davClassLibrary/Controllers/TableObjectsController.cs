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
        public static async Task<ApiResponse<TableObject>> CreateTableObject(
            Guid uuid,
            int tableId,
            bool file,
            Dictionary<string, string> properties
        )
        {
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Dav.AccessToken);
            httpClient.DefaultRequestHeaders.Add("CONTENT_TYPE", "application/json");

            var requestBodyDict = new Dictionary<string, object>
            {
                { "uuid", uuid },
                { "table_id", tableId },
                { "file", file }
            };

            if (properties != null)
                requestBodyDict.Add("properties", properties);

            var requestBody = new StringContent(
                JsonConvert.SerializeObject(requestBodyDict),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PostAsync($"{Dav.ApiBaseUrl}/table_object", requestBody);
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<TableObject>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(responseData);
                    result.Data = tableObjectData.ToTableObject();
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

        public static async Task<ApiResponse<TableObject>> GetTableObject(Guid uuid)
        {
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Dav.AccessToken);

            var response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}");
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<TableObject>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(responseData);
                    result.Data = tableObjectData.ToTableObject();
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

        public static async Task<ApiResponse<TableObject>> UpdateTableObject(
            Guid uuid,
            Dictionary<string, string> properties
        )
        {
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Dav.AccessToken);
            httpClient.DefaultRequestHeaders.Add("CONTENT_TYPE", "application/json");

            var requestBodyDict = new Dictionary<string, object>
            {
                { "properties", properties }
            };

            var requestBody = new StringContent(
                JsonConvert.SerializeObject(requestBodyDict),
                Encoding.UTF8,
                "application/json"
            );

            var response = await httpClient.PutAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}", requestBody);
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<TableObject>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(responseData);
                    result.Data = tableObjectData.ToTableObject();
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
                    return await UpdateTableObject(uuid, properties);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }

        public static async Task<ApiResponse> DeleteTableObject(Guid uuid)
        {
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Dav.AccessToken);

            var response = await httpClient.DeleteAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}");

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

        public static async Task<ApiResponse<TableObject>> SetTableObjectFile(
            Guid uuid,
            string filePath,
            string contentType
        )
        {
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Dav.AccessToken);
            httpClient.DefaultRequestHeaders.Add("CONTENT_TYPE", contentType);

            // Read the file
            byte[] data = Utils.ReadFile(filePath);
            if (data == null) return null;

            var response = await httpClient.PutAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}/file", new ByteArrayContent(data));
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<TableObject>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var tableObjectData = JsonConvert.DeserializeObject<TableObjectData>(responseData);
                    result.Data = tableObjectData.ToTableObject();
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
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Dav.AccessToken);

            var response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/table_object/{uuid}/file");
            long contentLength = response.Content.Headers.ContentLength.GetValueOrDefault();

            var result = new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    if (File.Exists(filePath))
                        File.Delete(filePath);

                    using (var responseStream = await response.Content.ReadAsStreamAsync())
                    {
                        var fileStream = File.Create(filePath);
                        var buffer = new byte[1024];
                        int read;
                        long offset = 0;

                        do
                        {
                            read = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                            await fileStream.WriteAsync(buffer, 0, buffer.Length);
                            offset += read;

                            if (offset != 0)
                                progress.Report((int)Math.Floor(offset / (float)contentLength * 100));

                        } while (read != 0);

                        await fileStream.FlushAsync();
                        fileStream.Dispose();
                        progress.Report(100);
                    }
                }
                catch (Exception)
                {
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
}
