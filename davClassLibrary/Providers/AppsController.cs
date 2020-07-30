using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace davClassLibrary.Providers
{
    public static class AppsController
    {
        public static async Task<HttpResponse> CreateExceptionLog(
            int appId,
            string apiKey,
            string name,
            string message,
            string stackTrace,
            string appVersion,
            string osVersion,
            string deviceFamily,
            string locale
        )
        {
            string url = $"{Dav.ApiBaseUrl}/apps/app/{appId}/exception";

            // Set the Content-Type header
            HttpClient httpClient = new HttpClient();
            httpClient.DefaultRequestHeaders.Add("CONTENT_TYPE", "application/json");

            // Create the request body
            Dictionary<string, string> bodyDict = new Dictionary<string, string>
            {
                { "api_key", apiKey },
                { "name", name },
                { "message", message },
                { "stack_trace", stackTrace },
                { "app_version", appVersion },
                { "os_version", osVersion },
                { "device_family", deviceFamily },
                { "locale", locale }
            };
            var content = new StringContent(JsonConvert.SerializeObject(bodyDict), Encoding.UTF8, "application/json");

            // Send the request
            var httpResponse = await httpClient.PostAsync(url, content);

            return new HttpResponse((int)httpResponse.StatusCode, await httpResponse.Content.ReadAsStringAsync());
        }
    }
}

public class HttpResponse
{
    public int Status { get; set; }
    public string Data { get; set; }

    public HttpResponse(int status, string data)
    {
        Status = status;
        Data = data;
    }
}