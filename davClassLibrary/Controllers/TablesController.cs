using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using GraphQL;
using System;
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

            try
            {
                return await ApiManager.GetGraphQLClient().SendQueryAsync<RetrieveTableResponse>(retrieveTableRequest);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class RetrieveTableResponse
    {
        public TableResource RetrieveTable { get; set; }
    }
}
