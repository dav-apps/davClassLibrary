using davClassLibrary.Common;

namespace davClassLibrary.DataAccess
{
    public static class SettingsManager
    {
        #region AccessToken
        public static void SetAccessToken(string accessToken)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.accessTokenKey, accessToken);
        }

        public static string GetAccessToken()
        {
            var accessToken = ProjectInterface.LocalDataSettings.GetString(Constants.accessTokenKey);
            if (!string.IsNullOrEmpty(accessToken)) return accessToken;

            return ProjectInterface.LocalDataSettings.GetString(Constants.jwtKey);
        }
        #endregion

        #region SessionUploadStatus
        public static void SetSessionUploadStatus(SessionUploadStatus sessionUploadStatus)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.sessionUploadStatusKey, (int)sessionUploadStatus);
        }

        public static SessionUploadStatus GetSessionUploadStatus()
        {
            return (SessionUploadStatus)ProjectInterface.LocalDataSettings.GetInt(Constants.sessionUploadStatusKey);
        }
        #endregion

        #region Email
        public static void SetEmail(string email)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.emailKey, email);
        }

        public static string GetEmail()
        {
            return ProjectInterface.LocalDataSettings.GetString(Constants.emailKey);
        }
        #endregion

        #region FirstName
        public static void SetFirstName(string firstName)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.firstNameKey, firstName);
        }

        public static string GetFirstName()
        {
            var firstName = ProjectInterface.LocalDataSettings.GetString(Constants.firstNameKey);
            if (!string.IsNullOrEmpty(firstName)) return firstName;

            return ProjectInterface.LocalDataSettings.GetString(Constants.usernameKey);
        }
        #endregion

        #region TotalStorage
        public static void SetTotalStorage(long totalStorage)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.totalStorageKey, totalStorage);
        }

        public static long GetTotalStorage()
        {
            return ProjectInterface.LocalDataSettings.GetLong(Constants.totalStorageKey);
        }
        #endregion

        #region UsedStorage
        public static void SetUsedStorage(long usedStorage)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.usedStorageKey, usedStorage);
        }

        public static long GetUsedStorage()
        {
            return ProjectInterface.LocalDataSettings.GetLong(Constants.usedStorageKey);
        }
        #endregion

        #region Plan
        public static void SetPlan(Plan plan)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.planKey, (int)plan);
        }

        public static Plan GetPlan()
        {
            return (Plan)ProjectInterface.LocalDataSettings.GetInt(Constants.planKey);
        }
        #endregion

        #region ProfileImageEtag
        public static void SetProfileImageEtag(string profileImageEtag)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.profileImageEtagKey, profileImageEtag);
        }

        public static string GetProfileImageEtag()
        {
            return ProjectInterface.LocalDataSettings.GetString(Constants.profileImageEtagKey);
        }
        #endregion

        #region TableEtag
        public static void SetTableEtag(int tableId, string tableEtag)
        {
            ProjectInterface.LocalDataSettings.Set(string.Format(Constants.tableEtagKey, tableId), tableEtag);
        }

        public static string GetTableEtag(int tableId)
        {
            return ProjectInterface.LocalDataSettings.GetString(string.Format(Constants.tableEtagKey, tableId));
        }
        #endregion

        public static void RemoveSession()
        {
            ProjectInterface.LocalDataSettings.Remove(Constants.accessTokenKey);
            ProjectInterface.LocalDataSettings.Remove(Constants.sessionUploadStatusKey);
        }

        public static void RemoveUser()
        {
            ProjectInterface.LocalDataSettings.Remove(Constants.emailKey);
            ProjectInterface.LocalDataSettings.Remove(Constants.firstNameKey);
            ProjectInterface.LocalDataSettings.Remove(Constants.totalStorageKey);
            ProjectInterface.LocalDataSettings.Remove(Constants.usedStorageKey);
            ProjectInterface.LocalDataSettings.Remove(Constants.planKey);
            ProjectInterface.LocalDataSettings.Remove(Constants.profileImageEtagKey);
        }
    }
}
