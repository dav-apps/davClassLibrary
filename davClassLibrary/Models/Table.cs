using System;
using System.Collections.Generic;
using System.Text;

namespace davClassLibrary.Models
{
    public class Table
    {
        public int Id { get; set; }
        public int AppId { get; set; }
        public string Name { get; set; }
    }

    public class TableData
    {
        public int id { get; set; }
        public int app_id { get; set; }
        public string name { get; set; }
        public List<TableObjectData> entries { get; set; }
    }
}
