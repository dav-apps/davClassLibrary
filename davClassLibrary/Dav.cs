using davClassLibrary.Common;
using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace davClassLibrary
{
    public static class Dav
    {
        public static bool IsLoggedIn = false;
        public static User User = new User
        {
            Email = "",
            FirstName = "",
            TotalStorage = 0,
            UsedStorage = 0,
            Plan = Plan.Free,
            ProfileImageEtag = ""
        };

        public static Environment Environment { get; internal set; }
        public static int AppId { get; internal set; }
        public static List<string> TableNames { get; internal set; }
        public static List<string> ParallelTableNames { get; internal set; }
        public static string DataPath { get; internal set; }

        public static string AccessToken { get; set; }
        private const string ApiBaseUrlProduction = "https://dav-api-ax6gp.ondigitalocean.app";
        private const string ApiBaseUrlDevelopment = "https://dav-api-staging-kb5tf.ondigitalocean.app";
        public static string ApiBaseUrl => Environment == Environment.Production ? ApiBaseUrlProduction : ApiBaseUrlDevelopment;

        private static bool isSyncing = false;

        internal static readonly HttpClient httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(60) };
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
            List<string> tableNames,
            List<string> parallelTableNames,
            string dataPath
        )
        {
            Environment = environment;
            AppId = appId;
            TableNames = tableNames;
            ParallelTableNames = parallelTableNames;
            DataPath = dataPath;

            LoadUser();
        }

        private static void LoadUser()
        {
            // Get the access token and session upload status from the local settings
            AccessToken = SettingsManager.GetAccessToken();
            var sessionUploadStatus = SettingsManager.GetSessionUploadStatus();

            if (string.IsNullOrEmpty(AccessToken) || sessionUploadStatus == SessionUploadStatus.Deleted)
            {
                _ = SyncManager.SessionSyncPush();
                IsLoggedIn = false;
                return;
            }
            IsLoggedIn = true;

            // Load the user
            SyncManager.LoadUser();
        }

        public static async Task SyncData()
        {
            if (!IsLoggedIn || isSyncing) return;
            isSyncing = true;

            // Sync the user
            await SyncManager.UserSync();

            // Sync the table objects
            bool syncSuccess = await SyncManager.Sync();
            bool syncPushSuccess = await SyncManager.SyncPush();

            if (!syncSuccess || !syncPushSuccess)
            {
                ProjectInterface.Callbacks.SyncFinished();
                isSyncing = false;
                return;
            }

            SyncManager.StartFileDownloads();

            ProjectInterface.Callbacks.SyncFinished();
            isSyncing = false;
        }

        public static void Login(string accessToken)
        {
            if (string.IsNullOrEmpty(accessToken)) return;

            // Save the access token in the local settings
            SettingsManager.SetAccessToken(accessToken);
            SettingsManager.SetSessionUploadStatus(SessionUploadStatus.UpToDate);

            LoadUser();
            _ = SyncData();
        }

        public static void Logout()
        {
            AccessToken = null;
            IsLoggedIn = false;

            // Remove the user
            SettingsManager.RemoveUser();

            // Delete the profile image
            // TODO

            // Set the session UploadStatus to Deleted
            SettingsManager.SetSessionUploadStatus(SessionUploadStatus.Deleted);

            // Start deleting the session on the server
            _ = SyncManager.SessionSyncPush();
        }
    }
}
