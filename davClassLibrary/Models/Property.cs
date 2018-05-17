using SQLite;
using System;
using System.Collections.Generic;
using System.Text;

namespace davClassLibrary.Models
{
    public class Property
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TableObjectId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }
    }
}
