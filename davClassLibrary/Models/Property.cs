using SQLite;

namespace davClassLibrary.Models
{
    public class Property
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TableObjectId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public Property(){}

        public Property(int tableObjectId, string name, string value)
        {
            TableObjectId = tableObjectId;
            Name = name;
            Value = value;

            Save();
        }

        public void SetValue(string value)
        {
            Value = value;
            Save();
        }

        private void Save()
        {
            // Check if the tableObject already exists
            if(TableObjectId != 0)
            {
                if (!Dav.Database.PropertyExists(Id))
                    Id = Dav.Database.CreateProperty(this);
                else
                    Dav.Database.UpdateProperty(this);
            }
        }

        public PropertyData ToPropertyData()
        {
            return new PropertyData
            {
                id = Id,
                table_object_id = TableObjectId,
                name = Name,
                value = Value
            };
        }

        public static Property ConvertPropertyDataToProperty(PropertyData propertyData)
        {
            Property property = new Property
            {
                Id = propertyData.id,
                TableObjectId = propertyData.table_object_id,
                Name = propertyData.name,
                Value = propertyData.value
            };

            return property;
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
