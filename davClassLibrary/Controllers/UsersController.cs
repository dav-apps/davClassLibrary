using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using GraphQL;
using System;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class UsersController
    {
        public static async Task<GraphQLResponse<RetrieveUserResponse>> RetrieveUser(string queryData)
        {
            var retrieveUserRequest = new GraphQLRequest
            {
                OperationName = "RetrieveUser",
                Query = $@"
                    query RetrieveUser {{
                        retrieveUser {{
                            {queryData}
                        }}
                    }}
                "
            };

            try
            {
                return await ApiManager.GetGraphQLClient().SendQueryAsync<RetrieveUserResponse>(retrieveUserRequest);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class RetrieveUserResponse
    {
        public UserResource RetrieveUser { get; set; }
    }
}
