using davClassLibrary.Common;
using davClassLibrary.Models;
using davClassLibrary.Tests.Common;
using NUnit.Framework;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using static davClassLibrary.Models.TableObject;

namespace davClassLibrary.Tests.DataAccess
{
    [TestFixture]
    public class DavDatabase
    {
        private const string databaseName = "dav.db";
        private string databasePath;

        #region Setup
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.RetrieveConstants = new RetrieveConstants();
            ProjectInterface.TriggerAction = new TriggerAction();
            databasePath = Path.Combine(Dav.GetDavDataPath(), databaseName);
        }
        #endregion

        #region Constructor
        [Test]
        public void DavDatabaseConstructorShouldCreateTheDatabase()
        {
            // Arrange
            // Delete the database file
            string databaseFilePath = Path.Combine(Dav.GetDavDataPath(), "dav.db");

            // Act
            new davClassLibrary.DataAccess.DavDatabase();

            // Assert
            Assert.IsTrue(File.Exists(databaseFilePath));
        }
        #endregion

        #region CreateTableObject
        [Test]
        public void CreateTableObjectShouldSaveTheTableObjectInTheDatabaseAndReturnTheId()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            int visibilityInt = 1;
            TableObjectVisibility visibility = TableObjectVisibility.Protected;

            var tableObjectData = new davClassLibrary.Models.TableObjectData
            {
                table_id = tableId,
                uuid = uuid,
                file = false,
                visibility = visibilityInt
            };
            var tableObject = davClassLibrary.Models.TableObject.ConvertTableObjectDataToTableObject(tableObjectData);

            // Act
            davClassLibrary.Dav.Database.CreateTableObject(tableObject);

            // Assert
            var tableObjectFromDatabase = database.Get<TableObject>(tableObject.Id);
            Assert.AreEqual(tableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
            Assert.AreEqual(uuid, tableObjectFromDatabase.Uuid);
            Assert.IsFalse(tableObjectFromDatabase.IsFile);
            Assert.AreEqual(visibility, tableObjectFromDatabase.Visibility);
        }
        #endregion

        #region CreateTableObjectWithProperties
        [Test]
        public void CreateTableObjectWithPropertiesShouldSaveTheTableObjectAndItsPropertiesInTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            string firstPropertyName = "page1";
            string secondPropertyName = "page2";
            string firstPropertyValue = "Hello World";
            string secondPropertyValue = "Hallo Welt";
            Dictionary<string, string> propertiesDictionary = new Dictionary<string, string>();
            propertiesDictionary.Add(firstPropertyName, firstPropertyValue);
            propertiesDictionary.Add(secondPropertyName, secondPropertyValue);

            var tableObjectData = new davClassLibrary.Models.TableObjectData
            {
                table_id = tableId,
                uuid = uuid,
                file = false,
                properties = propertiesDictionary
            };
            var tableObject = davClassLibrary.Models.TableObject.ConvertTableObjectDataToTableObject(tableObjectData);

            // Act
            davClassLibrary.Dav.Database.CreateTableObjectWithProperties(tableObject);

            // Assert
            var tableObjectFromDatabase = database.Get<TableObject>(tableObject.Id);
            Assert.AreEqual(tableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
            Assert.AreEqual(uuid, tableObjectFromDatabase.Uuid);
            Assert.IsFalse(tableObjectFromDatabase.IsFile);

            var firstPropertyFromDatabase = database.Get<Property>(tableObject.Properties[0].Id);
            Assert.AreEqual(tableObjectFromDatabase.Id, firstPropertyFromDatabase.TableObjectId);
            Assert.AreEqual(firstPropertyName, firstPropertyFromDatabase.Name);
            Assert.AreEqual(firstPropertyValue, firstPropertyValue);

            var secondPropertyFromDatabase = database.Get<Property>(tableObject.Properties[1].Id);
            Assert.AreEqual(tableObjectFromDatabase.Id, secondPropertyFromDatabase.TableObjectId);
            Assert.AreEqual(secondPropertyName, secondPropertyFromDatabase.Name);
            Assert.AreEqual(secondPropertyValue, secondPropertyFromDatabase.Value);
        }
        #endregion

        #region GetAllTableObjects(bool deleted)
        [Test]
        public void GetAllTableObjectsShouldReturnAllTableObjects()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);

            // Act
            var allTableObjects = davClassLibrary.Dav.Database.GetAllTableObjects(true);

            // Assert
            var allTableObjectsFromDatabase = database.Query<TableObject>("SELECT * FROM TableObject;");
            Assert.AreEqual(allTableObjectsFromDatabase.Count, allTableObjects.Count);
        }

        [Test]
        public void GetAllTableObjectsShouldReturnAllTableObjectsExceptDeletedOnes()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);

            // Act
            var allTableObjects = davClassLibrary.Dav.Database.GetAllTableObjects(false);

            // Assert
            int tableObjectsCount = 0;
            var allTableObjectsFromDatabase = database.Query<TableObject>("SELECT * FROM TableObject;");
            foreach(var obj in allTableObjectsFromDatabase)
            {
                if (obj.UploadStatus != TableObjectUploadStatus.Deleted)
                    tableObjectsCount++;
            }

            Assert.AreEqual(tableObjectsCount, allTableObjects.Count);
        }
        #endregion

        #region GetAllTableObjects(int tableId, bool deleted)
        [Test]
        public void GetAllTableObjectsWithTableIdShouldReturnAllTableObjectsOfTheTable()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableId = 4;

            // Act
            var allTableObjects = davClassLibrary.Dav.Database.GetAllTableObjects(4, true);

            // Assert
            var allTableObjectsFromDatabase = database.Query<TableObject>("SELECT * FROM TableObject WHERE TableId = " + tableId);
            Assert.AreEqual(allTableObjectsFromDatabase.Count, allTableObjects.Count);
        }

        [Test]
        public void GetAllTableObjectsWithTableIdShouldReturnAlltableObjectsOfTheTableExceptDeletedOnes()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableId = 4;

            // Act
            var allTableObjects = davClassLibrary.Dav.Database.GetAllTableObjects(4, false);

            // Assert
            int tableObjectsCount = 0;
            var allTableObjectsFromDatabase = database.Query<TableObject>("SELECT * FROM TableObject WHERE TableId = " + tableId);
            foreach(var obj in allTableObjectsFromDatabase)
            {
                if (obj.UploadStatus != TableObjectUploadStatus.Deleted)
                    tableObjectsCount++;
            }

            Assert.AreEqual(tableObjectsCount, allTableObjects.Count);
        }
        #endregion
    }
}
