using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class SessionsController
    {
        public static async Task<ApiResponse<SessionResponse>> CreateSession(
            string auth,
            string email,
            string password,
            int appId,
            string apiKey
        )
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth);

            var requestBodyDict = new Dictionary<string, object>
            {
                { "email", email },
                { "password", password },
                { "app_id", appId },
                { "api_key", apiKey }
            };

            var requestBody = new StringContent(
                JsonConvert.SerializeObject(requestBodyDict),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                response = await httpClient.PostAsync($"{Dav.ApiBaseUrl}/session", requestBody);
            }
            catch (Exception)
            {
                return new ApiResponse<SessionResponse> { Success = false, Status = 0 };
            }
            
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
                    var json = JsonConvert.DeserializeObject<ApiErrors>(responseData);
                    result.Errors = json.Errors;
                }
                catch (Exception) { }
            }

            return result;
        }

        public static async Task<ApiResponse<SessionResponse>> RenewSession(string accessToken)
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            try
            {
                response = await httpClient.PutAsync($"{Dav.ApiBaseUrl}/session/renew", new StringContent("{}", Encoding.UTF8, "application/json"));
            }
            catch (Exception)
            {
                return new ApiResponse<SessionResponse> { Success = false, Status = 0 };
            }
            
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
                    var json = JsonConvert.DeserializeObject<ApiErrors>(responseData);
                    result.Errors = json.Errors;
                }
                catch (Exception) { }
            }

            return result;
        }

        public static async Task<ApiResponse> DeleteSession(string accessToken)
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

            try
            {
                response = await httpClient.DeleteAsync($"{Dav.ApiBaseUrl}/session");
            }
            catch (Exception)
            {
                return new ApiResponse { Success = false, Status = 0 };
            }

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
                    var json = JsonConvert.DeserializeObject<ApiErrors>(responseData);
                    result.Errors = json.Errors;
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
