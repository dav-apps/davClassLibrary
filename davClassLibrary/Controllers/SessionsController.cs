using davClassLibrary.DataAccess;
using GraphQL;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class SessionsController
    {
        public static async Task<GraphQLApiResponse<SessionResponseData>> RenewSession(
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
                var response = await ApiManager
                    .GetGraphQLClient(accessToken)
                    .SendMutationAsync<RenewSessionResponse>(
                        renewSessionRequest
                    );

                if (response.Errors != null && response.Errors.Any())
                {
                    return new GraphQLApiResponse<SessionResponseData>
                    {
                        Success = false,
                        Errors = Utils.GetErrorCodesOfGraphQLError(response.Errors)
                    };
                }

                return new GraphQLApiResponse<SessionResponseData>
                {
                    Success = true,
                    Data = response.Data.RenewSession
                };
            }
            catch (Exception)
            {
                return new GraphQLApiResponse<SessionResponseData> { Success = false };
            }
        }

        public static async Task<GraphQLApiResponse<SessionResponseData>> DeleteSession(
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
                var response = await ApiManager
                    .GetGraphQLClient(accessToken)
                    .SendMutationAsync<DeleteSessionResponse>(
                        deleteSessionRequest
                    );

                if (response.Errors != null && response.Errors.Any())
                {
                    return new GraphQLApiResponse<SessionResponseData>
                    {
                        Success = false,
                        Errors = Utils.GetErrorCodesOfGraphQLError(response.Errors)
                    };
                }

                return new GraphQLApiResponse<SessionResponseData>
                {
                    Success = true,
                    Data = response.Data.DeleteSession
                };
            }
            catch (Exception)
            {
                return new GraphQLApiResponse<SessionResponseData> { Success = false };
            }
        }
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
