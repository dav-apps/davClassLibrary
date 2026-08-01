using System;
using System.Collections.Generic;

namespace davClassLibrary.Models
{
    public class ListResponse<T>
    {
        public int total { get; set; }
        public List<T> items { get; set; }
    }

    public class UserResource
    {
        public int id { get; set; }
        public string email { get; set; }
        public string firstName { get; set; }
        public bool confirmed { get; set; }
        public long totalStorage { get; set; }
        public long usedStorage { get; set; }
        public string stripeCustomerId { get; set; }
        public string plan { get; set; }
        public string subscriptionStatus { get; set; }
        public string periodEnd { get; set; }
        public UserProfileImageResource profileImage { get; set; }
    }

    public class TableResource
    {
        public int id { get; set; }
        public string name { get; set; }
        public string etag { get; set; }
        public ListResponse<TableObjectResource> tableObjects { get; set; }
    }

    public class TableObjectResource
    {
        public Guid uuid { get; set; }
        public UserResource user { get; set; }
        public TableResource table { get; set; }
        public string etag { get; set; }
        public string fileUrl { get; set; }
        public Dictionary<string, object> properties { get; set; }

        public TableObject ToTableObject()
        {
            List<Property> props = new List<Property>();

            if (properties != null)
                foreach (var kvp in properties)
                    props.Add(new Property(kvp.Key, kvp.Value.ToString()));

            TableObject tableObject = new TableObject
            {
                Uuid = uuid,
                TableId = table.id,
                Etag = etag,
                IsFile = fileUrl != null,
                Properties = props
            };

            return tableObject;
        }
    }

    public class UserProfileImageResource
    {
        public string url { get; set; }
        public string etag { get; set; }
    }

    public class CheckoutSessionResource
    {
        public string url { get; set; }
    }
}
