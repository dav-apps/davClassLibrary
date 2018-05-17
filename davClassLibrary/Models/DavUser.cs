using davClassLibrary.Common;
using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.NetworkInformation;
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
        public Bitmap Avatar { get; set; }      // TODO Save new Avatar image on set
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
            if(ProjectInterface.LocalDataSettings.GetValue(Dav.jwtKey) != null)
            {
                // User is logged in. Get the user information
                GetUserInformation();
            }
            else
            {
                IsLoggedIn = false;
            }

            DownloadUserInformation();
        }

        public void Login(string email, string password)
        {

        }

        private async Task DownloadUserInformation()
        {
            if (NetworkInterface.GetIsNetworkAvailable() && IsLoggedIn)
            {
                // Get the user information from the server and update local informations
                HttpClient httpClient = new HttpClient();
                var headers = httpClient.DefaultRequestHeaders;
                httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(JWT);

                Uri requestUri = new Uri(Dav.ApiBaseUrl + Dav.GetUserUrl);

                HttpResponseMessage httpResponse = new HttpResponseMessage();
                string httpResponseBody = "";

                try
                {
                    //Send the GET request
                    httpResponse = await httpClient.GetAsync(requestUri);
                    httpResponseBody = await httpResponse.Content.ReadAsStringAsync();

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        // Deserialize the json and create a user object
                        var serializer = new DataContractJsonSerializer(typeof(DavUserData));
                        var ms = new MemoryStream(Encoding.UTF8.GetBytes(httpResponseBody));
                        var dataReader = (DavUserData)serializer.ReadObject(ms);

                        Email = dataReader.email;
                        Username = dataReader.username;
                        TotalStorage = dataReader.total_storage;
                        UsedStorage = dataReader.used_storage;
                        Plan = ParseIntToDavPlan(dataReader.plan);
                        string newAvatarEtag = dataReader.avatar_etag;

                        if(!String.Equals(AvatarEtag, newAvatarEtag))
                        {
                            // TODO Download the new avatar
                            DownloadAvatar(dataReader.avatar);
                        }

                        // Save new values in local settings
                        SetUserInformation();
                    }
                }
                catch (Exception e)
                {
                    httpResponseBody = "Error: " + e.HResult.ToString("X") + " Message: " + e.Message;
                    Debug.WriteLine(httpResponseBody);
                }
            }
        }

        private static DavPlan ParseIntToDavPlan(int planValue)
        {
            switch (planValue)
            {
                case 1:
                    return DavPlan.Plus;
                default:
                    return DavPlan.Free;
            }
        }

        private static int ParseDavPlanToInt(DavPlan plan)
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

        private void DownloadAvatar(string avatarUrl)
        {
            // TODO
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
        }

        private string GetEmail()
        {
            return ProjectInterface.LocalDataSettings.GetValue(Dav.emailKey);
        }

        private string GetUsername()
        {
            return ProjectInterface.LocalDataSettings.GetValue(Dav.usernameKey);
        }

        private long GetTotalStorage()
        {
            long totalStorage = 0;
            long.TryParse(ProjectInterface.LocalDataSettings.GetValue(Dav.totalStorageKey), out totalStorage);
            return totalStorage;
        }

        private long GetUsedStorage()
        {
            long usedStorage = 0;
            long.TryParse(ProjectInterface.LocalDataSettings.GetValue(Dav.usedStorageKey), out usedStorage);
            return usedStorage;
        }

        private DavPlan GetPlan()
        {
            return ParseIntToDavPlan(int.Parse(ProjectInterface.LocalDataSettings.GetValue(Dav.planKey)));
        }

        private string GetJWT()
        {
            return ProjectInterface.LocalDataSettings.GetValue(Dav.jwtKey);
        }

        private string GetAvatarEtag()
        {
            return ProjectInterface.LocalDataSettings.GetValue(Dav.avatarEtagKey);
        }

        private void SetUserInformation()
        {
            SetEmail(Email);
            SetUsername(Username);
            SetTotalStorage(TotalStorage);
            SetPlan(Plan);
            SetAvatarEtag(AvatarEtag);
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

        private void SetJWT(string jwt)
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
