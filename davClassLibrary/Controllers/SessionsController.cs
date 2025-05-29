using davClassLibrary.DataAccess;
using GraphQL;
using System;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class SessionsController
    {
        public static async Task<GraphQLResponse<CreateSessionResponse>> CreateSession(
            string queryData,
            string auth,
            string email,
            string password,
            int appId,
            string apiKey
        )
        {
            var createSessionRequest = new GraphQLRequest
            {
                OperationName = "CreateSession",
                Query = $@"
                    mutation CreateSession(
                        $email: String!
					    $password: String!
					    $appId: Int!
					    $apiKey: String!
                    ) {{
                        createSession(
                            email: $email
						    password: $password
						    appId: $appId
						    apiKey: $apiKey
                        ) {{
                            {queryData}
                        }}
                    }}
                ",
                Variables = new
                {
                    email,
                    password,
                    appId,
                    apiKey
                }
            };

            try
            {
                return await ApiManager.GetGraphQLClient(auth).SendMutationAsync<CreateSessionResponse>(createSessionRequest);
            }
            catch(Exception)
            {
                return null;
            }
        }

        public static async Task<GraphQLResponse<RenewSessionResponse>> RenewSession(
            string queryData,
            string accessToken
        )
        {
            var renewSessionRequest = new GraphQLRequest
            {
                OperationName = "RenewSession",
                Query = $@"
                    mutation RenewSession {{
                        renewSession {{
                            {queryData}
                        }}
                    }}
                "
            };

            try
            {
                return await ApiManager.GetGraphQLClient(accessToken).SendMutationAsync<RenewSessionResponse>(renewSessionRequest);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static async Task<GraphQLResponse<DeleteSessionResponse>> DeleteSession(
            string queryData,
            string accessToken
        )
        {
            var deleteSessionRequest = new GraphQLRequest
            {
                OperationName = "DeleteSession",
                Query = $@"
                    mutation DeleteSession {{
                        deleteSession {{
                            {queryData}
                        }}
                    }}
                "
            };

            try
            {
                return await ApiManager.GetGraphQLClient(accessToken).SendMutationAsync<DeleteSessionResponse>(deleteSessionRequest);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }

    public class CreateSessionResponse
    {
        public SessionResponseData CreateSession { get; set; }
    }

    public class RenewSessionResponse
    {
        public SessionResponseData RenewSession { get; set; }
    }

    public class DeleteSessionResponse
    {
        public SessionResponseData DeleteSession { get; set; }
    }

    public class SessionResponseData
    {
        public string accessToken { get; set; }
        public string websiteAccessToken { get; set; }
    }
}
