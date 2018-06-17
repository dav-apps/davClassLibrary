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

        #region GetTableObject
        [Test]
        public void GetTableObjectShouldReturnTheTableObject()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            int tableId = 4;
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId);

            // Act
            var tableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(uuid);

            // Assert
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
            Assert.AreEqual(tableObject.TableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(tableObject.Uuid, tableObjectFromDatabase.Uuid);
            Assert.AreEqual(tableObject.UploadStatus, tableObjectFromDatabase.UploadStatus);
        }

        [Test]
        public void GetTableObjectShouldReturnNullWhenTheTableObjectDoesNotExist()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();

            // Act
            var tableObject = davClassLibrary.Dav.Database.GetTableObject(uuid);

            // Assert
            Assert.IsNull(tableObject);
        }
        #endregion

        #region GetPropertiesOfTableObject
        [Test]
        public void GetPropertiesOfTableObjectShouldReturnAllPropertiesOfTheTableObject()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            int tableId = 3;
            string firstPropertyName = "page1";
            string secondPropertyName = "page2";
            string firstPropertyValue = "Hello World";
            string secondPropertyValue = "Hallo Welt";
            List<Property> properties = new List<Property>
            {
                new Property{ Name = firstPropertyName, Value = firstPropertyValue },
                new Property{ Name = secondPropertyName, Value = secondPropertyValue }
            };
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, properties);

            // Act
            var propertiesList = davClassLibrary.Dav.Database.GetPropertiesOfTableObject(tableObject.Id);

            // Assert
            Assert.AreEqual(firstPropertyName, propertiesList[0].Name);
            Assert.AreEqual(firstPropertyValue, propertiesList[0].Value);
            Assert.AreEqual(secondPropertyName, propertiesList[1].Name);
            Assert.AreEqual(secondPropertyValue, propertiesList[1].Value);
        }
        #endregion

        #region TableObjectExists
        [Test]
        public void TableObjectExistsShouldReturnTrueIfTheTableObjectExists()
        {
            // Arrange
            int tableId = 5;
            var tableObject = new davClassLibrary.Models.TableObject(tableId);

            // Act
            bool tableObjectExists = davClassLibrary.Dav.Database.TableObjectExists(tableObject.Uuid);

            // Assert
            Assert.IsTrue(tableObjectExists);
        }

        [Test]
        public void TableObjectExistsShouldReturnFalseIfTheTableObjectDoesNotExist()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();

            // Act
            bool tableObjectExists = davClassLibrary.Dav.Database.TableObjectExists(uuid);

            // Assert
            Assert.IsFalse(tableObjectExists);
        }
        #endregion

        #region UpdateTableObject
        [Test]
        public void UpdateTableObjectShouldUpdateTheTableObjectInTheDatabase()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            int tableId = 5;
            int oldVisibilityInt = 1;
            int newVisibilityInt = 2;
            TableObjectVisibility newVisibility = TableObjectVisibility.Public;
            var tableObjectData = new davClassLibrary.Models.TableObjectData
            {
                uuid = uuid,
                table_id = tableId,
                visibility = oldVisibilityInt
            };
            var tableObject = davClassLibrary.Models.TableObject.ConvertTableObjectDataToTableObject(tableObjectData);

            // Save the tableObject in the db and create a new table object with the same uuid but different values
            davClassLibrary.Dav.Database.CreateTableObject(tableObject);

            var newTableObjectData = new davClassLibrary.Models.TableObjectData
            {
                uuid = uuid,
                id = tableObject.Id,
                table_id = tableId,
                visibility = newVisibilityInt
            };
            var newTableObject = davClassLibrary.Models.TableObject.ConvertTableObjectDataToTableObject(newTableObjectData);

            // Act
            davClassLibrary.Dav.Database.UpdateTableObject(newTableObject);

            // Assert
            var tableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.AreEqual(tableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(newVisibility, tableObjectFromDatabase.Visibility);
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
        }

        [Test]
        public void UpdateTableObjectShouldNotThrowAnExceptionWhenTheTableObjectDoesNotExist()
        {
            // Arrange
            var tableObjectData = new TableObjectData
            {
                id = -3,
                table_id = -2,
                uuid = Guid.NewGuid()
            };
            var tableObject = TableObject.ConvertTableObjectDataToTableObject(tableObjectData);

            // Act
            davClassLibrary.Dav.Database.UpdateTableObject(tableObject);
        }
        #endregion

        #region DeleteTableObject(Guid uuid)
        [Test]
        public void DeleteTableObjectWithUuidShouldSetTheUploadStatusToDeleted()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            var tableObject = new TableObject(uuid, tableId);

            // Act
            davClassLibrary.Dav.Database.DeleteTableObject(uuid);

            // Assert
            var tableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.AreEqual(TableObjectUploadStatus.Deleted, tableObjectFromDatabase.UploadStatus);
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
        }

        [Test]
        public void DeleteTableObjectWithUuidShouldDeleteTheTableObjectAndItsPropertiesIfTheUploadStatusIsDeleted()
        {
            // Arrange
            int tableId = 6;
            Guid uuid = Guid.NewGuid();
            List<Property> propertiesList = new List<Property>
            {
                new Property{Name = "page1", Value = "Good day"},
                new Property{Name = "page2", Value = "Guten Tag"}
            };
            var tableObject = new TableObject(uuid, tableId, propertiesList);
            tableObject.SetUploadStatus(TableObjectUploadStatus.Deleted);

            int firstPropertyId = tableObject.Properties[0].Id;
            int secondPropertyId = tableObject.Properties[1].Id;

            // Act
            davClassLibrary.Dav.Database.DeleteTableObject(uuid);

            // Assert
            var tableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.IsNull(tableObjectFromDatabase);

            var firstPropertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(firstPropertyId);
            Assert.IsNull(firstPropertyFromDatabase);

            var secondPropertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(secondPropertyId);
            Assert.IsNull(secondPropertyFromDatabase);
        }
        #endregion

        #region DeleteTableObject(TableObject tableObject)
        [Test]
        public void DeleteTableObjectWithTableObjectShouldSetTheUploadStatusToDeleted()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            var tableObject = new TableObject(uuid, tableId);

            // Act
            davClassLibrary.Dav.Database.DeleteTableObject(tableObject);

            // Assert
            var tableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.AreEqual(TableObjectUploadStatus.Deleted, tableObjectFromDatabase.UploadStatus);
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
        }

        public void DeleteTableObjectWithTableObjectShouldDeleteTheTableObjectAndItsPropertiesIfTheUploadStatusIsDeleted()
        {
            // Arrange
            int tableId = 6;
            Guid uuid = Guid.NewGuid();
            List<Property> propertiesList = new List<Property>
            {
                new Property{Name = "page1", Value = "Good day"},
                new Property{Name = "page2", Value = "Guten Tag"}
            };
            var tableObject = new TableObject(uuid, tableId, propertiesList);
            tableObject.SetUploadStatus(TableObjectUploadStatus.Deleted);

            int firstPropertyId = tableObject.Properties[0].Id;
            int secondPropertyId = tableObject.Properties[1].Id;

            // Act
            davClassLibrary.Dav.Database.DeleteTableObject(tableObject);

            // Assert
            var tableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.IsNull(tableObjectFromDatabase);

            var firstPropertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(firstPropertyId);
            Assert.IsNull(firstPropertyFromDatabase);

            var secondPropertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(secondPropertyId);
            Assert.IsNull(secondPropertyFromDatabase);
        }
        #endregion

        #region DeleteTableObjectImmediately(Guid uuid)
        [Test]
        public void DeleteTableObjectImmediatelyWithUuidShouldDeleteTheTableObjectAndItsPropertiesImmediately()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            var properties = new List<Property>
            {
                new Property{ Name = "test", Value = "test" },
                new Property {Name = "bla", Value = "bla" }
            };
            var tableObject = new TableObject(uuid, tableId, properties);

            int firstPropertyId = tableObject.Properties[0].Id;
            int secondPropertyId = tableObject.Properties[1].Id;

            // Act
            davClassLibrary.Dav.Database.DeleteTableObjectImmediately(uuid);

            // Assert
            var tableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.IsNull(tableObjectFromDatabase);

            var firstPropertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(firstPropertyId);
            Assert.IsNull(firstPropertyFromDatabase);

            var secondPropertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(secondPropertyId);
            Assert.IsNull(secondPropertyFromDatabase);
        }
        #endregion

        #region DeleteTableObjectImmediately(TableObject tableObject)
        [Test]
        public void DeleteTableObjectImmediatelyWithTableObjectShouldDeleteTheTableObjectAndItsPropertiesImmediately()
        {
            // Arrange
            int tableId = 6;
            Guid uuid = Guid.NewGuid();
            List<Property> propertiesList = new List<Property>
            {
                new Property{Name = "page1", Value = "Good day"},
                new Property{Name = "page2", Value = "Guten Tag"}
            };
            var tableObject = new TableObject(uuid, tableId, propertiesList);

            int firstPropertyId = tableObject.Properties[0].Id;
            int secondPropertyId = tableObject.Properties[1].Id;

            // Act
            davClassLibrary.Dav.Database.DeleteTableObjectImmediately(tableObject);

            // Assert
            var tableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.IsNull(tableObjectFromDatabase);

            var firstPropertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(firstPropertyId);
            Assert.IsNull(firstPropertyFromDatabase);

            var secondPropertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(secondPropertyId);
            Assert.IsNull(secondPropertyFromDatabase);
        }
        #endregion

        #region CreateProperty
        [Test]
        public void CreatePropertyShouldSaveThePropertyInTheDatabaseAndReturnThePropertyId()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var property = new Property { Name = "page1", Value = "Test", TableObjectId = -1 };

            // Act
            int id = davClassLibrary.Dav.Database.CreateProperty(property);

            // Assert
            var propertyFromDatabase = database.Get<Property>(id);
            Assert.AreEqual(property.TableObjectId, propertyFromDatabase.TableObjectId);
            Assert.AreEqual(property.Name, propertyFromDatabase.Name);
            Assert.AreEqual(property.Value, propertyFromDatabase.Value);
        }
        #endregion

        #region GetProperty
        [Test]
        public void GetPropertyShouldReturnThePropertyFromTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var property = new Property { Name = "page1", Value = "Test", TableObjectId = -1 };
            property.Id = davClassLibrary.Dav.Database.CreateProperty(property);

            // Act
            var propertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(property.Id);

            // Assert
            Assert.AreEqual(property.Id, propertyFromDatabase.Id);
            Assert.AreEqual(property.TableObjectId, propertyFromDatabase.TableObjectId);
            Assert.AreEqual(property.Name, propertyFromDatabase.Name);
            Assert.AreEqual(property.Value, propertyFromDatabase.Value);
        }

        [Test]
        public void GetPropertyShouldReturnNullIfThePropertyDoesNotExist()
        {
            // Arrange
            int propertyId = -13;

            // Act
            var property = davClassLibrary.Dav.Database.GetProperty(propertyId);

            // Assert
            Assert.IsNull(property);
        }
        #endregion

        #region PropertyExists
        [Test]
        public void PropertyExistsShouldReturnTrueIfThePropertyExists()
        {
            // Arrange
            var property = new Property { Name = "page1", Value = "Guten Tag", TableObjectId = -2 };
            property.Id = davClassLibrary.Dav.Database.CreateProperty(property);

            // Act
            bool propertyExists = davClassLibrary.Dav.Database.PropertyExists(property.Id);

            // Assert
            Assert.IsTrue(propertyExists);
        }

        [Test]
        public void PropertyExistsShouldReturnFalseIfThePropertyDoesNotExist()
        {
            // Arrange
            int propertyId = -13;

            // Act
            bool propertyExists = davClassLibrary.Dav.Database.PropertyExists(propertyId);

            // Assert
            Assert.IsFalse(propertyExists);
        }
        #endregion

        #region UpdateProperty
        [Test]
        public void UpdatePropertyShouldUpdateThePropertyInTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            string oldPropertyName = "oldCity";
            string newPropertyName = "newCity";
            string oldPropertyValue = "Petropavlovsk-Kamshatski";
            string newPropertyValue = "Dniepropetrovsk";
            int tableObjectId = -2;
            // Create a property, save it and then create a new property with the same id to update it
            var oldProperty = new Property { Name = oldPropertyName, Value = oldPropertyValue, TableObjectId = tableObjectId };
            oldProperty.Id = davClassLibrary.Dav.Database.CreateProperty(oldProperty);
            var newProperty = new Property { Id = oldProperty.Id, Name = newPropertyName, Value = newPropertyValue, TableObjectId = tableObjectId };

            // Act
            davClassLibrary.Dav.Database.UpdateProperty(newProperty);

            // Assert
            var propertyFromDatabase = database.Get<Property>(oldProperty.Id);
            Assert.AreEqual(newProperty.Id, propertyFromDatabase.Id);
            Assert.AreEqual(tableObjectId, propertyFromDatabase.TableObjectId);
            Assert.AreEqual(newPropertyName, propertyFromDatabase.Name);
            Assert.AreEqual(newPropertyValue, propertyFromDatabase.Value);
        }

        [Test]
        public void UpdatePropertyShouldNotThrowAnExceptionWhenThePropertyDoesNotExist()
        {
            // Arrange
            var property = new Property { Id = -2, Name = "bla", Value = "blabla", TableObjectId = -2 };

            // Act
            davClassLibrary.Dav.Database.UpdateProperty(property);
        }
        #endregion

        #region DeleteProperty(int id)
        [Test]
        public void DeletePropertyWithIdShouldDeleteThePropertyFromTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var property = new Property { Name = "bla", Value = "blabla", TableObjectId = -2 };
            property.Id = davClassLibrary.Dav.Database.CreateProperty(property);

            // Act
            davClassLibrary.Dav.Database.DeleteProperty(property.Id);

            // Assert
            var propertyFromDatabase = database.Query<Property>("SELECT * FROM Property WHERE Id = " + property.Id);
            Assert.AreEqual(0, propertyFromDatabase.Count);
        }

        [Test]
        public void DeletePropertyWithIdShouldNotThrowAnExceptionIfThePropertyDoesNotExist()
        {
            // Arrange
            int propertyId = -13;

            // Act
            davClassLibrary.Dav.Database.DeleteProperty(propertyId);
        }
        #endregion

        #region DeleteProperty(Property property)
        [Test]
        public void DeletePropertyWithPropertyShouldDeleteThePropertyFromTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var property = new Property { Name = "bla", Value = "blabla", TableObjectId = -2 };
            property.Id = davClassLibrary.Dav.Database.CreateProperty(property);

            // Act
            davClassLibrary.Dav.Database.DeleteProperty(property);

            // Assert
            var propertyFromDatabase = database.Query<Property>("SELECT * FROM Property WHERE Id = " + property.Id);
            Assert.AreEqual(0, propertyFromDatabase.Count);
        }

        [Test]
        public void DeletePropertyWithPropertyShouldNotThrowAnExceptionIfThePropertyDoesNotExist()
        {
            // Arrange
            var property = new Property { Name = "blabla", Value = "test", Id = -2, TableObjectId = -1 };

            // Act
            davClassLibrary.Dav.Database.DeleteProperty(property);
        }
        #endregion
    }
}
