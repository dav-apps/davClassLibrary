using SQLite;

namespace davClassLibrary.Models
{
    public class Property
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TableObjectId { get; set; }
        private string _name;
        public string Name
        {
            get => _name;
            set
            {
                if (_name == value)
                    return;

                _name = value;
                Save();
            }
        }
        private string _value;
        public string Value
        {
            get => _value;
            set
            {
                if (_value == value)
                    return;

                _value = value;
                Save();
            }
        }

        public Property(){}

        public Property(string name, string value)
        {
            _name = name;
            _value = value;

            Save();
        }

        public Property(int tableObjectId, string name, string value)
        {
            TableObjectId = tableObjectId;
            _name = name;
            _value = value;

            Save();
        }

        private void Save()
        {
            // Check if the tableObject already exists
            if (Dav.Database.GetProperty(Id) == null)
                Id = Dav.Database.CreateProperty(this);
            else
                Dav.Database.UpdateProperty(this);
        }
    }

    public class PropertyData
    {
        public int id { get; set; }
        public int table_object_id { get; set; }
        public string name { get; set; }
        public string value { get; set; }
    }
}
