using davClassLibrary.Models;
using SQLite;

namespace davClassLibrary
{
    public class DavDatabase
    {
        readonly SQLiteAsyncConnection database;
        private string databaseName = "dav.db";

        public DavDatabase()
        {
            database = new SQLiteAsyncConnection(Dav.DataPath + databaseName);
            database.CreateTableAsync<TableObject>().Wait();
            database.CreateTableAsync<Property>().Wait();
        }
    }
}
