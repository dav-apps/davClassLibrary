using davClassLibrary.Models;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class UsersController
    {
        public static async Task<ApiResponse<User>> GetUser()
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            try
            {
                response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/user");
            }
            catch (Exception)
            {
                return new ApiResponse<User> { Success = false, Status = 0 };
            }
            
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<User>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var userData = JsonConvert.DeserializeObject<UserData>(responseData);
                    result.Data = userData.ToUser();
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
                    return await GetUser();
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }

        public static async Task<ApiResponse> GetProfileImageOfUser(string filePath)
        {
            HttpResponseMessage response;
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", Dav.AccessToken);

            try
            {
                response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/user/profile_image");
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

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    File.WriteAllBytes(filePath, await response.Content.ReadAsByteArrayAsync());
                }
                catch (Exception)
                {
                    result.Success = false;
                }
            }
            else
            {
                string responseData = await response.Content.ReadAsStringAsync();
                var errorResult = await Utils.HandleApiError(responseData);

                if (errorResult.Success)
                    return await GetProfileImageOfUser(filePath);
                else
                    result.Errors = errorResult.Errors;
            }

            return result;
        }
    }
}
