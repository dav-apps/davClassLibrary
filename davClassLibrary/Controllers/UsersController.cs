using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using GraphQL;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class UsersController
    {
        public static async Task<GraphQLApiResponse<UserResource>> RetrieveUser(string queryData)
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
                var response = await ApiManager
                    .GetGraphQLClient()
                    .SendQueryAsync<RetrieveUserResponse>(
                        retrieveUserRequest
                    );

                if (response.Errors != null && response.Errors.Any())
                {
                    var errorCodes = Utils.GetErrorCodesOfGraphQLError(response.Errors);
                    var renewSessionErrors = await Utils.HandleGraphQLApiErrors(errorCodes);

                    if (renewSessionErrors != null)
                    {
                        return new GraphQLApiResponse<UserResource>
                        {
                            Success = false,
                            Errors = renewSessionErrors
                        };
                    }

                    return await RetrieveUser(queryData);
                }

                return new GraphQLApiResponse<UserResource>
                {
                    Success = true,
                    Data = response.Data.RetrieveUser
                };
            }
            catch (Exception)
            {
                return new GraphQLApiResponse<UserResource> { Success = false };
            }
        }
    }

    public class RetrieveUserResponse
    {
        public UserResource RetrieveUser { get; set; }
    }
}
