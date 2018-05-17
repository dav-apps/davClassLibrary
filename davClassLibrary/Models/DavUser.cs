using davClassLibrary.Common;
using SQLite;
using System.Drawing;
using System.Net.NetworkInformation;

namespace davClassLibrary.Models
{
    public class DavUser
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public Bitmap Avatar { get; set; }
        public long TotalStorage { get; set; }
        public long UsedStorage { get; set; }
        public DavPlan Plan { get; set; }
        public bool IsLoggedIn { get; set; }

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
                IsLoggedIn = true;
                Email = ProjectInterface.LocalDataSettings.GetValue(Dav.emailKey);
                Username = ProjectInterface.LocalDataSettings.GetValue(Dav.usernameKey);

                long totalStorage = 0;
                long.TryParse(ProjectInterface.LocalDataSettings.GetValue(Dav.totalStorageKey), out totalStorage);
                TotalStorage = totalStorage;

                long usedStorage = 0;
                long.TryParse(ProjectInterface.LocalDataSettings.GetValue(Dav.usedStorageKey), out usedStorage);
                UsedStorage = usedStorage;
                
                Plan = ParseIntToDavPlan(int.Parse(ProjectInterface.LocalDataSettings.GetValue(Dav.planKey)));
            }
            else
            {
                IsLoggedIn = false;
            }


            if (NetworkInterface.GetIsNetworkAvailable() && IsLoggedIn)
            {
                // Get the user information from the server and update local informations

            }
        }

        public void Login(string email, string password)
        {

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
    }
}
