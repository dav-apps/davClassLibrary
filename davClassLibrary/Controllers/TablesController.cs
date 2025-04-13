using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using GraphQL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class TablesController
    {
        public static async Task<GraphQLResponse<RetrieveTableResponse>> RetrieveTable(
            string queryData,
            string name,
            int limit = 100,
            int offset = 0
        )
        {
            string limitParam = queryData.Contains("limit") ? "$limit: Int" : "";
            string offsetParam = queryData.Contains("offset") ? "$offset: Int" : "";

            var retrieveTableRequest = new GraphQLRequest
            {
                OperationName = "RetrieveTable",
                Query = $@"
                    query RetrieveTable(
                        $name: String!
                        {limitParam}
                        {offsetParam}
                    ) {{
                        retrieveTable(name: $name) {{
                            {queryData}
                        }}
                    }}
                ",
                Variables = new
                {
                    name,
                    limit,
                    offset
                }
            };

            return await ApiManager.GetGraphQLClient().SendQueryAsync<RetrieveTableResponse>(retrieveTableRequest);
        }

        public static async Task<ApiResponse<GetTableResponse>> GetTable(int id, int page = 0)
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            try
            {
                response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/table/{id}?page={page}&count=50");
            }
            catch (Exception)
            {
                return new ApiResponse<GetTableResponse> { Success = false, Status = 0 };
            }

            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<GetTableResponse>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var getTableResponseData = JsonConvert.DeserializeObject<GetTableResponseData>(responseData);
                    result.Data = getTableResponseData.ToGetTableResponse();
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
                    return await GetTable(id);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }
    }

    public class RetrieveTableResponse
    {
        public TableResource RetrieveTable { get; set; }
    }

    public class GetTableResponse
    {
        public Table Table { get; set; }
        public int Pages { get; set; }
        public string Etag { get; set; }
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
        public string etag { get; set; }
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
                Etag = etag,
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
