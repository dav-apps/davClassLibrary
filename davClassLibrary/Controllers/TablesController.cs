using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using GraphQL;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class TablesController
    {
        public static async Task<GraphQLApiResponse<TableResource>> RetrieveTable(
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

            try
            {
                var response = await ApiManager
                    .GetGraphQLClient()
                    .SendQueryAsync<RetrieveTableResponse>(
                        retrieveTableRequest
                    );

                if (response.Errors != null && response.Errors.Any())
                {
                    var errorCodes = Utils.GetErrorCodesOfGraphQLError(response.Errors);
                    var renewSessionErrors = await Utils.HandleGraphQLApiErrors(errorCodes);

                    if (renewSessionErrors != null)
                    {
                        return new GraphQLApiResponse<TableResource>
                        {
                            Success = false,
                            Errors = renewSessionErrors
                        };
                    }

                    return await RetrieveTable(queryData, name, limit, offset);
                }

                return new GraphQLApiResponse<TableResource>
                {
                    Success = true,
                    Data = response.Data.RetrieveTable
                };
            }
            catch (Exception)
            {
                return new GraphQLApiResponse<TableResource> { Success = false };
            }
        }
    }

    public class RetrieveTableResponse
    {
        public TableResource RetrieveTable { get; set; }
    }
}
