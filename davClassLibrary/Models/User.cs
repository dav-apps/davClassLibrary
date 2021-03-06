namespace davClassLibrary.Models
{
    public class User
    {
        public string Email { get; set; }
        public string FirstName { get; set; }
        public long TotalStorage { get; set; }
        public long UsedStorage { get; set; }
        public Plan Plan { get; set; }
        public string ProfileImageEtag { get; set; }
    }

    class UserData
    {
        public string email { get; set; }
        public string first_name { get; set; }
        public long total_storage { get; set; }
        public long used_storage { get; set; }
        public int plan { get; set; }
        public string profile_image_etag { get; set; }

        public User ToUser()
        {
            return new User
            {
                Email = email,
                FirstName = first_name,
                TotalStorage = total_storage,
                UsedStorage = used_storage,
                Plan = (Plan)plan,
                ProfileImageEtag = profile_image_etag
            };
        }
    }
}
