using davClassLibrary.Common;
using davClassLibrary.Models;
using davClassLibrary.Tests.Common;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using static davClassLibrary.Models.TableObject;

namespace davClassLibrary.Tests.Models
{
    [TestFixture]
    public class TableObject
    {
        #region Setup
        [SetUp]
        public void Setup()
        {
            
        }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
            // Delete the data folder
            Directory.Delete(Dav.GetDavDataPath(), true);

            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.RetrieveConstants = new RetrieveConstants();
            ProjectInterface.TriggerAction = new TriggerAction();
        }

        [OneTimeTearDown]
        public void GlobalTeardown()
        {
            
        }
        #endregion

        #region Constructors
        [Test]
        public void ConstructorWithTableIdShouldCreateNewTableObject()
        {
            // Arrange
            int tableId = 4;

            // Act
            var tableObject = new davClassLibrary.Models.TableObject(tableId);

            // Assert
            Assert.AreEqual(tableId, tableObject.TableId);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(tableObject.Uuid);
            Assert.IsNotNull(tableObject2);
            Assert.AreEqual(tableObject.Id, tableObject2.Id);
            Assert.AreEqual(tableObject.TableId, tableObject2.TableId);
            Assert.AreEqual(tableObject.Uuid, tableObject2.Uuid);
        }

        [Test]
        public void ConstructorWithUuidAndTableIdShouldCreateNewTableObject()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();

