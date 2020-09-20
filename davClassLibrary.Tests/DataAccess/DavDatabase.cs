using davClassLibrary.Common;
using davClassLibrary.Models;
using davClassLibrary.Tests.Common;
using NUnit.Framework;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace davClassLibrary.Tests.DataAccess
{
    [TestFixture][SingleThreaded]
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
            ProjectInterface.GeneralMethods = new GeneralMethods();
            databasePath = Path.Combine(Dav.GetDavDataPath(), databaseName);
        }

        [SetUp]
        public async Task Setup()
        {
            // Delete all files and folders in the test folder except the database file
            var davFolder = new DirectoryInfo(Dav.GetDavDataPath());
            foreach(var folder in davFolder.GetDirectories())
                folder.Delete(true);
            
            // Clear the database
            var database = new davClassLibrary.DataAccess.DavDatabase();
            await database.DropAsync();
        }
        #endregion

        #region CreateTableObject
        [Test]
        public async Task CreateTableObjectShouldSaveTheTableObjectInTheDatabaseAndReturnTheId()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableId = 4;
            Guid uuid = Guid.NewGuid();

            var tableObjectData = new TableObjectData
            {
                table_id = tableId,
                uuid = uuid,
                file = false
            };
            var tableObject = TableObject.ConvertTableObjectDataToTableObject(tableObjectData);

            // Act
            await davClassLibrary.Dav.Database.CreateTableObjectAsync(tableObject);

            // Assert
            var tableObjectFromDatabase = database.Get<TableObject>(tableObject.Id);
            Assert.AreEqual(tableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
            Assert.AreEqual(uuid, tableObjectFromDatabase.Uuid);
            Assert.IsFalse(tableObjectFromDatabase.IsFile);
        }
        #endregion

        #region CreateTableObjectWithProperties
        [Test]
        public async Task CreateTableObjectWithPropertiesShouldSaveTheTableObjectAndItsPropertiesInTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            string firstPropertyName = "page1";
            string secondPropertyName = "page2";
            string firstPropertyValue = "Hello World";
            string secondPropertyValue = "Hallo Welt";
            Dictionary<string, string> propertiesDictionary = new Dictionary<string, string>
            {
                { firstPropertyName, firstPropertyValue },
                { secondPropertyName, secondPropertyValue }
            };

            var tableObjectData = new TableObjectData
            {
                table_id = tableId,
                uuid = uuid,
                file = false,
                properties = propertiesDictionary
            };
            var tableObject = TableObject.ConvertTableObjectDataToTableObject(tableObjectData);

            // Act
            await davClassLibrary.Dav.Database.CreateTableObjectWithPropertiesAsync(tableObject);

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
        public async Task GetAllTableObjectsShouldReturnAllTableObjects()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var tableObjects = new List<TableObject>
            {
                await TableObject.CreateAsync(Guid.NewGuid(), 13),
                await TableObject.CreateAsync(Guid.NewGuid(), 13),
                await TableObject.CreateAsync(Guid.NewGuid(), 12)
            };
            await tableObjects[0].SetUploadStatusAsync(TableObject.TableObjectUploadStatus.Deleted);

            // Act
            var allTableObjects = await davClassLibrary.Dav.Database.GetAllTableObjectsAsync(true);

            // Assert
            Assert.AreEqual(tableObjects.Count, allTableObjects.Count);
        }

        [Test]
        public async Task GetAllTableObjectsShouldReturnAllTableObjectsExceptDeletedOnes()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var tableObjects = new List<TableObject>
            {
                await TableObject.CreateAsync(Guid.NewGuid(), 13),
                await TableObject.CreateAsync(Guid.NewGuid(), 13),
                await TableObject.CreateAsync(Guid.NewGuid(), 12)
            };
            await tableObjects[0].SetUploadStatusAsync(TableObject.TableObjectUploadStatus.Deleted);

            // Act
            var allTableObjects = await davClassLibrary.Dav.Database.GetAllTableObjectsAsync(false);

            // Assert
            Assert.AreEqual(tableObjects.Count - 1, allTableObjects.Count);
        }
        #endregion

        #region GetAllTableObjects(int tableId, bool deleted)
        [Test]
        public async Task GetAllTableObjectsWithTableIdShouldReturnAllTableObjectsOfTheTable()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableId = 4;
            var tableObjects = new List<TableObject>
            {
                await TableObject.CreateAsync(Guid.NewGuid(), tableId),
                await TableObject.CreateAsync(Guid.NewGuid(), tableId),
                await TableObject.CreateAsync(Guid.NewGuid(), tableId),
                await TableObject.CreateAsync(Guid.NewGuid(), 3)
            };
            await tableObjects[0].SetUploadStatusAsync(TableObject.TableObjectUploadStatus.Deleted);

            // Act
            var allTableObjects = await davClassLibrary.Dav.Database.GetAllTableObjectsAsync(tableId, true);

            // Assert
            Assert.AreEqual(tableObjects.Count - 1, allTableObjects.Count);
        }

        [Test]
        public async Task GetAllTableObjectsWithTableIdShouldReturnAlltableObjectsOfTheTableExceptDeletedOnes()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableId = 4;
            var tableObjects = new List<TableObject>
            {
                await TableObject.CreateAsync(Guid.NewGuid(), tableId),
                await TableObject.CreateAsync(Guid.NewGuid(), tableId),
                await TableObject.CreateAsync(Guid.NewGuid(), tableId),
                await TableObject.CreateAsync(Guid.NewGuid(), 3),
            };
            await tableObjects[0].SetUploadStatusAsync(TableObject.TableObjectUploadStatus.Deleted);

            // Act
            var allTableObjects = await davClassLibrary.Dav.Database.GetAllTableObjectsAsync(tableId, false);

            // Assert
            Assert.AreEqual(tableObjects.Count - 2, allTableObjects.Count);
        }
        #endregion

        #region GetTableObject
        [Test]
        public async Task GetTableObjectShouldReturnTheTableObject()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            int tableId = 4;
            var tableObject = await TableObject.CreateAsync(uuid, tableId);

            // Act
            var tableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);

            // Assert
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
            Assert.AreEqual(tableObject.TableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(tableObject.Uuid, tableObjectFromDatabase.Uuid);
            Assert.AreEqual(tableObject.UploadStatus, tableObjectFromDatabase.UploadStatus);
        }

        [Test]
        public async Task GetTableObjectShouldReturnNullWhenTheTableObjectDoesNotExist()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();

            // Act
            var tableObject = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);

            // Assert
            Assert.IsNull(tableObject);
        }
        #endregion

        #region GetPropertiesOfTableObject
        [Test]
        public async Task GetPropertiesOfTableObjectShouldReturnAllPropertiesOfTheTableObject()
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
            var tableObject = await TableObject.CreateAsync(uuid, tableId, properties);

            // Act
            var propertiesList = await davClassLibrary.Dav.Database.GetPropertiesOfTableObjectAsync(tableObject.Id);

            // Assert
            Assert.AreEqual(firstPropertyName, propertiesList[0].Name);
            Assert.AreEqual(firstPropertyValue, propertiesList[0].Value);
            Assert.AreEqual(secondPropertyName, propertiesList[1].Name);
            Assert.AreEqual(secondPropertyValue, propertiesList[1].Value);
        }
        #endregion

        #region TableObjectExists
        [Test]
        public async Task TableObjectExistsShouldReturnTrueIfTheTableObjectExists()
        {
            // Arrange
            int tableId = 5;
            var tableObject = await TableObject.CreateAsync(tableId);

            // Act
            bool tableObjectExists = await davClassLibrary.Dav.Database.TableObjectExistsAsync(tableObject.Uuid);

            // Assert
            Assert.IsTrue(tableObjectExists);
        }

        [Test]
        public async Task TableObjectExistsShouldReturnFalseIfTheTableObjectDoesNotExist()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();

            // Act
            bool tableObjectExists = await davClassLibrary.Dav.Database.TableObjectExistsAsync(uuid);

            // Assert
            Assert.IsFalse(tableObjectExists);
        }
        #endregion

        #region UpdateTableObject
        [Test]
        public async Task UpdateTableObjectShouldUpdateTheTableObjectInTheDatabase()
        {
            // Arrange
            Guid uuid = Guid.NewGuid();
            int tableId = 5;
            var tableObjectData = new TableObjectData
            {
                uuid = uuid,
                table_id = tableId
            };
            var tableObject = TableObject.ConvertTableObjectDataToTableObject(tableObjectData);

            // Save the tableObject in the db and create a new table object with the same uuid but different values
            await davClassLibrary.Dav.Database.CreateTableObjectAsync(tableObject);

            var newTableObjectData = new TableObjectData
            {
                uuid = uuid,
                id = tableObject.Id,
                table_id = tableId
            };
            var newTableObject = TableObject.ConvertTableObjectDataToTableObject(newTableObjectData);

            // Act
            await davClassLibrary.Dav.Database.UpdateTableObjectAsync(newTableObject);

            // Assert
            var tableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(tableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
        }

        [Test]
        public async Task UpdateTableObjectShouldNotThrowAnExceptionWhenTheTableObjectDoesNotExist()
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
            await davClassLibrary.Dav.Database.UpdateTableObjectAsync(tableObject);
        }
        #endregion

        #region DeleteTableObject(Guid uuid)
        [Test]
        public async Task DeleteTableObjectWithUuidShouldSetTheUploadStatusToDeleted()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            var tableObject = await TableObject.CreateAsync(uuid, tableId);

            // Act
            await davClassLibrary.Dav.Database.DeleteTableObjectAsync(uuid);

            // Assert
            var tableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(TableObject.TableObjectUploadStatus.Deleted, tableObjectFromDatabase.UploadStatus);
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
        }

        [Test]
        public async Task DeleteTableObjectWithUuidShouldDeleteTheTableObjectAndItsPropertiesIfTheUploadStatusIsDeleted()
        {
            // Arrange
            int tableId = 6;
            Guid uuid = Guid.NewGuid();
            List<Property> propertiesList = new List<Property>
            {
                new Property{Name = "page1", Value = "Good day"},
                new Property{Name = "page2", Value = "Guten Tag"}
            };
            var tableObject = await TableObject.CreateAsync(uuid, tableId, propertiesList);
            await tableObject.SetUploadStatusAsync(TableObject.TableObjectUploadStatus.Deleted);

            int firstPropertyId = tableObject.Properties[0].Id;
            int secondPropertyId = tableObject.Properties[1].Id;

            // Act
            await davClassLibrary.Dav.Database.DeleteTableObjectAsync(uuid);

            // Assert
            var tableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNull(tableObjectFromDatabase);

            var firstPropertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(firstPropertyId);
            Assert.IsNull(firstPropertyFromDatabase);

            var secondPropertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(secondPropertyId);
            Assert.IsNull(secondPropertyFromDatabase);
        }
        #endregion

        #region DeleteTableObject(TableObject tableObject)
        [Test]
        public async Task DeleteTableObjectWithTableObjectShouldSetTheUploadStatusToDeleted()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            var tableObject = await TableObject.CreateAsync(uuid, tableId);

            // Act
            await davClassLibrary.Dav.Database.DeleteTableObjectAsync(tableObject);

            // Assert
            var tableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);
            Assert.AreEqual(TableObject.TableObjectUploadStatus.Deleted, tableObjectFromDatabase.UploadStatus);
            Assert.AreEqual(tableObject.Id, tableObjectFromDatabase.Id);
        }

        [Test]
        public async Task DeleteTableObjectWithTableObjectShouldDeleteTheTableObjectAndItsPropertiesIfTheUploadStatusIsDeleted()
        {
            // Arrange
            int tableId = 6;
            Guid uuid = Guid.NewGuid();
            List<Property> propertiesList = new List<Property>
            {
                new Property{Name = "page1", Value = "Good day"},
                new Property{Name = "page2", Value = "Guten Tag"}
            };
            var tableObject = await TableObject.CreateAsync(uuid, tableId, propertiesList);
            await tableObject.SetUploadStatusAsync(TableObject.TableObjectUploadStatus.Deleted);

            int firstPropertyId = tableObject.Properties[0].Id;
            int secondPropertyId = tableObject.Properties[1].Id;

            // Act
            await davClassLibrary.Dav.Database.DeleteTableObjectAsync(tableObject);

            // Assert
            var tableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNull(tableObjectFromDatabase);

            var firstPropertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(firstPropertyId);
            Assert.IsNull(firstPropertyFromDatabase);

            var secondPropertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(secondPropertyId);
            Assert.IsNull(secondPropertyFromDatabase);
        }
        #endregion

        #region DeleteTableObjectImmediately(Guid uuid)
        [Test]
        public async Task DeleteTableObjectImmediatelyWithUuidShouldDeleteTheTableObjectAndItsPropertiesImmediately()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            var properties = new List<Property>
            {
                new Property{ Name = "test", Value = "test" },
                new Property {Name = "bla", Value = "bla" }
            };
            var tableObject = await TableObject.CreateAsync(uuid, tableId, properties);

            int firstPropertyId = tableObject.Properties[0].Id;
            int secondPropertyId = tableObject.Properties[1].Id;

            // Act
            await davClassLibrary.Dav.Database.DeleteTableObjectImmediatelyAsync(uuid);

            // Assert
            var tableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNull(tableObjectFromDatabase);

            var firstPropertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(firstPropertyId);
            Assert.IsNull(firstPropertyFromDatabase);

            var secondPropertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(secondPropertyId);
            Assert.IsNull(secondPropertyFromDatabase);
        }
        #endregion

        #region DeleteTableObjectImmediately(TableObject tableObject)
        [Test]
        public async Task DeleteTableObjectImmediatelyWithTableObjectShouldDeleteTheTableObjectAndItsPropertiesImmediately()
        {
            // Arrange
            int tableId = 6;
            Guid uuid = Guid.NewGuid();
            List<Property> propertiesList = new List<Property>
            {
                new Property{Name = "page1", Value = "Good day"},
                new Property{Name = "page2", Value = "Guten Tag"}
            };
            var tableObject = await TableObject.CreateAsync(uuid, tableId, propertiesList);

            int firstPropertyId = tableObject.Properties[0].Id;
            int secondPropertyId = tableObject.Properties[1].Id;

            // Act
            await davClassLibrary.Dav.Database.DeleteTableObjectImmediatelyAsync(tableObject);

            // Assert
            var tableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNull(tableObjectFromDatabase);

            var firstPropertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(firstPropertyId);
            Assert.IsNull(firstPropertyFromDatabase);

            var secondPropertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(secondPropertyId);
            Assert.IsNull(secondPropertyFromDatabase);
        }
        #endregion

        #region CreateProperty
        [Test]
        public async Task CreatePropertyShouldSaveThePropertyInTheDatabaseAndReturnThePropertyId()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var property = new Property { Name = "page1", Value = "Test", TableObjectId = -1 };

            // Act
            int id = await davClassLibrary.Dav.Database.CreatePropertyAsync(property);

            // Assert
            var propertyFromDatabase = database.Get<Property>(id);
            Assert.AreEqual(property.TableObjectId, propertyFromDatabase.TableObjectId);
            Assert.AreEqual(property.Name, propertyFromDatabase.Name);
            Assert.AreEqual(property.Value, propertyFromDatabase.Value);
        }
        #endregion

        #region CreateProperties
        [Test]
        public async Task CreatePropertiesShouldSaveThePropertiesInTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableObjectId = 23;
            string firstPropertyName = "page1";
            string firstPropertyValue = "Hello World";
            string secondPropertyName = "page2";
            string secondPropertyValue = "Hallo Welt";
            var properties = new List<Property>
            {
                new Property(tableObjectId, firstPropertyName, firstPropertyValue),
                new Property(tableObjectId, secondPropertyName, secondPropertyValue)
            };

            // Act
            await davClassLibrary.Dav.Database.CreatePropertiesAsync(properties);

            // Assert
            var firstPropertyFromDatabase = database.Get<Property>(properties[0].Id);
            Assert.AreEqual(tableObjectId, firstPropertyFromDatabase.TableObjectId);
            Assert.AreEqual(firstPropertyName, firstPropertyFromDatabase.Name);
            Assert.AreEqual(firstPropertyValue, firstPropertyFromDatabase.Value);

            var secondPropertyFromDatabase = database.Get<Property>(properties[1].Id);
            Assert.AreEqual(tableObjectId, secondPropertyFromDatabase.TableObjectId);
            Assert.AreEqual(secondPropertyName, secondPropertyFromDatabase.Name);
            Assert.AreEqual(secondPropertyValue, secondPropertyFromDatabase.Value);
        }
        #endregion

        #region GetProperty
        [Test]
        public async Task GetPropertyShouldReturnThePropertyFromTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var property = new Property { Name = "page1", Value = "Test", TableObjectId = -1 };
            property.Id = await davClassLibrary.Dav.Database.CreatePropertyAsync(property);

            // Act
            var propertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(property.Id);

            // Assert
            Assert.AreEqual(property.Id, propertyFromDatabase.Id);
            Assert.AreEqual(property.TableObjectId, propertyFromDatabase.TableObjectId);
            Assert.AreEqual(property.Name, propertyFromDatabase.Name);
            Assert.AreEqual(property.Value, propertyFromDatabase.Value);
        }

        [Test]
        public async Task GetPropertyShouldReturnNullIfThePropertyDoesNotExist()
        {
            // Arrange
            int propertyId = -13;

            // Act
            var property = await davClassLibrary.Dav.Database.GetPropertyAsync(propertyId);

            // Assert
            Assert.IsNull(property);
        }
        #endregion

        #region PropertyExists
        [Test]
        public async Task PropertyExistsShouldReturnTrueIfThePropertyExists()
        {
            // Arrange
            var property = new Property { Name = "page1", Value = "Guten Tag", TableObjectId = -2 };
            property.Id = await davClassLibrary.Dav.Database.CreatePropertyAsync(property);

            // Act
            bool propertyExists = await davClassLibrary.Dav.Database.PropertyExistsAsync(property.Id);

            // Assert
            Assert.IsTrue(propertyExists);
        }

        [Test]
        public async Task PropertyExistsShouldReturnFalseIfThePropertyDoesNotExist()
        {
            // Arrange
            int propertyId = -13;

            // Act
            bool propertyExists = await davClassLibrary.Dav.Database.PropertyExistsAsync(propertyId);

            // Assert
            Assert.IsFalse(propertyExists);
        }
        #endregion

        #region UpdateProperty
        [Test]
        public async Task UpdatePropertyShouldUpdateThePropertyInTheDatabase()
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
            oldProperty.Id = await davClassLibrary.Dav.Database.CreatePropertyAsync(oldProperty);
            var newProperty = new Property { Id = oldProperty.Id, Name = newPropertyName, Value = newPropertyValue, TableObjectId = tableObjectId };

            // Act
            await davClassLibrary.Dav.Database.UpdatePropertyAsync(newProperty);

            // Assert
            var propertyFromDatabase = database.Get<Property>(oldProperty.Id);
            Assert.AreEqual(newProperty.Id, propertyFromDatabase.Id);
            Assert.AreEqual(tableObjectId, propertyFromDatabase.TableObjectId);
            Assert.AreEqual(newPropertyName, propertyFromDatabase.Name);
            Assert.AreEqual(newPropertyValue, propertyFromDatabase.Value);
        }

        [Test]
        public async Task UpdatePropertyShouldNotThrowAnExceptionWhenThePropertyDoesNotExist()
        {
            // Arrange
            var property = new Property { Id = -2, Name = "bla", Value = "blabla", TableObjectId = -2 };

            // Act
            await davClassLibrary.Dav.Database.UpdatePropertyAsync(property);
        }
        #endregion

        #region UpdateProperties
        [Test]
        public async Task UpdatePropertiesShouldUpdateThePropertiesInTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableObjectId = 34;
            string oldFirstPropertyName = "page1";
            string newFirstPropertyName = "test1";
            string oldFirstPropertyValue = "Hello World";
            string newFirstPropertyValue = "Hello Test";
            string secondPropertyName = "test2";
            string oldSecondPropertyValue = "Hallo Welt";
            string newSecondPropertyValue = "Hallo Test";

            // Create the properties with the old values
            var properties = new List<Property>
            {
                await Property.CreateAsync(tableObjectId, oldFirstPropertyName, oldFirstPropertyValue),
                await Property.CreateAsync(tableObjectId, secondPropertyName, oldSecondPropertyValue)
            };

            // Change the values of the properties and call UpdateProperties
            properties[0].Name = newFirstPropertyName;
            properties[0].Value = newFirstPropertyValue;
            properties[1].Value = newSecondPropertyValue;

            // Act
            await davClassLibrary.Dav.Database.UpdatePropertiesAsync(properties);

            // Assert
            var firstPropertyFromDatabase = database.Get<Property>(properties[0].Id);
            Assert.AreEqual(tableObjectId, firstPropertyFromDatabase.TableObjectId);
            Assert.AreEqual(newFirstPropertyName, firstPropertyFromDatabase.Name);
            Assert.AreEqual(newFirstPropertyValue, firstPropertyFromDatabase.Value);

            var secondPropertyFromDatabase = database.Get<Property>(properties[1].Id);
            Assert.AreEqual(tableObjectId, secondPropertyFromDatabase.TableObjectId);
            Assert.AreEqual(secondPropertyName, secondPropertyFromDatabase.Name);
            Assert.AreEqual(newSecondPropertyValue, secondPropertyFromDatabase.Value);
        }
        #endregion

        #region DeleteProperty(int id)
        [Test]
        public async Task DeletePropertyWithIdShouldDeleteThePropertyFromTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var property = new Property { Name = "bla", Value = "blabla", TableObjectId = -2 };
            property.Id = await davClassLibrary.Dav.Database.CreatePropertyAsync(property);

            // Act
            await davClassLibrary.Dav.Database.DeletePropertyAsync(property.Id);

            // Assert
            var propertyFromDatabase = database.Query<Property>("SELECT * FROM Property WHERE Id = " + property.Id);
            Assert.AreEqual(0, propertyFromDatabase.Count);
        }

        [Test]
        public async Task DeletePropertyWithIdShouldNotThrowAnExceptionIfThePropertyDoesNotExist()
        {
            // Arrange
            int propertyId = -13;

            // Act
            await davClassLibrary.Dav.Database.DeletePropertyAsync(propertyId);
        }
        #endregion

        #region DeleteProperty(Property property)
        [Test]
        public async Task DeletePropertyWithPropertyShouldDeleteThePropertyFromTheDatabase()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var property = new Property { Name = "bla", Value = "blabla", TableObjectId = -2 };
            property.Id = await davClassLibrary.Dav.Database.CreatePropertyAsync(property);

            // Act
            await davClassLibrary.Dav.Database.DeletePropertyAsync(property);

            // Assert
            var propertyFromDatabase = database.Query<Property>("SELECT * FROM Property WHERE Id = " + property.Id);
            Assert.AreEqual(0, propertyFromDatabase.Count);
        }

        [Test]
        public async Task DeletePropertyWithPropertyShouldNotThrowAnExceptionIfThePropertyDoesNotExist()
        {
            // Arrange
            var property = new Property { Name = "blabla", Value = "test", Id = -2, TableObjectId = -1 };

            // Act
            await davClassLibrary.Dav.Database.DeletePropertyAsync(property);
        }
        #endregion

        #region HttpGet
        [Test]
        public async Task HttpGetShouldSendGetRequestToExistingResourceAndReturnTrue()
        {
            // Arrange
            var uuid = Dav.TestDataFirstTableObject.uuid;

            // Act
            var response = await davClassLibrary.DataAccess.DataManager.HttpGetAsync(Dav.Jwt, "apps/object/" + uuid);

            // Assert
            Assert.IsTrue(response.Success);
        }

        [Test]
        public async Task HttpGetShouldSendGetRequestToNotExistingResourceAndReturnFalse()
        {
            // Arrange
            var uuid = Guid.NewGuid();

            // Act
            var response = await davClassLibrary.DataAccess.DataManager.HttpGetAsync(Dav.Jwt, "apps/object/" + uuid);

            // Assert
            Assert.IsFalse(response.Success);
        }
        #endregion

        #region ExportData
        [Test]
        public async Task ExportDataShouldCreateJsonFileWithAllTableObjects()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, Dav.Jwt);
            await davClassLibrary.DataAccess.DataManager.Sync();

            // Copy image.jpg, which is the file of the uploaded table object
            var fileTableObject = (await davClassLibrary.Dav.Database.GetAllTableObjectsAsync(Dav.TableIds[1], false))[0];
            var tableFolder = Directory.CreateDirectory(Path.Combine(Dav.GetDavDataPath(), fileTableObject.TableId.ToString()));
            File.Copy(Path.Combine(Dav.ProjectDirectory, "Assets", "image.jpg"), Path.Combine(tableFolder.FullName, fileTableObject.Uuid.ToString()), true);

            var progress = new Progress<int>();
            var exportFolder = Directory.CreateDirectory(Path.Combine(Dav.GetDavDataPath(), "export"));

            // Act
            await davClassLibrary.DataAccess.DataManager.ExportDataAsync(exportFolder, progress);

            // Assert
            FileInfo dataFile = new FileInfo(Path.Combine(exportFolder.FullName, davClassLibrary.Dav.ExportDataFileName));
            FileAssert.Exists(dataFile);

            // Read the file and deserialize the content
            var firstTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(Dav.TestDataFirstTableObject.uuid);
            var secondTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(Dav.TestDataSecondTableObject.uuid);
            var data = davClassLibrary.DataAccess.DataManager.GetDataFromFile(dataFile);
            Assert.AreEqual(firstTableObjectFromDatabase.Id, data[0].id);
            Assert.AreEqual(firstTableObjectFromDatabase.TableId, data[0].table_id);
            Assert.AreEqual(firstTableObjectFromDatabase.Uuid, data[0].uuid);
            Assert.AreEqual(firstTableObjectFromDatabase.IsFile, data[0].file);
            Assert.AreEqual(firstTableObjectFromDatabase.GetPropertyValue(Dav.TestDataFirstPropertyName), data[0].properties[Dav.TestDataFirstPropertyName]);
            Assert.AreEqual(firstTableObjectFromDatabase.GetPropertyValue(Dav.TestDataSecondPropertyName), data[0].properties[Dav.TestDataSecondPropertyName]);
            Assert.AreEqual(firstTableObjectFromDatabase.Etag, data[0].etag);

            Assert.AreEqual(secondTableObjectFromDatabase.Id, data[1].id);
            Assert.AreEqual(secondTableObjectFromDatabase.TableId, data[1].table_id);
            Assert.AreEqual(secondTableObjectFromDatabase.Uuid, data[1].uuid);
            Assert.AreEqual(secondTableObjectFromDatabase.IsFile, data[1].file);
            Assert.AreEqual(secondTableObjectFromDatabase.GetPropertyValue(Dav.TestDataFirstPropertyName), data[1].properties[Dav.TestDataFirstPropertyName]);
            Assert.AreEqual(secondTableObjectFromDatabase.GetPropertyValue(Dav.TestDataSecondPropertyName), data[1].properties[Dav.TestDataSecondPropertyName]);
            Assert.AreEqual(secondTableObjectFromDatabase.Etag, data[1].etag);
        }

        [Test]
        public async Task ExportDataShouldCopyAllTableObjectFiles()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, null);
            var uuid = Guid.NewGuid();
            FileInfo file = new FileInfo(Path.Combine(Dav.ProjectDirectory, "Assets", "image.jpg"));
            var tableObject = await TableObject.CreateAsync(uuid, Dav.TestFileTableId, file);

            var progress = new Progress<int>();
            var exportFolder = Directory.CreateDirectory(Path.Combine(Dav.GetDavDataPath(), "export"));

            // Act
            await davClassLibrary.DataAccess.DataManager.ExportDataAsync(exportFolder, progress);

            // Assert
            FileInfo dataFile = new FileInfo(Path.Combine(exportFolder.FullName, davClassLibrary.Dav.ExportDataFileName));
            var data = davClassLibrary.DataAccess.DataManager.GetDataFromFile(dataFile);
            FileAssert.Exists(Path.Combine(exportFolder.FullName, Dav.TestFileTableId.ToString(), uuid.ToString()));
            FileAssert.Exists(dataFile);

            Assert.AreEqual(tableObject.Id, data[0].id);
            Assert.AreEqual(tableObject.TableId, data[0].table_id);
            Assert.AreEqual(tableObject.Uuid, data[0].uuid);
            Assert.AreEqual(tableObject.IsFile, data[0].file);
            Assert.AreEqual(tableObject.Etag, data[0].etag);
        }
        #endregion

        #region ImportData
        [Test]
        public async Task ImportDataShouldCopyAllTableObjectsIntoTheDatabase()
        {
            // Arrange
            string exportFolderName = "export";
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, Dav.Jwt);
            await davClassLibrary.DataAccess.DataManager.Sync();

            var progress = new Progress<int>();
            var exportFolder = Directory.CreateDirectory(Path.Combine(Dav.GetDavDataPath(), exportFolderName));
            await davClassLibrary.DataAccess.DataManager.ExportDataAsync(exportFolder, progress);
            progress = new Progress<int>();

            // Clear the database
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, null);
            var firstTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(Dav.TestDataFirstTableObject.uuid);
            var secondTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(Dav.TestDataSecondTableObject.uuid);
            await firstTableObjectFromDatabase.DeleteImmediatelyAsync();
            await secondTableObjectFromDatabase.DeleteImmediatelyAsync();

            // Act
            await davClassLibrary.DataAccess.DataManager.ImportDataAsync(exportFolder, progress);

            // Assert
            firstTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(Dav.TestDataFirstTableObject.uuid);
            secondTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(Dav.TestDataSecondTableObject.uuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.IsNotNull(secondTableObjectFromDatabase);

            Assert.AreEqual(Dav.TestDataFirstTableObject.table_id, firstTableObjectFromDatabase.TableId);
            Assert.AreEqual(Dav.TestDataFirstTableObject.uuid, firstTableObjectFromDatabase.Uuid);
            Assert.AreEqual(Dav.TestDataFirstTableObject.file, firstTableObjectFromDatabase.IsFile);
            Assert.AreEqual(Dav.TestDataFirstTableObject.properties[Dav.TestDataFirstPropertyName], firstTableObjectFromDatabase.GetPropertyValue(Dav.TestDataFirstPropertyName));
            Assert.AreEqual(Dav.TestDataFirstTableObject.properties[Dav.TestDataSecondPropertyName], firstTableObjectFromDatabase.GetPropertyValue(Dav.TestDataSecondPropertyName));

            Assert.AreEqual(Dav.TestDataSecondTableObject.table_id, secondTableObjectFromDatabase.TableId);
            Assert.AreEqual(Dav.TestDataSecondTableObject.uuid, secondTableObjectFromDatabase.Uuid);
            Assert.AreEqual(Dav.TestDataSecondTableObject.file, secondTableObjectFromDatabase.IsFile);
            Assert.AreEqual(Dav.TestDataSecondTableObject.properties[Dav.TestDataFirstPropertyName], secondTableObjectFromDatabase.GetPropertyValue(Dav.TestDataFirstPropertyName));
            Assert.AreEqual(Dav.TestDataSecondTableObject.properties[Dav.TestDataSecondPropertyName], secondTableObjectFromDatabase.GetPropertyValue(Dav.TestDataSecondPropertyName));
        }

        [Test]
        public async Task ImportDataShouldCopyAllTableObjectFiles()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, null);
            var uuid = Guid.NewGuid();
            string exportFolderName = "export";
            FileInfo file = new FileInfo(Path.Combine(Dav.ProjectDirectory, "Assets", "image.jpg"));
            var tableObject = await TableObject.CreateAsync(uuid, Dav.TestFileTableId, file);
            FileAssert.Exists(Path.Combine(Dav.GetDavDataPath(), Dav.TestFileTableId.ToString(), uuid.ToString()));

            var progress = new Progress<int>();
            var exportFolder = Directory.CreateDirectory(Path.Combine(Dav.GetDavDataPath(), exportFolderName));
            await davClassLibrary.DataAccess.DataManager.ExportDataAsync(exportFolder, progress);
            progress = new Progress<int>();

            // Clear the database
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, null);
            var tableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);
            await tableObjectFromDatabase.DeleteImmediatelyAsync();
            FileAssert.DoesNotExist(Path.Combine(Dav.GetDavDataPath(), Dav.TestFileTableId.ToString(), uuid.ToString()));

            // Act
            await davClassLibrary.DataAccess.DataManager.ImportDataAsync(exportFolder, progress);

            // Assert
            tableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(uuid);
            Assert.IsNotNull(tableObjectFromDatabase);
            FileAssert.Exists(Path.Combine(Dav.GetDavDataPath(), Dav.TestFileTableId.ToString(), uuid.ToString()));
            Assert.AreEqual(tableObject.TableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(tableObject.Uuid, tableObjectFromDatabase.Uuid);
            Assert.AreEqual(tableObject.IsFile, tableObjectFromDatabase.IsFile);
        }
        #endregion
    }
}
