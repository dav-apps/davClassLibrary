using System.Net.Http;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class SessionsController
    {
        public static async Task<ApiResponse<SessionResponse>> RenewSession(string accessToken)
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("AUTHORIZATION", accessToken);

            var response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/session/renew");
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<SessionResponse> { Status = (int)response.StatusCode };

            if (response.IsSuccessStatusCode)
            {
                var sessionResponseData = Utils.SerializeJson<SessionResponseData>(responseData);
                result.Data = sessionResponseData.ToSessionResponse();
            }
            else
            {
                var errors = Utils.SerializeJson<ApiError[]>(responseData);
                result.Errors = errors;
            }

            return result;
        }
    }

    public class SessionResponse
    {
        public string AccessToken { get; set; }
        public string WebsiteAccessToken { get; set; }
    }

    public class SessionResponseData
    {
        public string access_token { get; set; }
        public string website_access_token { get; set; }

        public SessionResponse ToSessionResponse()
        {
            return new SessionResponse
            {
                AccessToken = access_token,
                WebsiteAccessToken = website_access_token
            };
        }
    }
}
