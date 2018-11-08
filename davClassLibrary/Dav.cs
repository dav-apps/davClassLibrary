using davClassLibrary.Common;
using davClassLibrary.DataAccess;

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

        private const string ApiBaseUrlProduction = "https://dav-backend-staging.herokuapp.com/v1/";
        private const string ApiBaseUrlDevelopment = "https://33920996.ngrok.io/v1/";
        public static string ApiBaseUrl => Environment == DavEnvironment.Production ? ApiBaseUrlProduction : ApiBaseUrlDevelopment;
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
        public static DavEnvironment Environment
        {
            get { return ProjectInterface.GeneralMethods.GetEnvironment(); }
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
