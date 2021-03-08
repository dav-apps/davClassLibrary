using davClassLibrary.Common;
using System;

namespace davClassLibrary.DataAccess
{
    internal static class SettingsManager
    {
        #region AccessToken
        internal static void SetAccessToken(string accessToken)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.accessTokenKey, accessToken);
        }

        internal static string GetAccessToken()
        {
            var accessToken = ProjectInterface.LocalDataSettings.GetString(Constants.accessTokenKey);
            if (!string.IsNullOrEmpty(accessToken)) return accessToken;

            return ProjectInterface.LocalDataSettings.GetString(Constants.jwtKey);
        }
        #endregion

        #region SessionUploadStatus
        internal static void SetSessionUploadStatus(SessionUploadStatus sessionUploadStatus)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.sessionUploadStatusKey, (int)sessionUploadStatus);
        }

        internal static SessionUploadStatus GetSessionUploadStatus()
        {
            return (SessionUploadStatus)ProjectInterface.LocalDataSettings.GetInt(Constants.sessionUploadStatusKey);
        }
        #endregion

        #region Email
        internal static void SetEmail(string email)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.emailKey, email);
        }

        internal static string GetEmail()
        {
            return ProjectInterface.LocalDataSettings.GetString(Constants.emailKey);
        }
        #endregion

        #region FirstName
        internal static void SetFirstName(string firstName)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.firstNameKey, firstName);
        }

        internal static string GetFirstName()
        {
            var firstName = ProjectInterface.LocalDataSettings.GetString(Constants.firstNameKey);
            if (!String.IsNullOrEmpty(firstName)) return firstName;

            return ProjectInterface.LocalDataSettings.GetString(Constants.usernameKey);
        }
        #endregion

        #region TotalStorage
        internal static void SetTotalStorage(long totalStorage)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.totalStorageKey, totalStorage);
        }

        internal static long GetTotalStorage()
        {
            return ProjectInterface.LocalDataSettings.GetLong(Constants.totalStorageKey);
        }
        #endregion

        #region UsedStorage
        internal static void SetUsedStorage(long usedStorage)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.usedStorageKey, usedStorage);
        }

        internal static long GetUsedStorage()
        {
            return ProjectInterface.LocalDataSettings.GetLong(Constants.usedStorageKey);
        }
        #endregion

        #region Plan
        internal static void SetPlan(Plan plan)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.planKey, (int)plan);
        }

        internal static Plan GetPlan()
        {
            return (Plan)ProjectInterface.LocalDataSettings.GetInt(Constants.planKey);
        }
        #endregion

        #region ProfileImageEtag
        internal static void SetProfileImageEtag(string profileImageEtag)
        {
            ProjectInterface.LocalDataSettings.Set(Constants.profileImageEtagKey, profileImageEtag);
        }

        internal static string GetProfileImageEtag()
        {
            return ProjectInterface.LocalDataSettings.GetString(Constants.profileImageEtagKey);
        }
        #endregion

        internal static void RemoveSession()
        {
            ProjectInterface.LocalDataSettings.Remove(Constants.accessTokenKey);
            ProjectInterface.LocalDataSettings.Remove(Constants.sessionUploadStatusKey);
        }

        internal static void RemoveUser()
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
