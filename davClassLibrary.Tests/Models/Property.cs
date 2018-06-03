using davClassLibrary.Common;
using davClassLibrary.Tests.Common;
using NUnit.Framework;

namespace davClassLibrary.Tests.Models
{
    [TestFixture]
    public class Property
    {
        #region Setup
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.RetrieveConstants = new RetrieveConstants();
            ProjectInterface.TriggerAction = new TriggerAction();
        }
        #endregion

        #region Constructors
        [Test]
        public static void ConstructorWithTableObjectIdNameAndValueShouldCreateANewPropertyAndSaveItInTheDatabase()
        {
            // Arrange
            int tableObjectId = -1;
            string propertyName = "test1";
            string propertyValue = "value";

            // Act
            var property = new davClassLibrary.Models.Property(tableObjectId, propertyName, propertyValue);

            // Assert
            var propertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(property.Id);
            Assert.AreEqual(property.Id, propertyFromDatabase.Id);
            Assert.AreEqual(tableObjectId, propertyFromDatabase.TableObjectId);
            Assert.AreEqual(propertyName, propertyFromDatabase.Name);
            Assert.AreEqual(propertyValue, propertyFromDatabase.Value);
        }
        #endregion

        #region SetValue
        [Test]
        public void SetValueShouldUpdateThePropertyWithTheNewValueInTheDatabase()
        {
            // Arrange
            string propertyName = "test1";
            string oldPropertyValue = "bla";
            string newPropertyValue = "blablabla";
            int tableObjectId = -13;
            var property = new davClassLibrary.Models.Property(tableObjectId, propertyName, oldPropertyValue);

            // Act
            property.SetValue(newPropertyValue);

            // Assert
            var propertyFromDatabase = davClassLibrary.Dav.Database.GetProperty(property.Id);
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
