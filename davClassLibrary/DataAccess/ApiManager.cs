using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Diagnostics;
using System.IO;

namespace davClassLibrary.DataAccess
{
    public class ApiManager
    {
        // Public CDN downloads must never inherit the signed-in user's token.
        private static readonly HttpClient publicDownloadClient = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
        private static readonly Dictionary<string, HttpClient> httpClients = new Dictionary<string, HttpClient>();
        private static readonly Dictionary<string, GraphQLHttpClient> graphQLClients = new Dictionary<string, GraphQLHttpClient>();

        public static HttpClient GetHttpClient(string authorization = null)
        {
            if (authorization == null)
                authorization = Dav.AccessToken;

            if (httpClients.TryGetValue(authorization ?? "", out var httpClient))
                return httpClient;

            httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };

            if (authorization != null)
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorization);

            httpClients.Add(authorization ?? "", httpClient);
            return httpClient;
        }

        public static GraphQLHttpClient GetGraphQLClient(string authorization = null)
        {
            if (authorization == null)
                authorization = Dav.AccessToken;

            if (graphQLClients.TryGetValue(authorization ?? "", out var graphQLClient))
                return graphQLClient;

            graphQLClient = new GraphQLHttpClient(Dav.ApiBaseUrl, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.Timeout = TimeSpan.FromMinutes(60);

            if (authorization != null)
                graphQLClient.HttpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorization);

            graphQLClients.Add(authorization ?? "", graphQLClient);
            return graphQLClient;
        }

        public static Task<bool> DownloadFile(string url, string filePath, IProgress<int> progress = null)
        {
            return DownloadFileCore(GetHttpClient(), url, filePath, progress);
        }

        public static Task<bool> DownloadPublicFile(string url, string filePath, IProgress<int> progress = null)
        {
            return DownloadFileCore(publicDownloadClient, url, filePath, progress);
        }

        private static async Task<bool> DownloadFileCore(HttpClient httpClient, string url, string filePath, IProgress<int> progress)
        {
            HttpResponseMessage response;

            try
            {
                response = await httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            }
            catch (Exception e)
            {
                Debug.WriteLine(e);
                return false;
            }

            if (!response.IsSuccessStatusCode)
                return false;

            long contentLength = response.Content.Headers.ContentLength.GetValueOrDefault();
            FileStream fileStream = null;

            try
            {
                if (File.Exists(filePath))
                    File.Delete(filePath);

                using (var responseStream = await response.Content.ReadAsStreamAsync())
                {
                    fileStream = File.Create(filePath);
                    var buffer = new byte[8192];
                    int read;
                    long offset = 0;

                    do
                    {
                        read = await responseStream.ReadAsync(buffer, 0, buffer.Length);
                        await fileStream.WriteAsync(buffer, 0, read);
                        offset += read;

                        if (progress != null && offset != 0 && read != 0 && contentLength != 0)
                            progress.Report((int)Math.Floor((double)offset / contentLength * 100));
                    } while (read != 0);

                    await fileStream.FlushAsync();
                    fileStream.Close();
                }
            }
            catch (Exception e)
            {
                fileStream?.Close();
                Debug.WriteLine(e);
                return false;
            }

            return true;
        }
    }
}