            // Act
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId);

            // Assert
            Assert.AreEqual(tableId, tableObject.TableId);
            Assert.AreEqual(uuid, tableObject.Uuid);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.IsNotNull(tableObject2);
            Assert.AreEqual(tableObject.TableId, tableObject2.TableId);
            Assert.AreEqual(tableObject.Id, tableObject2.Id);
            Assert.AreEqual(tableObject.Uuid, tableObject2.Uuid);
        }

        [Test]
        public void ConstructorWithUuidTableIdAndFileShouldCreateNewTableObject()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            FileInfo file = new FileInfo(Path.Combine(Dav.ProjectDirectory, "Assets", "image.jpg"));
            string newFilePath = Path.Combine(Dav.GetDavDataPath(), tableId.ToString(), uuid.ToString());

            // Act
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, file);

            // Assert
            Assert.AreEqual(tableId, tableObject.TableId);
            Assert.AreEqual(uuid, tableObject.Uuid);
            Assert.IsTrue(tableObject.IsFile);
            Assert.AreEqual(newFilePath, tableObject.File.FullName);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.IsNotNull(tableObject2);
            Assert.AreEqual(tableObject.TableId, tableObject2.TableId);
            Assert.AreEqual(tableObject.Id, tableObject2.Id);
            Assert.AreEqual(tableObject.Uuid, tableObject2.Uuid);
            Assert.AreEqual(newFilePath, tableObject2.File.FullName);
            Assert.AreEqual(tableObject.Properties.Find(prop => prop.Name == "ext").Value, file.Extension.Replace(".", ""));
        }

        [Test]
        public void ConstructorWithUuidTableIdAndPropertiesShouldCreateNewTableObject()
        {
            // Arrange
            var tableId = 4;
            Guid uuid = Guid.NewGuid();
            List<Property> propertiesList = new List<Property>
            {
                new Property{ Name = "page1", Value = "Hallo Welt" },
                new Property{ Name = "page2", Value = "Hello World" }
            };

            // Act
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, propertiesList);

            // Assert
            Assert.AreEqual(tableId, tableObject.TableId);
            Assert.AreEqual(uuid, tableObject.Uuid);
            Assert.AreEqual(propertiesList, tableObject.Properties);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.IsNotNull(tableObject2);
            Assert.AreEqual(tableObject.TableId, tableObject2.TableId);
            Assert.AreEqual(tableObject.Id, tableObject2.Id);
            Assert.AreEqual(tableObject.Uuid, tableObject2.Uuid);
        }
        #endregion

        #region SetVisibility
        [Test]
        public void SetVisibilityShouldSetTheVisibilityOfTheTableObjectAndSaveItInTheDatabase()
        {
            // Arrange
            int tableId = 4;
            var tableObject = new davClassLibrary.Models.TableObject(tableId);
            TableObjectVisibility oldVisibility = tableObject.Visibility;
            TableObjectVisibility newVisibility = TableObjectVisibility.Public;

            // Act
            tableObject.SetVisibility(newVisibility);

            // Assert
            Assert.AreEqual(newVisibility, tableObject.Visibility);
            Assert.AreNotEqual(newVisibility, oldVisibility);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(tableObject.Uuid);
            Assert.NotNull(tableObject2);
            Assert.AreEqual(newVisibility, tableObject2.Visibility);
        }
        #endregion

        #region SetFile
        [Test]
        public void SetFileShouldCopyTheFileAndSaveTheExtInTheDatabase()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            FileInfo oldFile = new FileInfo(Path.Combine(Dav.ProjectDirectory, "Assets", "image.jpg"));
            FileInfo newFile = new FileInfo(Path.Combine(Dav.ProjectDirectory, "Assets", "icon.ico"));

            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, oldFile);

            // Act
            tableObject.SetFile(newFile);

            // Assert
            Assert.AreNotEqual(oldFile, tableObject.File);
            Assert.AreEqual(newFile.Length, tableObject.File.Length);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.NotNull(tableObject2);
            Assert.AreEqual(newFile.Length, tableObject2.File.Length);
            Assert.AreEqual(tableObject2.Properties.Find(prop => prop.Name == "ext").Value, newFile.Extension.Replace(".", ""));
        }

        [Test]
        public void SetFileShouldNotWorkWhenTheTableObjectIsNotAFile()
        {
            // Arrange
            int tableId = 4;
            FileInfo file = new FileInfo(Path.Combine(Dav.ProjectDirectory, "Assets", "image.jpg"));
            var tableObject = new davClassLibrary.Models.TableObject(tableId);

            // Act
            tableObject.SetFile(file);

            // Assert
            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(tableObject.Uuid);
            Assert.IsFalse(tableObject2.IsFile);
            Assert.IsNull(tableObject2.File);
        }
        #endregion

        #region SetPropertyValue
        [Test]
        public void SetPropertyValueShouldCreateANewPropertyAndSaveItInTheDatabase()
        {
            // Arrange
            int tableId = 4;
            string propertyName = "page1";
            string propertyValue = "Hello World";
            var tableObject = new davClassLibrary.Models.TableObject(tableId);

            // Act
            tableObject.SetPropertyValue(propertyName, propertyValue);

            // Assert
            Assert.AreEqual(tableObject.Properties.Count, 1);
            Assert.AreEqual(tableObject.Properties[0].Name, propertyName);
            Assert.AreEqual(tableObject.Properties[0].Value, propertyValue);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(tableObject.Uuid);
            Assert.NotNull(tableObject2);
            Assert.AreEqual(tableObject2.Properties.Count, 1);
            Assert.AreEqual(tableObject2.Properties[0].Name, propertyName);
            Assert.AreEqual(tableObject2.Properties[0].Value, propertyValue);
        }

        [Test]
        public void SetPropertyValueShouldUpdateAnExistingPropertyAndSaveItInTheDatabase()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            string propertyName = "page1";
            string oldPropertyValue = "Hello World";
            string newPropertyValue = "Hallo Welt";
            List<Property> propertiesList = new List<Property>
            {
                new Property{ Name = propertyName, Value = oldPropertyValue }
            };
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, propertiesList);

            Assert.AreEqual(tableObject.Properties.Count, 1);
            Assert.AreEqual(tableObject.Properties[0].Name, propertyName);
            Assert.AreEqual(tableObject.Properties[0].Value, oldPropertyValue);

            // Act
            tableObject.SetPropertyValue(propertyName, newPropertyValue);

            // Assert
            Assert.AreEqual(tableObject.Properties.Count, 1);
            Assert.AreEqual(tableObject.Properties[0].Name, propertyName);
            Assert.AreEqual(tableObject.Properties[0].Value, newPropertyValue);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.AreEqual(tableObject2.Properties.Count, 1);
            Assert.AreEqual(tableObject2.Properties[0].Name, propertyName);
            Assert.AreEqual(tableObject2.Properties[0].Value, newPropertyValue);
        }
        #endregion

        #region GetPropertyValue
        [Test]
        public void GetPropertyValueShouldReturnTheValueOfTheProperty()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            string propertyName = "page1";
            string propertyValue = "Hello World";
            List<Property> propertiesList = new List<Property>
            {
                new Property{ Name = propertyName, Value = propertyValue }
            };
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, propertiesList);

            Assert.AreEqual(tableObject.Properties.Count, 1);
            Assert.AreEqual(tableObject.Properties[0].Name, propertyName);
            Assert.AreEqual(tableObject.Properties[0].Value, propertyValue);

            // Act
            string newPropertyValue = tableObject.GetPropertyValue(propertyName);

            // Assert
            Assert.AreEqual(propertyValue, newPropertyValue);
        }

        [Test]
        public void GetPropertyValueShouldReturnNullIfThePropertyDoesNotExist()
        {
            // Arrange
            int tableId = 4;
            string propertyName = "page1";
            var tableObject = new davClassLibrary.Models.TableObject(tableId);

            // Act
            var value = tableObject.GetPropertyValue(propertyName);

            Assert.IsNull(value);
        }
        #endregion

        #region RemoveProperty
        [Test]
        public void RemovePropertyShouldRemoveThePropertyFromThePropertiesListAndDeleteItFromTheDatabase()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            string propertyName = "page1";
            string propertyValue = "Hello World";
            List<Property> propertiesList = new List<Property>
            {
                new Property{ Name = propertyName, Value = propertyValue }
            };
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, propertiesList);

            // Act
            tableObject.RemoveProperty(propertyName);

            // Assert
            Assert.AreEqual(0, tableObject.Properties.Count);
            Assert.IsNull(tableObject.GetPropertyValue(propertyName));

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(tableObject.Uuid);
            Assert.NotNull(tableObject2);
            Assert.AreEqual(0, tableObject2.Properties.Count);
            Assert.IsNull(tableObject2.GetPropertyValue(propertyName));
        }
        #endregion

        #region RemoveAllProperties
        [Test]
        public void RemoveAllPropertiesShouldRemoveAllPropertiesAndDeleteThemFromTheDatabase()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            List<Property> propertiesList = new List<Property>
            {
                new Property{Name = "page1", Value = "Hello World"},
                new Property{Name = "page2", Value = "Hallo Welt"}
            };
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, propertiesList);

            // Act
            tableObject.RemoveAllProperties();

            // Assert
            Assert.AreEqual(0, tableObject.Properties.Count);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.AreEqual(0, tableObject2.Properties.Count);
        }
        #endregion

        #region SetUploadStatus
        [Test]
        public void SetUploadStatusShouldUpdateTheUploadStatusOfTheTableObjectAndSaveItInTheDatabase()
        {
            // Arrange
            int tableId = 4;
            var tableObject = new davClassLibrary.Models.TableObject(tableId);
            TableObjectUploadStatus newUploadStatus = TableObjectUploadStatus.NoUpload;

            Assert.AreEqual(TableObjectUploadStatus.New, tableObject.UploadStatus);

            // Act
            tableObject.SetUploadStatus(newUploadStatus);

            // Assert
            Assert.AreEqual(newUploadStatus, tableObject.UploadStatus);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(tableObject.Uuid);
            Assert.IsNotNull(tableObject);
            Assert.AreEqual(newUploadStatus, tableObject2.UploadStatus);
        }
        #endregion

        #region Delete
        [Test]
        public void DeleteShouldSetTheUploadStatusOfTheTableObjectToDeleted()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            List<Property> propertiesList = new List<Property>
            {
                new Property{Name = "page1", Value = "Hello World"},
                new Property{Name = "page2", Value = "Hallo Welt"}
            };
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, propertiesList);

            // Act
            tableObject.Delete();

            // Assert
            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.IsNotNull(tableObject2);
            Assert.AreEqual(TableObjectUploadStatus.Deleted, tableObject2.UploadStatus);
        }

        [Test]
        public void DeleteShouldDeleteTheFileOfATableObjectAndSetTheUploadStatusToDeleted()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            FileInfo file = new FileInfo(Path.Combine(Dav.ProjectDirectory, "Assets", "icon.ico"));
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, file);
            string filePath = tableObject.File.FullName;

            // Act
            tableObject.Delete();

            // Assert
            Assert.IsFalse(File.Exists(filePath));
            Assert.AreEqual(TableObjectUploadStatus.Deleted, tableObject.UploadStatus);

            var tableObject2 = davClassLibrary.Dav.Database.GetTableObject(uuid);
            Assert.IsNotNull(tableObject2);
            Assert.AreEqual(TableObjectUploadStatus.Deleted, tableObject2.UploadStatus);
        }
        #endregion
    }
}
