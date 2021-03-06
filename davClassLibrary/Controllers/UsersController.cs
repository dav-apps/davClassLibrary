using davClassLibrary.Models;
using System.Net.Http;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class UsersController
    {
        public static async Task<ApiResponse<User>> GetUser()
        {
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("AUTHORIZATION", Dav.AccessToken);

            var response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/user");
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<User> { Status = (int)response.StatusCode };

            if (response.IsSuccessStatusCode)
            {
                var userData = Utils.SerializeJson<UserData>(responseData);
                result.Data = userData.ToUser();
            }
            else
            {
                var errorResult = await Utils.HandleApiError(responseData);

                if (errorResult.Success)
                {
                    return await GetUser();
                }
                else
                {
                    result.Errors = errorResult.Errors;
                }
            }

            return result;
        }
    }
}
