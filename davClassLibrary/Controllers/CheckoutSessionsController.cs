using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class CheckoutSessionsController
    {
        public static async Task<ApiResponse<CreateCheckoutSessionResponse>> CreateCheckoutSession(
            int plan,
            string successUrl,
            string cancelUrl
        )
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            var requestBodyDict = new Dictionary<string, object>
            {
                { "plan", plan },
                { "success_url", successUrl },
                { "cancel_url", cancelUrl }
            };

            var requestBody = new StringContent(
                JsonConvert.SerializeObject(requestBodyDict),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                response = await httpClient.PostAsync($"{Dav.ApiBaseUrl}/checkout_session", requestBody);
            }
            catch (Exception)
            {
                return new ApiResponse<CreateCheckoutSessionResponse> { Success = false, Status = 0 };
            }

            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<CreateCheckoutSessionResponse>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var createCheckoutSessionResponseData = JsonConvert.DeserializeObject<CreateCheckoutSessionResponseData>(responseData);
                    result.Data = createCheckoutSessionResponseData.ToCreateCheckoutSessionResponse();
                }
                catch (Exception)
                {
                    result.Success = false;
                }
            }
            else
            {
                try
                {
                    var json = JsonConvert.DeserializeObject<ApiErrors>(responseData);
                    result.Errors = json.Errors;
                }
                catch (Exception) { }
            }

            return result;
        }
    }

    public class CreateCheckoutSessionResponse
    {
        public string SessionUrl { get; set; }
    }

    public class CreateCheckoutSessionResponseData
    {
        public string session_url { get; set; }

        public CreateCheckoutSessionResponse ToCreateCheckoutSessionResponse()
        {
            return new CreateCheckoutSessionResponse
            {
                SessionUrl = session_url
            };
        }
    }
}
