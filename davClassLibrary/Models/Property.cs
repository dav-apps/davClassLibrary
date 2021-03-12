using SQLite;
using System.Threading.Tasks;

namespace davClassLibrary.Models
{
    public class Property
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        public int TableObjectId { get; set; }
        public string Name { get; set; }
        public string Value { get; set; }

        public Property() { }

        public Property(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public Property(int tableObjectId, string name, string value)
        {
            TableObjectId = tableObjectId;
            Name = name;
            Value = value;
        }

        public static async Task<Property> CreateAsync(int tableObjectId, string name, string value)
        {
            var property = new Property(tableObjectId, name, value);
            await property.SaveAsync();
            return property;
        }

        public async Task SetValueAsync(string value)
        {
            Value = value;
            await SaveAsync();
        }

        private async Task SaveAsync()
        {
            // Check if the tableObject already exists
            if(TableObjectId != 0)
            {
                if (!await Dav.Database.PropertyExistsAsync(Id))
                    Id = await Dav.Database.CreatePropertyAsync(this);
                else
                    await Dav.Database.UpdatePropertyAsync(this);
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
    }

    public class PropertyData
    {
        public int id { get; set; }
        public int table_object_id { get; set; }
        public string name { get; set; }
        public string value { get; set; }
    }
}
