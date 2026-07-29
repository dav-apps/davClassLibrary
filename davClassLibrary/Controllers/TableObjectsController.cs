using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using GraphQL;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class TableObjectsController
    {
        public static async Task<GraphQLApiResponse<TableObjectResource>> RetrieveTableObject(
            string queryData,
            Guid uuid
        )
        {
            var retrieveTableObjectRequest = new GraphQLRequest
            {
                OperationName = "RetrieveTableObject",
                Query = $@"
                    query RetrieveTableObject($uuid: String!) {{
                        retrieveTableObject(uuid: $uuid) {{
                            {queryData}
                        }}
                    }}
                ",
                Variables = new { uuid }
            };

            try
            {
                var response = await ApiManager
                    .GetGraphQLClient()
                    .SendQueryAsync<RetrieveTableObjectResponse>(
                        retrieveTableObjectRequest
                    );

                if (response.Errors != null && response.Errors.Any())
                {
                    var errorCodes = Utils.GetErrorCodesOfGraphQLError(response.Errors);
                    var renewSessionErrors = await Utils.HandleGraphQLApiErrors(errorCodes);

                    if (renewSessionErrors != null)
                    {
                        return new GraphQLApiResponse<TableObjectResource>
                        {
                            Success = false,
                            Errors = renewSessionErrors
                        };
                    }

                    return await RetrieveTableObject(queryData, uuid);
                }

                return new GraphQLApiResponse<TableObjectResource>
                {
                    Success = true,
                    Data = response.Data.RetrieveTableObject
                };
            }
            catch (Exception)
            {
                return new GraphQLApiResponse<TableObjectResource> { Success = false };
            }
        }

        public static async Task<GraphQLApiResponse<TableObjectResource>> CreateTableObject(
            string queryData,
            Guid uuid,
            int tableId,
            bool file,
            string ext,
            Dictionary<string, string> properties
        )
        {
            var createTableObjectRequest = new GraphQLRequest
            {
                OperationName = "CreateTableObject",
                Query = $@"
                    mutation CreateTableObject(
                        $uuid: String
					    $tableId: Int!
					    $file: Boolean
					    $ext: String
					    $properties: JSONObject
                    ) {{
                        createTableObject(
						    uuid: $uuid
						    tableId: $tableId
						    file: $file
						    ext: $ext
						    properties: $properties
					    ) {{
						    {queryData}
					    }}
                    }}
                ",
                Variables = new
                {
                    uuid,
                    tableId,
                    file,
                    ext,
                    properties
                }
            };

            try
            {
                var response = await ApiManager
                    .GetGraphQLClient()
                    .SendMutationAsync<CreateTableObjectResponse>(
                        createTableObjectRequest
                    );

                if (response.Errors != null && response.Errors.Any())
                {
                    var errorCodes = Utils.GetErrorCodesOfGraphQLError(response.Errors);
                    var renewSessionErrors = await Utils.HandleGraphQLApiErrors(errorCodes);

                    if (renewSessionErrors != null)
                    {
                        return new GraphQLApiResponse<TableObjectResource>
                        {
                            Success = false,
                            Errors = renewSessionErrors
                        };
                    }

                    return await CreateTableObject(queryData, uuid, tableId, file, ext, properties);
                }

                return new GraphQLApiResponse<TableObjectResource>
                {
                    Success = true,
                    Data = response.Data.CreateTableObject
                };
            }
            catch (Exception)
            {
                return new GraphQLApiResponse<TableObjectResource> { Success = false };
            }
        }

        public static async Task<GraphQLApiResponse<TableObjectResource>> UpdateTableObject(
            string queryData,
            Guid uuid,
            string ext,
            Dictionary<string, string> properties
        )
        {
            var updateTableObjectRequest = new GraphQLRequest
            {
                OperationName = "UpdateTableObject",
                Query = $@"
                    mutation UpdateTableObject(
                        $uuid: String!
                        $ext: String
                        $properties: JSONObject
                    ) {{
                        updateTableObject(
                            uuid: $uuid
                            ext: $ext
                            properties: $properties
                        ) {{
                            {queryData}
                        }}
                    }}
                ",
                Variables = new
                {
                    uuid,
                    ext,
                    properties
                }
            };

            try
            {
                var response = await ApiManager
                    .GetGraphQLClient()
                    .SendMutationAsync<UpdateTableObjectResponse>(
                        updateTableObjectRequest
                    );

                if (response.Errors != null && response.Errors.Any())
                {
                    var errorCodes = Utils.GetErrorCodesOfGraphQLError(response.Errors);
                    var renewSessionErrors = await Utils.HandleGraphQLApiErrors(errorCodes);

                    if (renewSessionErrors != null)
                    {
                        return new GraphQLApiResponse<TableObjectResource>
                        {
                            Success = false,
                            Errors = renewSessionErrors
                        };
                    }

                    return await UpdateTableObject(queryData, uuid, ext, properties);
                }

                return new GraphQLApiResponse<TableObjectResource>
                {
                    Success = true,
                    Data = response.Data.UpdateTableObject
                };
            }
            catch (Exception)
            {
                return new GraphQLApiResponse<TableObjectResource> { Success = false };
            }
        }

        public static async Task<GraphQLApiResponse<TableObjectResource>> DeleteTableObject(
            string queryData,
            Guid uuid
        )
        {
            var deleteTableObjectRequest = new GraphQLRequest
            {
                OperationName = "DeleteTableObject",
                Query = $@"
                    mutation DeleteTableObject(
                        $uuid: String!
                    ) {{
                        deleteTableObject(
                            uuid: $uuid
                        ) {{
                            {queryData}
                        }}
                    }}
                ",
                Variables = new
                {
                    uuid
                }
            };

            try
            {
                var response = await ApiManager
                    .GetGraphQLClient()
                    .SendMutationAsync<DeleteTableObjectResponse>(
                        deleteTableObjectRequest
                    );

                if (response.Errors != null && response.Errors.Any())
                {
                    var errorCodes = Utils.GetErrorCodesOfGraphQLError(response.Errors);
                    var renewSessionErrors = await Utils.HandleGraphQLApiErrors(errorCodes);

                    if (renewSessionErrors != null)
                    {
                        return new GraphQLApiResponse<TableObjectResource>
                        {
                            Success = false,
                            Errors = renewSessionErrors
                        };
                    }

                    return await DeleteTableObject(queryData, uuid);
                }

                return new GraphQLApiResponse<TableObjectResource>
                {
                    Success = true,
                    Data = response.Data.DeleteTableObject
                };
            }
            catch (Exception)
            {
                return new GraphQLApiResponse<TableObjectResource> { Success = false };
            }
        }

        public static async Task<ApiResponse<UploadTableObjectFileData>> UploadTableObjectFile(
            Guid uuid,
            string contentType,
            string filePath
        )
        {
            HttpResponseMessage response;
            byte[] data = null;

            using (FileStream fs = File.OpenRead(filePath))
            {
                var binaryReader = new BinaryReader(fs);
                data = binaryReader.ReadBytes((int)fs.Length);
            }

            var content = new ByteArrayContent(data);
            content.Headers.ContentType = new MediaTypeHeaderValue(contentType);

            try
            {
                response = await ApiManager.GetHttpClient().PutAsync($"{Dav.ApiBaseUrl}/tableObject/{uuid}/file", content);
            }
            catch (Exception)
            {
                return new ApiResponse<UploadTableObjectFileData>
                {
                    Success = false
                };
            }

            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<UploadTableObjectFileData>
            {
                Success = response.IsSuccessStatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    result.Data = JsonConvert.DeserializeObject<UploadTableObjectFileData>(responseData);
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
                    return await UploadTableObjectFile(uuid, contentType, filePath);
                else if (errorResult.Errors.Count > 0)
                {
                    result.Error = new ApiResponseError
                    {
                        Code = errorResult.Errors.First()
                    };
                }
            }

            return result;
        }
    }

    public class RetrieveTableObjectResponse
    {
        public TableObjectResource RetrieveTableObject { get; set; }
    }

    public class CreateTableObjectResponse
    {
        public TableObjectResource CreateTableObject { get; set; }
    }

    public class UpdateTableObjectResponse
    {
        public TableObjectResource UpdateTableObject { get; set; }
    }

    public class DeleteTableObjectResponse
    {
        public TableObjectResource DeleteTableObject { get; set; }
    }
}
