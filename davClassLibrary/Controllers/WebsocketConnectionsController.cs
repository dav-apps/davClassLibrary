using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class WebsocketConnectionsController
    {
        public static async Task<ApiResponse<WebsocketConnectionResponse>> CreateWebsocketConnection(string accessToken)
        {
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            var response = await httpClient.PostAsync($"{Dav.ApiBaseUrl}/websocket_connection", new StringContent("{}", Encoding.UTF8, "application/json"));
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<WebsocketConnectionResponse>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var websocketConnectionData = JsonConvert.DeserializeObject<WebsocketConnectionResponseData>(responseData);
                    result.Data = websocketConnectionData.ToWebsocketConnectionResponse();
                }
                catch (Exception)
                {
                    result.Success = false;
                }
            }
            else
            {
                var errorResult = await Utils.HandleApiError(responseData);

                if (errorResult.Success)
                    return await CreateWebsocketConnection(accessToken);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }
    }

    public class WebsocketConnectionResponse
    {
        public string Token { get; set; }
    }

    public class WebsocketConnectionResponseData
    {
        public string token { get; set; }

        public WebsocketConnectionResponse ToWebsocketConnectionResponse()
        {
            return new WebsocketConnectionResponse
            {
                Token = token
            };
        }
    }
}
