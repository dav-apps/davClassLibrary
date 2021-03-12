using davClassLibrary.Models;
using NUnit.Framework;
using System.Threading.Tasks;

namespace davClassLibrary.Tests.Models
{
    [TestFixture][SingleThreaded]
    public class PropertyTest
    {
        #region Setup
        [OneTimeSetUp]
        public void GlobalSetup()
        {
            Utils.GlobalSetup();
        }

        [SetUp]
        public async Task Setup()
        {
            await Utils.Setup();
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
            var property = await Property.CreateAsync(tableObjectId, propertyName, propertyValue);

            // Assert
            var propertyFromDatabase = await Dav.Database.GetPropertyAsync(property.Id);
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
            var property = await Property.CreateAsync(tableObjectId, propertyName, oldPropertyValue);

            // Act
            await property.SetValueAsync(newPropertyValue);

            // Assert
            var propertyFromDatabase = await Dav.Database.GetPropertyAsync(property.Id);
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
            var property = new Property(tableObjectId, propertyName, propertyValue);

            // Act
            var propertyData = property.ToPropertyData();

            // Assert
            Assert.AreEqual(property.Id, propertyData.id);
            Assert.AreEqual(tableObjectId, propertyData.table_object_id);
            Assert.AreEqual(propertyName, propertyData.name);
            Assert.AreEqual(propertyValue, propertyData.value);
        }
        #endregion
    }
}
