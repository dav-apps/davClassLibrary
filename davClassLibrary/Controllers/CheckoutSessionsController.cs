using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using GraphQL;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class CheckoutSessionsController
    {
        public static async Task<GraphQLApiResponse<CheckoutSessionResource>> CreateSubscriptionCheckoutSession(
            string queryData,
            Plan plan,
            string successUrl,
            string cancelUrl
        )
        {
            var createSubscriptionCheckoutSessionRequest = new GraphQLRequest
            {
                OperationName = "CreateSubscriptionCheckoutSession",
                Query = $@"
                    mutation CreateSubscriptionCheckoutSession(
                        $plan: Plan!
					    $successUrl: String!
					    $cancelUrl: String!
                    ) {{
                        createSubscriptionCheckoutSession(
                            plan: $plan
						    successUrl: $successUrl
						    cancelUrl: $cancelUrl
                        ) {{
                            {queryData}
                        }}
                    }}
                ",
                Variables = new
                {
                    plan,
                    successUrl,
                    cancelUrl
                }
            };

            try
            {
                var response = await ApiManager
                    .GetGraphQLClient()
                    .SendMutationAsync<CreateSubscriptionCheckoutSessionResponse>(
                        createSubscriptionCheckoutSessionRequest
                    );

                if (response.Errors != null && response.Errors.Any())
                {
                    var errorCodes = Utils.GetErrorCodesOfGraphQLError(response.Errors);
                    var renewSessionErrors = await Utils.HandleGraphQLApiErrors(errorCodes);

                    if (renewSessionErrors != null)
                    {
                        return new GraphQLApiResponse<CheckoutSessionResource>
                        {
                            Success = false,
                            Errors = renewSessionErrors
                        };
                    }

                    return await CreateSubscriptionCheckoutSession(
                        queryData,
                        plan,
                        successUrl,
                        cancelUrl
                    );
                }

                return new GraphQLApiResponse<CheckoutSessionResource>
                {
                    Success = true,
                    Data = response.Data.CreateSubscriptionCheckoutSession
                };
            }
            catch (Exception)
            {
                return new GraphQLApiResponse<CheckoutSessionResource> { Success = false };
            }
        }
    }
}

public class CreateSubscriptionCheckoutSessionResponse
{
    public CheckoutSessionResource CreateSubscriptionCheckoutSession { get; set; }
}
