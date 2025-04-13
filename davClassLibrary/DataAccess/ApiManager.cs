using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Collections.Generic;

namespace davClassLibrary.DataAccess
{
    public class ApiManager
    {
        private static readonly Dictionary<string, HttpClient> httpClients = new Dictionary<string, HttpClient>();
        private static readonly Dictionary<string, GraphQLHttpClient> graphQLClients = new Dictionary<string, GraphQLHttpClient>();

        public static HttpClient GetHttpClient(string authorization = null)
        {
            if (authorization == null)
                authorization = Dav.AccessToken;

            if (httpClients.TryGetValue(authorization, out var httpClient))
                return httpClient;

            httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", authorization);
            httpClients.Add(authorization, httpClient);

            return httpClient;
        }

        public static GraphQLHttpClient GetGraphQLClient(string authorization = null)
        {
            if (authorization == null)
                authorization = Dav.AccessToken;

            if (graphQLClients.TryGetValue(authorization, out var graphQLClient))
                return graphQLClient;

            graphQLClient = new GraphQLHttpClient(Dav.NewApiBaseUrl, new NewtonsoftJsonSerializer());
            graphQLClient.HttpClient.DefaultRequestHeaders.Add("Authorization", authorization);

            return graphQLClient;
        }
    }
}
