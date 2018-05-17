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

        public const string ApiBaseUrl = "localhost:3111/v1/";
        public const string GetUserUrl = "auth/user";

        public static string ApiKey = "";         
        public static string DataPath;
    }
}
