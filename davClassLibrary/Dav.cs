using davClassLibrary.Common;
using davClassLibrary.DataAccess;
using System;

namespace davClassLibrary
{
    public class Dav
    {
        public const string jwtKey = "jwt";
        public const string emailKey = "email";
        public const string usernameKey = "username";
        public const string totalStorageKey = "totalStorage";
        public const string usedStorageKey = "usedStorage";
        public const string planKey = "plan";
        public const string avatarEtagKey = "avatarEtag";

        // Websocket keys
        public const string uuidKey = "uuid";
        public const string changeKey = "change";

        public const string ExportDataFileName = "data.json";
        public const string GetUserUrl = "auth/user";

        //public const string ApiBaseUrl = "https://dav-backend-staging.herokuapp.com/v1/";
        public const string ApiBaseUrl = "https://d38c87e2.ngrok.io/v1/";
        public static string DataPath
        {
            get { return ProjectInterface.RetrieveConstants.GetDataPath(); }
        }
        public static string ApiKey
        {
            get { return ProjectInterface.RetrieveConstants.GetApiKey(); }
        }
        public static int AppId
        {
            get { return ProjectInterface.RetrieveConstants.GetAppId(); }
        }

        private static DavDatabase database;

        public static DavDatabase Database
        {
            get
            {
                if (database == null)
                {
                    database = new DavDatabase();
                }
                return database;
            }
        }
    }
}
