using davClassLibrary.Common;
using davClassLibrary.Models;
using davClassLibrary.Tests.Common;
using NUnit.Framework;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using static davClassLibrary.Models.TableObject;

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

            // Tidy up
            tableObjectFromDatabase.DeleteImmediately();
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

            // Tidy up
            tableObjectFromDatabase.DeleteImmediately();
        }
        #endregion

        #region GetAllTableObjects(bool deleted)
        [Test]
        public void GetAllTableObjectsShouldReturnAllTableObjects()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var tableObjects = new List<davClassLibrary.Models.TableObject>
            {
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), 13),
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), 13),
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), 12)
            };
            tableObjects[0].SetUploadStatus(TableObjectUploadStatus.Deleted);

            // Act
            var allTableObjects = davClassLibrary.Dav.Database.GetAllTableObjects(true);

            // Assert
            Assert.AreEqual(tableObjects.Count, allTableObjects.Count);

            // Tidy up
            foreach(var tableObject in tableObjects)
                tableObject.DeleteImmediately();
        }

        [Test]
        public void GetAllTableObjectsShouldReturnAllTableObjectsExceptDeletedOnes()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            var tableObjects = new List<davClassLibrary.Models.TableObject>
            {
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), 13),
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), 13),
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), 12)
            };
            tableObjects[0].SetUploadStatus(TableObjectUploadStatus.Deleted);

            // Act
            var allTableObjects = davClassLibrary.Dav.Database.GetAllTableObjects(false);

            // Assert
            Assert.AreEqual(tableObjects.Count - 1, allTableObjects.Count);

            // Tidy up
            foreach (var tableObject in tableObjects)
                tableObject.DeleteImmediately();
        }
        #endregion

        #region GetAllTableObjects(int tableId, bool deleted)
        [Test]
        public void GetAllTableObjectsWithTableIdShouldReturnAllTableObjectsOfTheTable()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableId = 4;
            var tableObjects = new List<davClassLibrary.Models.TableObject>
            {
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), tableId),
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), tableId),
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), tableId),
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), 3)
            };
            tableObjects[0].SetUploadStatus(TableObjectUploadStatus.Deleted);

            // Act
            var allTableObjects = davClassLibrary.Dav.Database.GetAllTableObjects(tableId, true);

            // Assert
            Assert.AreEqual(tableObjects.Count - 1, allTableObjects.Count);

            // Tidy up
            foreach (var tableObject in tableObjects)
                tableObject.DeleteImmediately();
        }

        [Test]
        public void GetAllTableObjectsWithTableIdShouldReturnAlltableObjectsOfTheTableExceptDeletedOnes()
        {
            // Arrange
            SQLiteConnection database = new SQLiteConnection(databasePath);
            int tableId = 4;
            var tableObjects = new List<davClassLibrary.Models.TableObject>
            {
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), tableId),
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), tableId),
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), tableId),
                new davClassLibrary.Models.TableObject(Guid.NewGuid(), 3),
            };
            tableObjects[0].SetUploadStatus(TableObjectUploadStatus.Deleted);

            // Act
            var allTableObjects = davClassLibrary.Dav.Database.GetAllTableObjects(tableId, false);

            // Assert
            Assert.AreEqual(tableObjects.Count - 2, allTableObjects.Count);

            // Tidy up
            foreach (var tableObject in tableObjects)
                tableObject.DeleteImmediately();
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

            // Tidy up
            tableObjectFromDatabase.DeleteImmediately();
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

            // Tidy up
            tableObject.DeleteImmediately();
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

            // Tidy up
            tableObject.DeleteImmediately();
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

            // Tidy up
            tableObjectFromDatabase.DeleteImmediately();
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

            // Tidy up
            tableObjectFromDatabase.DeleteImmediately();
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

            // Tidy up
            tableObjectFromDatabase.DeleteImmediately();
        }

        [Test]
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

            // Tidy up
            davClassLibrary.Dav.Database.DeleteProperty(propertyFromDatabase);
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

            // Tidy up
            davClassLibrary.Dav.Database.DeleteProperty(propertyFromDatabase);
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

            // Tidy up
            davClassLibrary.Dav.Database.DeleteProperty(property);
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

            // Tidy up
            davClassLibrary.Dav.Database.DeleteProperty(propertyFromDatabase);
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

        #region HttpGet
        [Test]
        public async Task HttpGetShouldSendGetRequestToExistingResourceAndReturnTrue()
        {
            // Arrange
            var uuid = Dav.TestDataFirstTableObject.uuid;

            // Act
            var response = await davClassLibrary.DataAccess.DataManager.HttpGet(Dav.Jwt, "apps/object/" + uuid);

            // Assert
            Assert.IsTrue(response.Key);
        }

        [Test]
        public async Task HttpGetShouldSendGetRequestToNotExistingResourceAndReturnFalse()
        {
            // Arrange
            var uuid = Guid.NewGuid();

            // Act
            var response = await davClassLibrary.DataAccess.DataManager.HttpGet(Dav.Jwt, "apps/object/" + uuid);

            // Assert
            Assert.IsFalse(response.Key);
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
            var fileTableObject = davClassLibrary.Dav.Database.GetAllTableObjects(Dav.TableIds[1], false)[0];
            var tableFolder = Directory.CreateDirectory(Path.Combine(Dav.GetDavDataPath(), fileTableObject.TableId.ToString()));
            File.Copy(Path.Combine(Dav.ProjectDirectory, "Assets", "image.jpg"), Path.Combine(tableFolder.FullName, fileTableObject.Uuid.ToString()), true);

            var progress = new Progress<int>();
            var exportFolder = Directory.CreateDirectory(Path.Combine(Dav.GetDavDataPath(), "export"));

            // Act
            await davClassLibrary.DataAccess.DataManager.ExportData(exportFolder, progress);

            // Assert
            FileInfo dataFile = new FileInfo(Path.Combine(exportFolder.FullName, davClassLibrary.Dav.ExportDataFileName));
            FileAssert.Exists(dataFile);

            // Read the file and deserialize the content
            var firstTableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(Dav.TestDataFirstTableObject.uuid);
            var secondTableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(Dav.TestDataSecondTableObject.uuid);
            var data = davClassLibrary.DataAccess.DataManager.GetDataFromFile(dataFile);
            Assert.AreEqual(firstTableObjectFromDatabase.Id, data[0].id);
            Assert.AreEqual(firstTableObjectFromDatabase.TableId, data[0].table_id);
            Assert.AreEqual(davClassLibrary.Models.TableObject.ParseVisibilityToInt(firstTableObjectFromDatabase.Visibility), data[0].visibility);
            Assert.AreEqual(firstTableObjectFromDatabase.Uuid, data[0].uuid);
            Assert.AreEqual(firstTableObjectFromDatabase.IsFile, data[0].file);
            Assert.AreEqual(firstTableObjectFromDatabase.GetPropertyValue(Dav.TestDataFirstPropertyName), data[0].properties[Dav.TestDataFirstPropertyName]);
            Assert.AreEqual(firstTableObjectFromDatabase.GetPropertyValue(Dav.TestDataSecondPropertyName), data[0].properties[Dav.TestDataSecondPropertyName]);
            Assert.AreEqual(firstTableObjectFromDatabase.Etag, data[0].etag);

            Assert.AreEqual(secondTableObjectFromDatabase.Id, data[1].id);
            Assert.AreEqual(secondTableObjectFromDatabase.TableId, data[1].table_id);
            Assert.AreEqual(davClassLibrary.Models.TableObject.ParseVisibilityToInt(secondTableObjectFromDatabase.Visibility), data[1].visibility);
            Assert.AreEqual(secondTableObjectFromDatabase.Uuid, data[1].uuid);
            Assert.AreEqual(secondTableObjectFromDatabase.IsFile, data[1].file);
            Assert.AreEqual(secondTableObjectFromDatabase.GetPropertyValue(Dav.TestDataFirstPropertyName), data[1].properties[Dav.TestDataFirstPropertyName]);
            Assert.AreEqual(secondTableObjectFromDatabase.GetPropertyValue(Dav.TestDataSecondPropertyName), data[1].properties[Dav.TestDataSecondPropertyName]);
            Assert.AreEqual(secondTableObjectFromDatabase.Etag, data[1].etag);

            // Tidy up
            exportFolder.Delete(true);
            firstTableObjectFromDatabase.DeleteImmediately();
            secondTableObjectFromDatabase.DeleteImmediately();
            fileTableObject.DeleteImmediately();
        }

        [Test]
        public async Task ExportDataShouldCopyAllTableObjectFiles()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, null);
            var uuid = Guid.NewGuid();
            FileInfo file = new FileInfo(Path.Combine(Dav.ProjectDirectory, "Assets", "image.jpg"));
            var tableObject = new davClassLibrary.Models.TableObject(uuid, Dav.TestFileTableId, file);

            var progress = new Progress<int>();
            var exportFolder = Directory.CreateDirectory(Path.Combine(Dav.GetDavDataPath(), "export"));

            // Act
            await davClassLibrary.DataAccess.DataManager.ExportData(exportFolder, progress);

            // Assert
            FileInfo dataFile = new FileInfo(Path.Combine(exportFolder.FullName, davClassLibrary.Dav.ExportDataFileName));
            var data = davClassLibrary.DataAccess.DataManager.GetDataFromFile(dataFile);
            FileAssert.Exists(Path.Combine(exportFolder.FullName, Dav.TestFileTableId.ToString(), uuid.ToString()));
            FileAssert.Exists(dataFile);

            Assert.AreEqual(tableObject.Id, data[0].id);
            Assert.AreEqual(tableObject.TableId, data[0].table_id);
            Assert.AreEqual(davClassLibrary.Models.TableObject.ParseVisibilityToInt(tableObject.Visibility), data[0].visibility);
            Assert.AreEqual(tableObject.Uuid, data[0].uuid);
            Assert.AreEqual(tableObject.IsFile, data[0].file);
            Assert.AreEqual(tableObject.Etag, data[0].etag);

            // Tidy up
            exportFolder.Delete(true);
            tableObject.DeleteImmediately();
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
            await davClassLibrary.DataAccess.DataManager.ExportData(exportFolder, progress);
            progress = new Progress<int>();

            // Clear the database
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, null);
            var firstTableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(Dav.TestDataFirstTableObject.uuid);
            var secondTableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(Dav.TestDataSecondTableObject.uuid);
            firstTableObjectFromDatabase.DeleteImmediately();
            secondTableObjectFromDatabase.DeleteImmediately();

            // Act
            davClassLibrary.DataAccess.DataManager.ImportData(exportFolder, progress);

            // Assert
            firstTableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(Dav.TestDataFirstTableObject.uuid);
            secondTableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(Dav.TestDataSecondTableObject.uuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.IsNotNull(secondTableObjectFromDatabase);

            Assert.AreEqual(Dav.TestDataFirstTableObject.table_id, firstTableObjectFromDatabase.TableId);
            Assert.AreEqual(Dav.TestDataFirstTableObject.visibility, davClassLibrary.Models.TableObject.ParseVisibilityToInt(firstTableObjectFromDatabase.Visibility));
            Assert.AreEqual(Dav.TestDataFirstTableObject.uuid, firstTableObjectFromDatabase.Uuid);
            Assert.AreEqual(Dav.TestDataFirstTableObject.file, firstTableObjectFromDatabase.IsFile);
            Assert.AreEqual(Dav.TestDataFirstTableObject.properties[Dav.TestDataFirstPropertyName], firstTableObjectFromDatabase.GetPropertyValue(Dav.TestDataFirstPropertyName));
            Assert.AreEqual(Dav.TestDataFirstTableObject.properties[Dav.TestDataSecondPropertyName], firstTableObjectFromDatabase.GetPropertyValue(Dav.TestDataSecondPropertyName));

            Assert.AreEqual(Dav.TestDataSecondTableObject.table_id, secondTableObjectFromDatabase.TableId);
            Assert.AreEqual(Dav.TestDataSecondTableObject.visibility, davClassLibrary.Models.TableObject.ParseVisibilityToInt(secondTableObjectFromDatabase.Visibility));
            Assert.AreEqual(Dav.TestDataSecondTableObject.uuid, secondTableObjectFromDatabase.Uuid);
            Assert.AreEqual(Dav.TestDataSecondTableObject.file, secondTableObjectFromDatabase.IsFile);
            Assert.AreEqual(Dav.TestDataSecondTableObject.properties[Dav.TestDataFirstPropertyName], secondTableObjectFromDatabase.GetPropertyValue(Dav.TestDataFirstPropertyName));
            Assert.AreEqual(Dav.TestDataSecondTableObject.properties[Dav.TestDataSecondPropertyName], secondTableObjectFromDatabase.GetPropertyValue(Dav.TestDataSecondPropertyName));

            // Tidy up
            exportFolder.Delete(true);
            firstTableObjectFromDatabase.DeleteImmediately();
            secondTableObjectFromDatabase.DeleteImmediately();
        }

        [Test]
        public async Task ImportDataShouldCopyAllTableObjectFiles()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, null);
            var uuid = Guid.NewGuid();
            string exportFolderName = "export";
            FileInfo file = new FileInfo(Path.Combine(Dav.ProjectDirectory, "Assets", "image.jpg"));
            var tableObject = new davClassLibrary.Models.TableObject(uuid, Dav.TestFileTableId, file);
            FileAssert.Exists(Path.Combine(Dav.GetDavDataPath(), Dav.TestFileTableId.ToString(), uuid.ToString()));

            var progress = new Progress<int>();
            var exportFolder = Directory.CreateDirectory(Path.Combine(Dav.GetDavDataPath(), exportFolderName));
            await davClassLibrary.DataAccess.DataManager.ExportData(exportFolder, progress);
            progress = new Progress<int>();

            // Clear the database
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, null);
            var tableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(uuid);
            tableObjectFromDatabase.DeleteImmediately();
            FileAssert.DoesNotExist(Path.Combine(Dav.GetDavDataPath(), Dav.TestFileTableId.ToString(), uuid.ToString()));

            // Act
            davClassLibrary.DataAccess.DataManager.ImportData(exportFolder, progress);

            // Assert
            tableObjectFromDatabase = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.IsNotNull(tableObjectFromDatabase);
            FileAssert.Exists(Path.Combine(Dav.GetDavDataPath(), Dav.TestFileTableId.ToString(), uuid.ToString()));
            Assert.AreEqual(tableObject.TableId, tableObjectFromDatabase.TableId);
            Assert.AreEqual(tableObject.Visibility, tableObjectFromDatabase.Visibility);
            Assert.AreEqual(tableObject.Uuid, tableObjectFromDatabase.Uuid);
            Assert.AreEqual(tableObject.IsFile, tableObjectFromDatabase.IsFile);

            // Tidy up
            exportFolder.Delete(true);
            tableObjectFromDatabase.DeleteImmediately();
        }
        #endregion
    }
}
