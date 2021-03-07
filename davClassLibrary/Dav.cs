using davClassLibrary.DataAccess;
using System.Collections.Generic;
using System.Net.Http;

namespace davClassLibrary
{
    public static class Dav
    {
        public static Environment Environment { get; internal set; }
        public static int AppId { get; internal set; }
        public static List<int> TableIds { get; internal set; }
        public static List<int> ParallelTableIds { get; internal set; }
        public static string DataPath { get; internal set; }

        public static string AccessToken { get; internal set; }
        private const string ApiBaseUrlProduction = "https://dav-backend.herokuapp.com/v1";
        private const string ApiBaseUrlDevelopment = "https://829cc76acebc.ngrok.io/v1";
        public static string ApiBaseUrl => Environment == Environment.Production ? ApiBaseUrlProduction : ApiBaseUrlDevelopment;

        internal static readonly HttpClient httpClient = new HttpClient();
        private static DavDatabase database;
        public static DavDatabase Database
        {
            get
            {
                if (database == null)
                    database = new DavDatabase();
                return database;
            }
        }

        public static void Init(
            Environment environment,
            int appId,
            List<int> tableIds,
            List<int> parallelTableIds,
            string dataPath
        )
        {
            Environment = environment;
            AppId = appId;
            TableIds = tableIds;
            ParallelTableIds = parallelTableIds;
            DataPath = dataPath;
        }
    }
}
