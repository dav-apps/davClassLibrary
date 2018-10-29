using davClassLibrary.Common;
using davClassLibrary.DataAccess;
using System;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Threading.Tasks;

namespace davClassLibrary.Models
{
    public class DavUser
    {
        public string Email
        {
            get { return GetEmail(); }
            set { SetEmail(value); }
        }
        public string Username
        {
            get { return GetUsername(); }
            set { SetUsername(value); }
        }
        public long TotalStorage
        {
            get { return GetTotalStorage(); }
            set { SetTotalStorage(value); }
        }
        public long UsedStorage
        {
            get { return GetUsedStorage(); }
            set { SetUsedStorage(value); }
        }
        public DavPlan Plan
        {
            get { return GetPlan(); }
            set { SetPlan(value); }
        }
        public FileInfo Avatar { get; set; }
        public string AvatarEtag
        {
            get { return GetAvatarEtag(); }
            set { SetAvatarEtag(value); }
        }
        public bool IsLoggedIn { get; set; }
        public string JWT
        {
            get { return GetJWT(); }
            set { SetJWT(value); }
        }

        public enum DavPlan
        {
            Free,
            Plus
        }

        public DavUser()
        {
            // Get the user information from the local settings
            if(!String.IsNullOrEmpty(ProjectInterface.LocalDataSettings.GetValue(Dav.jwtKey)))
            {
                // User is logged in. Get the user information
                GetUserInformation();

                DownloadUserInformation();
                DataManager.Sync();
            }
            else
            {
                IsLoggedIn = false;
            }
        }

        public async Task Login(string jwt)
        {
            JWT = jwt;
            IsLoggedIn = true;
            if(await DownloadUserInformation())
            {
                DataManager.Sync();
            }
            else
            {
                Logout();
            }
        }

        public void Logout()
        {
            // Clear all values
            IsLoggedIn = false;
            SetJWT(null);
            SetEmail(null);
            SetUsername(null);
            SetTotalStorage(0);
            SetUsedStorage(0);
            SetPlan(DavPlan.Free);
            SetAvatarEtag(null);

            // Delete the avatar
            DeleteAvatar();
        }

        private async Task<bool> DownloadUserInformation()
        {
            if (IsLoggedIn)
            {
                var getResult = await DataManager.HttpGet(JWT, Dav.GetUserUrl);
                
                if(getResult.Key)
                {
                    // Deserialize the json and create a user object
                    var serializer = new DataContractJsonSerializer(typeof(DavUserData));
                    var ms = new MemoryStream(Encoding.UTF8.GetBytes(getResult.Value));
                    var dataReader = (DavUserData)serializer.ReadObject(ms);

                    Email = dataReader.email;
                    Username = dataReader.username;
                    TotalStorage = dataReader.total_storage;
                    UsedStorage = dataReader.used_storage;
                    Plan = ParseIntToDavPlan(dataReader.plan);
                    string newAvatarEtag = dataReader.avatar_etag;

                    string avatarFileName = "avatar.png";
                    var avatarFilePath = Path.Combine(Dav.DataPath, avatarFileName);

                    if (!String.Equals(AvatarEtag, newAvatarEtag) || !File.Exists(avatarFilePath))
                    {
                        // Download the new avatar
                        DownloadAvatar(dataReader.avatar);
                    }

                    Avatar = new FileInfo(Path.Combine(Dav.DataPath, avatarFileName));
                    AvatarEtag = newAvatarEtag;

                    // Save new values in local settings
                    SetUserInformation();
                    return true;
                }
                else
                {
                    Debug.WriteLine(getResult.Value);
                    return false;
                }
            }
            return false;
        }

        private void DownloadAvatar(string avatarUrl)
        {
            using (var client = new WebClient())
            {
                client.DownloadFile(avatarUrl, Path.Combine(Dav.DataPath, "avatar.png"));
            }
        }

        private void DeleteAvatar()
        {
            string path = Path.Combine(Dav.DataPath, "avatar.png");
            if(File.Exists(path))
                File.Delete(path);
        }

