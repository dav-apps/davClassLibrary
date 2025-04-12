using System;
using System.Collections.Generic;

namespace davClassLibrary.Models
{
    public class ListResponse<T>
    {
        public int total { get; set; }
        public List<T> items { get; set; }
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
        public string etag { get; set; }

        public TableObject ToTableObject()
        {
            TableObject tableObject = new TableObject
            {
                Uuid = uuid,
                Etag = etag
            };

            return tableObject;
        }
    }
}
