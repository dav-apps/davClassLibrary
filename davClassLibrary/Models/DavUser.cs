using davClassLibrary.Common;
using davClassLibrary.DataAccess;
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
            if(!string.IsNullOrEmpty(ProjectInterface.LocalDataSettings.GetValue(Dav.jwtKey)))
            {
                // User is logged in. Get the user information
                IsLoggedIn = true;
                Avatar = GetAvatar();
            }
            else
                IsLoggedIn = false;
        }

        public async Task InitAsync()
        {
            if (await DownloadUserInformationAsync())
            {
                var x = DataManager.Sync();
            }
        }

        public async Task LoginAsync(string jwt)
        {
            JWT = jwt;
            IsLoggedIn = true;
            if(await DownloadUserInformationAsync())
            {
                var x = DataManager.Sync();
            }
            else
                Logout();
        }

        public void Logout()
        {
            string jwt = JWT;

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

            // Close the websocket connection
            DataManager.CloseWebsocketConnection();
            var x = DataManager.DeleteSessionOnServerAsync(jwt);
        }

        private async Task<bool> DownloadUserInformationAsync()
        {
            if (IsLoggedIn)
            {
                var getResult = await DataManager.HttpGetAsync(JWT, Dav.GetUserUrl);
                
                if(getResult.Success)
                {
                    // Deserialize the json and create a user object
                    var serializer = new DataContractJsonSerializer(typeof(DavUserData));
                    var ms = new MemoryStream(Encoding.UTF8.GetBytes(getResult.Data));
                    var dataReader = (DavUserData)serializer.ReadObject(ms);

                    Email = dataReader.email;
                    Username = dataReader.username;
                    TotalStorage = dataReader.total_storage;
                    UsedStorage = dataReader.used_storage;
                    Plan = (DavPlan)dataReader.plan;
                    string newAvatarEtag = dataReader.avatar_etag;

                    string avatarFileName = "avatar.png";
                    var avatarFilePath = Path.Combine(Dav.DataPath, avatarFileName);

                    if (!string.Equals(AvatarEtag, newAvatarEtag) || !File.Exists(avatarFilePath))
                    {
                        // Download the new avatar
                        DownloadAvatar(dataReader.avatar);
                    }

                    Avatar = new FileInfo(avatarFilePath);
                    AvatarEtag = newAvatarEtag;
                }
                else
                {
                    // Check if the session was deleted on the server
                    if(getResult.Status == 404 && getResult.Data.Contains("2814"))
                        Logout();
                }

                return getResult.Success;
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
            long.TryParse(ProjectInterface.LocalDataSettings.GetValue(Dav.totalStorageKey), out long totalStorage);
            return totalStorage;
        }

        public static long GetUsedStorage()
        {
            long.TryParse(ProjectInterface.LocalDataSettings.GetValue(Dav.usedStorageKey), out long usedStorage);
            return usedStorage;
        }

        public static DavPlan GetPlan()
        {
            var plan = ProjectInterface.LocalDataSettings.GetValue(Dav.planKey);
            if(plan != null)
            {
                int.TryParse(plan, out int planInt);
                return (DavPlan)planInt;
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
            ProjectInterface.LocalDataSettings.SetValue(Dav.planKey, ((int)plan).ToString());
        }

        private void SetAvatarEtag(string avatarEtag)
        {
            ProjectInterface.LocalDataSettings.SetValue(Dav.avatarEtagKey, avatarEtag);
        }

        public static void SetJWT(string jwt)
        {
            ProjectInterface.LocalDataSettings.SetValue(Dav.jwtKey, jwt);
        }
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
