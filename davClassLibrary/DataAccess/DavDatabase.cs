using SQLite;
using System;
using System.Diagnostics;

namespace davClassLibrary
{
    public class DavDatabase
    {
        readonly SQLiteAsyncConnection database;

        public DavDatabase(string dbPath)
        {
            database = new SQLiteAsyncConnection(dbPath);
            database.CreateTableAsync<TableObject>().Wait();
        }
    }
}
