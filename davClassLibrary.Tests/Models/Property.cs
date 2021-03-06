﻿using davClassLibrary.Common;
using davClassLibrary.Tests.Common;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace davClassLibrary.Tests.Models
{
    [TestFixture][SingleThreaded]
    public class Property
    {
        #region Setup
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.RetrieveConstants = new RetrieveConstants();
            ProjectInterface.TriggerAction = new TriggerAction();
            ProjectInterface.GeneralMethods = new GeneralMethods();
        }

        [SetUp]
        public async Task Setup()
        {
            // Delete all files and folders in the test folder except the database file
            var davFolder = new DirectoryInfo(Dav.GetDavDataPath());
            foreach (var folder in davFolder.GetDirectories())
                folder.Delete(true);

            // Clear the database
            var database = new davClassLibrary.DataAccess.DavDatabase();
            await database.DropAsync();
        }
        #endregion

        #region Create
        [Test]
        public async Task CreateWithTableObjectIdNameAndValueShouldCreateNewProperty()
        {
            // Arrange
            int tableObjectId = -1;
            string propertyName = "test1";
            string propertyValue = "value";

            // Act
            var property = await davClassLibrary.Models.Property.CreateAsync(tableObjectId, propertyName, propertyValue);

            // Assert
            var propertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(property.Id);
            Assert.AreEqual(property.Id, propertyFromDatabase.Id);
            Assert.AreEqual(tableObjectId, propertyFromDatabase.TableObjectId);
            Assert.AreEqual(propertyName, propertyFromDatabase.Name);
            Assert.AreEqual(propertyValue, propertyFromDatabase.Value);
        }
        #endregion

        #region SetValue
        [Test]
        public async Task SetValueShouldUpdateThePropertyWithTheNewValueInTheDatabase()
        {
            // Arrange
            string propertyName = "test1";
            string oldPropertyValue = "bla";
            string newPropertyValue = "blablabla";
            int tableObjectId = -13;
            var property = await davClassLibrary.Models.Property.CreateAsync(tableObjectId, propertyName, oldPropertyValue);

            // Act
            await property.SetValueAsync(newPropertyValue);

            // Assert
            var propertyFromDatabase = await davClassLibrary.Dav.Database.GetPropertyAsync(property.Id);
            Assert.AreEqual(newPropertyValue, property.Value);
            Assert.AreEqual(newPropertyValue, propertyFromDatabase.Value);
            Assert.AreEqual(propertyName, propertyFromDatabase.Name);
            Assert.AreEqual(tableObjectId, propertyFromDatabase.TableObjectId);
        }
        #endregion

        #region ToPropertyData
        [Test]
        public void ToPropertyDataShouldReturnValidPropertyDataObject()
        {
            // Arrange
            int tableObjectId = -14;
            string propertyName = "test123";
            string propertyValue = "blabla";
            var property = new davClassLibrary.Models.Property(tableObjectId, propertyName, propertyValue);

            // Act
            var propertyData = property.ToPropertyData();

            // Assert
            Assert.AreEqual(property.Id, propertyData.id);
            Assert.AreEqual(tableObjectId, propertyData.table_object_id);
            Assert.AreEqual(propertyName, propertyData.name);
            Assert.AreEqual(propertyValue, propertyData.value);
        }
        #endregion

        #region ConvertPropertyDataToProperty
        [Test]
        public void ConvertPropertyDataToPropertyShouldReturnValidProperty()
        {
            // Arrange
            int tableObjectId = -12;
            string propertyName = "blablabla";
            string propertyValue = "asdasdasd";
            var propertyData = new davClassLibrary.Models.PropertyData
            {
                table_object_id = tableObjectId,
                name = propertyName,
                value = propertyValue
            };

            // Act
            var property = davClassLibrary.Models.Property.ConvertPropertyDataToProperty(propertyData);

            // Assert
            Assert.AreEqual(tableObjectId, property.TableObjectId);
            Assert.AreEqual(propertyName, property.Name);
            Assert.AreEqual(propertyValue, property.Value);
        }
        #endregion
    }
}
