using davClassLibrary.Common;
using davClassLibrary.Tests.Common;
using NUnit.Framework;
using System;
using System.Diagnostics;
using System.IO;

namespace davClassLibrary.Tests.Models
{
    [TestFixture]
    public class TableObject
    {
        [SetUp]
        public void Setup()
        {
            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.RetrieveConstants = new RetrieveConstants();
            ProjectInterface.TriggerAction = new TriggerAction();
        }

        [Test]
        public void ConstructorWithTableIdShouldCreateNewTableObject()
        {
            // Arrange
            int tableId = 4;

            // Act
            var tableObject = new davClassLibrary.Models.TableObject(tableId);

            // Assert
            Assert.AreEqual(tableId, tableObject.TableId);
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
        }

        [Test]
        public void ConstructorWithUuidTableIdAndFileShouldCreateNewTableObject()
        {
            // Arrange
            int tableId = 4;
            Guid uuid = Guid.NewGuid();
            string filePath = Path.Combine(Dav.ProjectDirectory, "Assets", "image.jpg");
            FileInfo file = new FileInfo(filePath);

            // Act
            var tableObject = new davClassLibrary.Models.TableObject(uuid, tableId, file);

            Assert.AreEqual(tableId, tableObject.TableId);
            Assert.AreEqual(uuid, tableObject.Uuid);
            Assert.IsTrue(tableObject.IsFile);

            FileInfo newFile = new FileInfo(Path.Combine(Dav.GetDavDataPath(), tableObject.TableId.ToString(), tableObject.Uuid.ToString()));
            Assert.AreEqual(newFile.FullName, tableObject.File.FullName);
        }
    }
}
