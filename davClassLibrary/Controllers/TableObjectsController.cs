using davClassLibrary.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
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
                var tableObjectData = Utils.SerializeJson<TableObjectData>(responseData);
                result.Data = tableObjectData.ToTableObject();
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
                var tableObjectData = Utils.SerializeJson<TableObjectData>(responseData);
                result.Data = tableObjectData.ToTableObject();
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
                var tableObjectData = Utils.SerializeJson<TableObjectData>(responseData);
                result.Data = tableObjectData.ToTableObject();
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
                var tableObjectData = Utils.SerializeJson<TableObjectData>(responseData);
                result.Data = tableObjectData.ToTableObject();
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
    }
}
