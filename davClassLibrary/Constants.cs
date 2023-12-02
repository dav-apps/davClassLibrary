namespace davClassLibrary
{
    internal static class Constants
    {
        // Old settings keys
        internal const string jwtKey = "jwt";
        internal const string usernameKey = "username";
        internal const string avatarEtagKey = "avatarEtag";

        // New settings keys
        internal const string accessTokenKey = "accessToken";
        internal const string sessionUploadStatusKey = "sessionUploadStatus";
        internal const string idKey = "id";
        internal const string emailKey = "email";
        internal const string firstNameKey = "firstName";
        internal const string totalStorageKey = "totalStorage2";
        internal const string usedStorageKey = "usedStorage2";
        internal const string planKey = "plan2";
        internal const string profileImageEtagKey = "profileImageEtag";
        internal const string tableEtagKey = "tableEtag:{0}";

        internal const string extPropertyName = "ext";
        internal const string tableObjectUpdateChannelName = "TableObjectUpdateChannel";
        internal const string profileImageFileName = "profileImage";

        internal const int maxPropertiesUploadCount = 100;
    }
}
