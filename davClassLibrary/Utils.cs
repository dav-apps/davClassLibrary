using davClassLibrary.Controllers;
using davClassLibrary.DataAccess;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace davClassLibrary
{
    public static class Utils
    {
        public static T SerializeJson<T>(string json)
        {
            var serializer = new DataContractJsonSerializer(typeof(T));
            var ms = new MemoryStream(Encoding.UTF8.GetBytes(json));
            return (T)serializer.ReadObject(ms);
        }

        internal static async Task<HandleApiErrorResult> HandleApiError(string responseData)
        {
            var errors = SerializeJson<ApiError[]>(responseData);

            if (errors.Length > 0 && errors[0].Code == ErrorCodes.AccessTokenMustBeRenewed)
            {
                // Renew the session
                var renewSessionResult = await SessionsController.RenewSession(Dav.AccessToken);

                if (renewSessionResult.Status == 200)
                {
                    // Update the access token and save it in the local settings
                    Dav.AccessToken = renewSessionResult.Data.AccessToken;
                    SettingsManager.SetAccessToken(Dav.AccessToken);

                    return new HandleApiErrorResult { Success = true, Errors = null };
                }
                else
                {
                    return new HandleApiErrorResult { Success = false, Errors = renewSessionResult.Errors };
                }
            }

            return new HandleApiErrorResult { Success = false, Errors = errors };
        }
    }
}
