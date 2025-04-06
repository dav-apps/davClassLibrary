using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Net.Http.Headers;
using System.Net.Http;

namespace davClassLibrary.DataAccess
{
    public class ApiManager
    {
        private static HttpClient httpClient;
        public static HttpClient HttpClient
        {
            get
            {
                if (httpClient == null)
                    CreateHttpClient(Dav.AccessToken);

                return httpClient;
            }
        }
        private static GraphQLHttpClient graphQLClient;
        public static GraphQLHttpClient GraphQLClient
        {
            get
            {
                if (graphQLClient == null)
                    CreateGraphQLClient(Dav.NewApiBaseUrl, Dav.AccessToken);

                return graphQLClient;
            }
        }

        private static void CreateHttpClient(string accessToken)
        {
            httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        }

        private static void CreateGraphQLClient(string apiBaseUrl, string accessToken)
        {
            graphQLClient = new GraphQLHttpClient(apiBaseUrl, new NewtonsoftJsonSerializer());
            if (accessToken != null) graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", accessToken);
        }
    }
}
