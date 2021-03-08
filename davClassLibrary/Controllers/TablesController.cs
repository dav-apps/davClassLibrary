using davClassLibrary.Models;
using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class TablesController
    {
        public static async Task<ApiResponse<GetTableResponse>> GetTable(int id, int page = 0)
        {
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Dav.AccessToken);

            var response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/table/{id}?page={page}");
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<GetTableResponse>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                var getTableResponseData = Utils.SerializeJson<GetTableResponseData>(responseData);
                result.Data = getTableResponseData.ToGetTableResponse();
            }
            else
            {
                var errorResult = await Utils.HandleApiError(responseData);

                if (errorResult.Success)
                    return await GetTable(id);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }
    }

    public class GetTableResponse
    {
        public Table Table { get; set; }
        public int Pages { get; set; }
        public List<GetTableResponseTableObject> TableObjects { get; set; }
    }

    public class GetTableResponseTableObject
    {
        public Guid Uuid { get; set; }
        public string Etag { get; set; }
    }

    public class GetTableResponseData
    {
        public int id { get; set; }
        public int app_id { get; set; }
        public string name { get; set; }
        public int pages { get; set; }
        public GetTableResponseTableObjectData[] table_objects { get; set; }

        public GetTableResponse ToGetTableResponse()
        {
            List<GetTableResponseTableObject> tableObjects = new List<GetTableResponseTableObject>();
            foreach(var obj in table_objects)
            {
                tableObjects.Add(new GetTableResponseTableObject
                {
                    Uuid = obj.uuid,
                    Etag = obj.etag
                });
            }

            return new GetTableResponse
            {
                Table = new Table
                {
                    Id = id,
                    AppId = app_id,
                    Name = name
                },
                Pages = pages,
                TableObjects = tableObjects
            };
        }
    }

    public class GetTableResponseTableObjectData
    {
        public Guid uuid { get; set; }
        public string etag { get; set; }
    }
}
