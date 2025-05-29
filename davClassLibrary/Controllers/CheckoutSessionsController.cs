using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using GraphQL;
using System;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class CheckoutSessionsController
    {
        public static async Task<GraphQLResponse<CreateSubscriptionCheckoutSessionResponse>> CreateSubscriptionCheckoutSession(
            string queryData,
            int plan,
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
                return await ApiManager.GetGraphQLClient().SendMutationAsync<CreateSubscriptionCheckoutSessionResponse>(createSubscriptionCheckoutSessionRequest);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}

public class CreateSubscriptionCheckoutSessionResponse
{
    public CheckoutSessionResource CreateSubscriptionCheckoutSession { get; set; }
}