        private void GetUserInformation()
        {
            IsLoggedIn = true;
            Email = GetEmail();
            Username = GetUsername();
            TotalStorage = GetTotalStorage();
            UsedStorage = GetUsedStorage();
            Plan = GetPlan();
            JWT = GetJWT();
            AvatarEtag = GetAvatarEtag();
            Avatar = GetAvatar();
        }

        public static string GetEmail()
        {
            return ProjectInterface.LocalDataSettings.GetValue(Dav.emailKey);
        }

        public static string GetUsername()
        {
            return ProjectInterface.LocalDataSettings.GetValue(Dav.usernameKey);
        }

        public static long GetTotalStorage()
        {
            long totalStorage = 0;
            long.TryParse(ProjectInterface.LocalDataSettings.GetValue(Dav.totalStorageKey), out totalStorage);
            return totalStorage;
        }

        public static long GetUsedStorage()
        {
            long usedStorage = 0;
            long.TryParse(ProjectInterface.LocalDataSettings.GetValue(Dav.usedStorageKey), out usedStorage);
            return usedStorage;
        }

        public static DavPlan GetPlan()
        {
            var plan = ProjectInterface.LocalDataSettings.GetValue(Dav.planKey);
            if(plan != null)
            {
                var planInt = 0;
                int.TryParse(plan, out planInt);
                return ParseIntToDavPlan(planInt);
            }
            else
            {
                return DavPlan.Free;
            }
        }

        public static string GetJWT()
        {
            return ProjectInterface.LocalDataSettings.GetValue(Dav.jwtKey);
        }

        public static string GetAvatarEtag()
        {
            return ProjectInterface.LocalDataSettings.GetValue(Dav.avatarEtagKey);
        }

        public static FileInfo GetAvatar()
        {
            string avatarPath = Path.Combine(Dav.DataPath, "avatar.png");
            return File.Exists(avatarPath) ? new FileInfo(avatarPath) : null;
        }

        private void SetUserInformation()
        {
            SetEmail(Email);
            SetUsername(Username);
            SetTotalStorage(TotalStorage);
            SetUsedStorage(UsedStorage);
            SetPlan(Plan);
            SetAvatarEtag(AvatarEtag);
            SetJWT(JWT);
        }

        private void SetEmail(string email)
        {
            ProjectInterface.LocalDataSettings.SetValue(Dav.emailKey, email);
        }

        private void SetUsername(string username)
        {
            ProjectInterface.LocalDataSettings.SetValue(Dav.usernameKey, username);
        }

        private void SetTotalStorage(long totalStorage)
        {
            ProjectInterface.LocalDataSettings.SetValue(Dav.totalStorageKey, totalStorage.ToString());
        }

        private void SetUsedStorage(long usedStorage)
        {
            ProjectInterface.LocalDataSettings.SetValue(Dav.usedStorageKey, usedStorage.ToString());
        }

        private void SetPlan(DavPlan plan)
        {
            ProjectInterface.LocalDataSettings.SetValue(Dav.planKey, ParseDavPlanToInt(plan).ToString());
        }

        private void SetAvatarEtag(string avatarEtag)
        {
            ProjectInterface.LocalDataSettings.SetValue(Dav.avatarEtagKey, avatarEtag);
        }

        public static void SetJWT(string jwt)
        {
            ProjectInterface.LocalDataSettings.SetValue(Dav.jwtKey, jwt);
        }

        #region Static methods
        public static DavPlan ParseIntToDavPlan(int planValue)
        {
            switch (planValue)
            {
                case 1:
                    return DavPlan.Plus;
                default:
                    return DavPlan.Free;
            }
        }

        public static int ParseDavPlanToInt(DavPlan plan)
        {
            switch (plan)
            {
                case DavPlan.Free:
                    return 0;
                case DavPlan.Plus:
                    return 1;
                default:
                    return 0;
            }
        }
        #endregion
    }

    public class DavUserData
    {
        public string email { get; set; }
        public string username { get; set; }
        public long total_storage { get; set; }
        public long used_storage { get; set; }
        public int plan { get; set; }
        public string avatar { get; set; }
        public string avatar_etag { get; set; }
    }
}
