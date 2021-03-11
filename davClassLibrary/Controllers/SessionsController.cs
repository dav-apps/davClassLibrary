using Newtonsoft.Json;
using System;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class SessionsController
    {
        public static async Task<ApiResponse<SessionResponse>> RenewSession(string accessToken)
        {
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Dav.AccessToken);

            var response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/session/renew");
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<SessionResponse>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var sessionResponseData = JsonConvert.DeserializeObject<SessionResponseData>(responseData);
                    result.Data = sessionResponseData.ToSessionResponse();
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
                    var errors = JsonConvert.DeserializeObject<ApiError[]>(responseData);
                    result.Errors = errors;
                }
                catch (Exception) { }
            }

            return result;
        }

        public static async Task<ApiResponse> DeleteSession(string accessToken)
        {
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Dav.AccessToken);

            var response = await httpClient.DeleteAsync($"{Dav.ApiBaseUrl}/session");

            var result = new ApiResponse
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (!response.IsSuccessStatusCode)
            {
                try
                {
                    string responseData = await response.Content.ReadAsStringAsync();
                    var errors = JsonConvert.DeserializeObject<ApiError[]>(responseData);
                    result.Errors = errors;
                }
                catch (Exception) { }
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
