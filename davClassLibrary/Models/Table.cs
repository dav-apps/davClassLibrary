using System.Collections.Generic;

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
        public int pages { get; set; }
        public List<TableObjectData> table_objects { get; set; }
    }
}
