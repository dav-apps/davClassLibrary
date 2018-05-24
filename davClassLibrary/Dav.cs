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

        //public const string ApiBaseUrl = "https://dav-backend.herokuapp.com/v1/";
        public const string ApiBaseUrl = "https://61eacf05.ngrok.io/v1/";
        public const string GetUserUrl = "auth/user";
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
