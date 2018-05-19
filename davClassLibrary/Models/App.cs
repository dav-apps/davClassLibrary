using System;
using System.Collections.Generic;
using System.Text;

namespace davClassLibrary.Models
{
    public class App
    {
        public int Id { get; set; }
        public string Name { get; set; }
    }

    public class AppData
    {
        public int id { get; set; }
        public string name { get; set; }
        public List<TableData> tables { get; set; }
    }
}
