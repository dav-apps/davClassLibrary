using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using GraphQL;
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

            return await ApiManager.GetGraphQLClient().SendQueryAsync<RetrieveUserResponse>(retrieveUserRequest);
        }
    }

    public class RetrieveUserResponse
    {
        public UserResource RetrieveUser { get; set; }
    }
}
