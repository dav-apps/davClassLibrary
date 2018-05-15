using SQLite;
using System;

namespace davClassLibrary.Models
{
    public class TableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TableId { get; set; }
        public int UserId { get; set; }
        public TableObjectVisibility Visibility { get; set; }
        public Guid Uuid { get; set; }
        public bool File { get; set; }

        public enum TableObjectVisibility
        {
            Public,
            Protected,
            Private
        }
    }
}
