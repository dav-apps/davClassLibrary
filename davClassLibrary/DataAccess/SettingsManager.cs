using davClassLibrary.Common;
using System;

namespace davClassLibrary.DataAccess
{
    internal static class SettingsManager
    {
        #region AccessToken
        internal static void SetAccessToken(string accessToken)
        {
            ProjectInterface.LocalDataSettings.SetValue(Constants.accessTokenKey, accessToken);
        }

        internal static string GetAccessToken()
        {
            var accessToken = ProjectInterface.LocalDataSettings.GetStringValue(Constants.accessTokenKey);
            if (!String.IsNullOrEmpty(accessToken)) return accessToken;

            return ProjectInterface.LocalDataSettings.GetStringValue(Constants.jwtKey);
        }
        #endregion

        #region Email
        internal static void SetEmail(string email)
        {
            ProjectInterface.LocalDataSettings.SetValue(Constants.emailKey, email);
        }

        internal static string GetEmail()
        {
            return ProjectInterface.LocalDataSettings.GetStringValue(Constants.emailKey);
        }
        #endregion

        #region FirstName
        internal static void SetFirstName(string firstName)
        {
            ProjectInterface.LocalDataSettings.SetValue(Constants.firstNameKey, firstName);
        }

        internal static string GetFirstName()
        {
            var firstName = ProjectInterface.LocalDataSettings.GetStringValue(Constants.firstNameKey);
            if (!String.IsNullOrEmpty(firstName)) return firstName;

            return ProjectInterface.LocalDataSettings.GetStringValue(Constants.usernameKey);
        }
        #endregion

        #region TotalStorage
        internal static void SetTotalStorage(long totalStorage)
        {
            ProjectInterface.LocalDataSettings.SetValue(Constants.totalStorageKey, totalStorage);
        }

        internal static long GetTotalStorage()
        {
            return ProjectInterface.LocalDataSettings.GetLongValue(Constants.totalStorageKey);
        }
        #endregion

        #region UsedStorage
        internal static void SetUsedStorage(long usedStorage)
        {
            ProjectInterface.LocalDataSettings.SetValue(Constants.usedStorageKey, usedStorage);
        }

        internal static long GetUsedStorage()
        {
            return ProjectInterface.LocalDataSettings.GetLongValue(Constants.usedStorageKey);
        }
        #endregion

        #region Plan
        internal static void SetPlan(Plan plan)
        {
            ProjectInterface.LocalDataSettings.SetValue(Constants.planKey, (int)plan);
        }

        internal static Plan GetPlan()
        {
            return (Plan)ProjectInterface.LocalDataSettings.GetIntValue(Constants.planKey);
        }
        #endregion

        #region ProfileImageEtag
        internal static void SetProfileImageEtag(string profileImageEtag)
        {
            ProjectInterface.LocalDataSettings.SetValue(Constants.profileImageEtagKey, profileImageEtag);
        }

        internal static string GetProfileImageEtag()
        {
            return ProjectInterface.LocalDataSettings.GetStringValue(Constants.profileImageEtagKey);
        }
        #endregion
    }
}
