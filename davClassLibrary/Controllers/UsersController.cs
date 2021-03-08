using davClassLibrary.Models;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace davClassLibrary.Controllers
{
    public static class UsersController
    {
        public static async Task<ApiResponse<User>> GetUser()
        {
            var httpClient = Dav.httpClient;
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(Dav.AccessToken);

            var response = await httpClient.GetAsync($"{Dav.ApiBaseUrl}/user");
            string responseData = await response.Content.ReadAsStringAsync();

            var result = new ApiResponse<User>
            {
                Success = response.IsSuccessStatusCode,
                Status = (int)response.StatusCode
            };

            if (response.IsSuccessStatusCode)
            {
                var userData = Utils.SerializeJson<UserData>(responseData);
                result.Data = userData.ToUser();
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
    }
}
