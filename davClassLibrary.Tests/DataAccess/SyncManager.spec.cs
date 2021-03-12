using davClassLibrary.Controllers;
using davClassLibrary.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace davClassLibrary.Tests.DataAccess
{
    [TestFixture][SingleThreaded]
    class SyncManagerTest
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
            // Delete all files and folders in the test folder except the database file
            var davFolder = new DirectoryInfo(Utils.GetDavDataPath());
            foreach (var folder in davFolder.GetDirectories())
                folder.Delete(true);

            // Clear the database
            var database = new davClassLibrary.DataAccess.DavDatabase();
            await database.DropAsync();
        }
        #endregion

        #region Sync
        public async Task SyncShouldDownloadAllTableObjectsFromTheServerAndUpdateThePropertiesOfExistingTableObjects()
        {
            // Arrange
            davClassLibrary.Dav.IsLoggedIn = true;
            davClassLibrary.Dav.AccessToken = Constants.testerXTestAppAccessToken;

            var firstTableObjectUuid = Guid.NewGuid();
            var firstTableObjectTableId = Constants.testAppFirstTestTableId;
            var firstTableObjectFirstPropertyName = "page1";
            var firstTableObjectFirstPropertyValue = "Hello World";
            var firstTableObjectSecondPropertyName = "page2";
            var firstTableObjectSecondPropertyValue = "Hallo Welt";

            var secondTableObjectUuid = Guid.NewGuid();
            var secondTableObjectTableId = Constants.testAppSecondTestTableId;
            var secondTableObjectFirstPropertyName = "test1";
            var secondTableObjectFirstPropertyValue = "First test";
            var secondTableObjectSecondPropertyName = "test2";
            var secondTableObjectSecondPropertyValue = "Second test";

            await TableObjectsController.CreateTableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                false,
                new Dictionary<string, string>
                {
                    { firstTableObjectFirstPropertyName, firstTableObjectFirstPropertyValue },
                    { firstTableObjectSecondPropertyName, firstTableObjectSecondPropertyValue }
                }
            );

            await TableObjectsController.CreateTableObject(
                secondTableObjectUuid,
                secondTableObjectTableId,
                false,
                new Dictionary<string, string>
                {
                    { secondTableObjectFirstPropertyName, secondTableObjectFirstPropertyValue },
                    { secondTableObjectSecondPropertyName, secondTableObjectSecondPropertyValue }
                }
            );

            // Act (1)
            await davClassLibrary.DataAccess.SyncManager.Sync();

            // Assert (1)
            var allTableObjects = await davClassLibrary.Dav.Database.GetAllTableObjectsAsync(false);
            Assert.AreEqual(2, allTableObjects.Count);

            var firstTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromDatabase.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, firstTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectSecondPropertyName));

            var secondTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNotNull(secondTableObjectFromDatabase);
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromDatabase.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, secondTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(secondTableObjectFirstPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectSecondPropertyName));

            // Arrange (2)
            var firstTableObjectFirstUpdatedPropertyValue = "First updated value";
            var firstTableObjectSecondUpdatedPropertyValue = "Erster aktualisierter Wert";

            var secondTableObjectFirstUpdatedPropertyValue = "Second updated value";
            var secondTableObjectSecondUpdatedPropertyValue = "Zweiter aktualisierter Wert";

            await TableObjectsController.UpdateTableObject(
                firstTableObjectUuid,
                new Dictionary<string, string>
                {
                    { firstTableObjectFirstPropertyName, firstTableObjectFirstUpdatedPropertyValue },
                    { firstTableObjectSecondPropertyName, firstTableObjectSecondUpdatedPropertyValue }
                }
            );

            await TableObjectsController.UpdateTableObject(
                secondTableObjectUuid,
                new Dictionary<string, string>
                {
                    { secondTableObjectFirstPropertyName, secondTableObjectFirstUpdatedPropertyValue },
                    { secondTableObjectSecondPropertyName, secondTableObjectSecondUpdatedPropertyValue }
                }
            );

            // Act (2)
            await davClassLibrary.DataAccess.SyncManager.Sync();

            // Assert (2)
            allTableObjects = await davClassLibrary.Dav.Database.GetAllTableObjectsAsync(false);
            Assert.AreEqual(2, allTableObjects.Count);

            firstTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromDatabase.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, firstTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstUpdatedPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondUpdatedPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectSecondPropertyName));

            secondTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNotNull(secondTableObjectFromDatabase);
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromDatabase.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, secondTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(secondTableObjectFirstUpdatedPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondUpdatedPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectSecondPropertyName));
        }

        public async Task SyncShouldRemoveTheTableObjectsThatAreNotOnTheServer()
        {
            // Arrange
            davClassLibrary.Dav.IsLoggedIn = true;
            davClassLibrary.Dav.AccessToken = Constants.testerXTestAppAccessToken;

            var firstTableObjectUuid = Guid.NewGuid();
            var firstTableObjectTableId = Constants.testAppFirstTestTableId;
            var firstTableObjectFirstPropertyName = "page1";
            var firstTableObjectFirstPropertyValue = "Hello World";
            var firstTableObjectSecondPropertyName = "page2";
            var firstTableObjectSecondPropertyValue = "Hallo Welt";

            var secondTableObjectUuid = Guid.NewGuid();
            var secondTableObjectTableId = Constants.testAppSecondTestTableId;
            var secondTableObjectFirstPropertyName = "test1";
            var secondTableObjectFirstPropertyValue = "First test";
            var secondTableObjectSecondPropertyName = "test2";
            var secondTableObjectSecondPropertyValue = "Second test";

            var localTableObjectUuid = Guid.NewGuid();
            var localTableObjectTableId = Constants.testAppFirstTestTableId;
            var localTableObjectFirstPropertyName = "page1";
            var localTableObjectFirstPropertyValue = "Guten Tag";
            var localTableObjectSecondPropertyName = "page2";
            var localTableObjectSecondPropertyValue = "Good day";

            await TableObjectsController.CreateTableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                false,
                new Dictionary<string, string>
                {
                    { firstTableObjectFirstPropertyName, firstTableObjectFirstPropertyValue },
                    { firstTableObjectSecondPropertyName, firstTableObjectSecondPropertyValue }
                }
            );

            await TableObjectsController.CreateTableObject(
                secondTableObjectUuid,
                secondTableObjectTableId,
                false,
                new Dictionary<string, string>
                {
                    { secondTableObjectFirstPropertyName, secondTableObjectFirstPropertyValue },
                    { secondTableObjectSecondPropertyName, secondTableObjectSecondPropertyValue }
                }
            );

            await davClassLibrary.Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
                localTableObjectUuid,
                localTableObjectTableId,
                new List<Property>
                {
                    new Property(0, localTableObjectFirstPropertyName, localTableObjectFirstPropertyValue),
                    new Property(0, localTableObjectSecondPropertyName, localTableObjectSecondPropertyValue)
                },
                TableObjectUploadStatus.UpToDate
            ));

            // Act
            await davClassLibrary.DataAccess.SyncManager.Sync();

            // Assert
            var allTableObjects = await davClassLibrary.Dav.Database.GetAllTableObjectsAsync(false);
            Assert.AreEqual(2, allTableObjects.Count);

            var firstTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromDatabase.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, firstTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectSecondPropertyName));

            var secondTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNotNull(secondTableObjectFromDatabase);
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromDatabase.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, secondTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(secondTableObjectFirstPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectSecondPropertyName));
        }
        #endregion

        #region SyncPush
        public async Task SyncPushShouldCreateTableObjectsOnTheServer()
        {
            // Arrange
            davClassLibrary.Dav.IsLoggedIn = true;
            davClassLibrary.Dav.AccessToken = Constants.testerXTestAppAccessToken;

            var firstTableObjectUuid = Guid.NewGuid();
            var firstTableObjectTableId = Constants.testAppFirstTestTableId;
            var firstTableObjectFirstPropertyName = "page1";
            var firstTableObjectFirstPropertyValue = "Hello World";
            var firstTableObjectSecondPropertyName = "page2";
            var firstTableObjectSecondPropertyValue = "Hallo Welt";

            var secondTableObjectUuid = Guid.NewGuid();
            var secondTableObjectTableId = Constants.testAppSecondTestTableId;
            var secondTableObjectFirstPropertyName = "test1";
            var secondTableObjectFirstPropertyValue = "First test";
            var secondTableObjectSecondPropertyName = "test2";
            var secondTableObjectSecondPropertyValue = "Second test";

            await davClassLibrary.Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                new List<Property>
                {
                    new Property(firstTableObjectFirstPropertyName, firstTableObjectFirstPropertyValue),
                    new Property(firstTableObjectSecondPropertyName, firstTableObjectSecondPropertyValue)
                },
                TableObjectUploadStatus.New
            ));

            await davClassLibrary.Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
                secondTableObjectUuid,
                secondTableObjectTableId,
                new List<Property>
                {
                    new Property(secondTableObjectFirstPropertyName, secondTableObjectFirstPropertyValue),
                    new Property(secondTableObjectSecondPropertyName, secondTableObjectSecondPropertyValue)
                },
                TableObjectUploadStatus.New
            ));

            // Act
            await davClassLibrary.DataAccess.SyncManager.SyncPush();

            // Assert
            var firstTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.AreEqual(TableObjectUploadStatus.UpToDate, firstTableObjectFromDatabase.UploadStatus);

            var secondTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNotNull(secondTableObjectFromDatabase);
            Assert.AreEqual(TableObjectUploadStatus.UpToDate, secondTableObjectFromDatabase.UploadStatus);

            var firstTableObjectFromServerResponse = await TableObjectsController.GetTableObject(firstTableObjectUuid);
            Assert.True(firstTableObjectFromServerResponse.Success);

            var firstTableObjectFromServer = firstTableObjectFromServerResponse.Data;
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromServer.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromServer.TableId);
            Assert.AreEqual(2, firstTableObjectFromServer.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstPropertyValue, firstTableObjectFromServer.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondPropertyValue, firstTableObjectFromServer.GetPropertyValue(firstTableObjectSecondPropertyName));

            var secondTableObjectFromServerResponse = await TableObjectsController.GetTableObject(secondTableObjectUuid);
            Assert.True(secondTableObjectFromServerResponse.Success);

            var secondTableObjectFromServer = secondTableObjectFromServerResponse.Data;
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromServer.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromServer.TableId);
            Assert.AreEqual(2, secondTableObjectFromServer.Properties.Count);
            Assert.AreEqual(secondTableObjectFirstPropertyValue, secondTableObjectFromServer.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondPropertyValue, secondTableObjectFromServer.GetPropertyValue(secondTableObjectSecondPropertyName));
        }

        public async Task SyncPushShouldUpdateTableObjectsOnTheServer()
        {
            // Arrange
            davClassLibrary.Dav.IsLoggedIn = true;
            davClassLibrary.Dav.AccessToken = Constants.testerXTestAppAccessToken;

            var firstTableObjectUuid = Guid.NewGuid();
            var firstTableObjectTableId = Constants.testAppFirstTestTableId;
            var firstTableObjectFirstPropertyName = "page1";
            var firstTableObjectFirstPropertyValue = "Hello World";
            var firstTableObjectSecondPropertyName = "page2";
            var firstTableObjectSecondPropertyValue = "Hallo Welt";

            var secondTableObjectUuid = Guid.NewGuid();
            var secondTableObjectTableId = Constants.testAppSecondTestTableId;
            var secondTableObjectFirstPropertyName = "test1";
            var secondTableObjectFirstPropertyValue = "First test";
            var secondTableObjectSecondPropertyName = "test2";
            var secondTableObjectSecondPropertyValue = "Second test";

            await TableObjectsController.CreateTableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                false,
                new Dictionary<string, string>
                {
                    { firstTableObjectFirstPropertyName, firstTableObjectFirstPropertyValue },
                    { firstTableObjectSecondPropertyName, firstTableObjectSecondPropertyValue }
                }
            );

            await TableObjectsController.CreateTableObject(
                secondTableObjectUuid,
                secondTableObjectTableId,
                false,
                new Dictionary<string, string>
                {
                    { secondTableObjectFirstPropertyName, secondTableObjectFirstPropertyValue },
                    { secondTableObjectSecondPropertyName, secondTableObjectSecondPropertyValue }
                }
            );

            var firstTableObjectFirstUpdatedPropertyValue = "First updated value";
            var firstTableObjectSecondUpdatedPropertyValue = "Erster aktualisierter Wert";

            var secondTableObjectFirstUpdatedPropertyValue = "Second updated value";
            var secondTableObjectSecondUpdatedPropertyValue = "Zweiter aktualisierter Wert";

            await davClassLibrary.Dav.Database.CreateTableObjectAsync(new TableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                new List<Property>
                {
                    new Property(firstTableObjectFirstPropertyName, firstTableObjectFirstUpdatedPropertyValue),
                    new Property(firstTableObjectSecondPropertyName, firstTableObjectSecondUpdatedPropertyValue)
                },
                TableObjectUploadStatus.Updated
            ));

            await davClassLibrary.Dav.Database.CreateTableObjectAsync(new TableObject(
                secondTableObjectUuid,
                secondTableObjectTableId,
                new List<Property>
                {
                    new Property(secondTableObjectFirstPropertyName, secondTableObjectFirstUpdatedPropertyValue),
                    new Property(secondTableObjectSecondPropertyName, secondTableObjectSecondUpdatedPropertyValue)
                },
                TableObjectUploadStatus.Updated
            ));

            // Act
            await davClassLibrary.DataAccess.SyncManager.SyncPush();

            // Assert
            var firstTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromDatabase.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, firstTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstUpdatedPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondUpdatedPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectSecondUpdatedPropertyValue));
            Assert.AreEqual(TableObjectUploadStatus.UpToDate, firstTableObjectFromDatabase.UploadStatus);

            var secondTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNotNull(secondTableObjectFromDatabase);
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromDatabase.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, secondTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(secondTableObjectFirstUpdatedPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondUpdatedPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectSecondUpdatedPropertyValue));
            Assert.AreEqual(TableObjectUploadStatus.UpToDate, secondTableObjectFromDatabase.UploadStatus);

            var firstTableObjectFromServerResponse = await TableObjectsController.GetTableObject(firstTableObjectUuid);
            Assert.True(firstTableObjectFromServerResponse.Success);

            var firstTableObjectFromServer = firstTableObjectFromServerResponse.Data;
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromServer.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromServer.TableId);
            Assert.AreEqual(2, firstTableObjectFromServer.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstUpdatedPropertyValue, firstTableObjectFromServer.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondUpdatedPropertyValue, firstTableObjectFromServer.GetPropertyValue(firstTableObjectSecondPropertyName));

            var secondTableObjectFromServerResponse = await TableObjectsController.GetTableObject(secondTableObjectUuid);
            Assert.True(secondTableObjectFromServerResponse.Success);

            var secondTableObjectFromServer = secondTableObjectFromServerResponse.Data;
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromServer.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromServer.TableId);
            Assert.AreEqual(2, secondTableObjectFromServer.TableId);
            Assert.AreEqual(secondTableObjectFirstUpdatedPropertyValue, secondTableObjectFromServer.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondUpdatedPropertyValue, secondTableObjectFromServer.GetPropertyValue(secondTableObjectSecondPropertyName));
        }

        public async Task SyncPushShouldDeleteTableObjectsOnTheServer()
        {
            // Arrange
            davClassLibrary.Dav.IsLoggedIn = true;
            davClassLibrary.Dav.AccessToken = Constants.testerXTestAppAccessToken;

            var firstTableObjectUuid = Guid.NewGuid();
            var firstTableObjectTableId = Constants.testAppFirstTestTableId;
            var firstTableObjectFirstPropertyName = "page1";
            var firstTableObjectFirstPropertyValue = "Hello World";
            var firstTableObjectSecondPropertyName = "page2";
            var firstTableObjectSecondPropertyValue = "Hallo Welt";

            var secondTableObjectUuid = Guid.NewGuid();
            var secondTableObjectTableId = Constants.testAppSecondTestTableId;
            var secondTableObjectFirstPropertyName = "test1";
            var secondTableObjectFirstPropertyValue = "First test";
            var secondTableObjectSecondPropertyName = "test2";
            var secondTableObjectSecondPropertyValue = "Second test";

            await TableObjectsController.CreateTableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                false,
                new Dictionary<string, string>
                {
                    { firstTableObjectFirstPropertyName, firstTableObjectFirstPropertyValue },
                    { firstTableObjectSecondPropertyName, firstTableObjectSecondPropertyValue }
                }
            );

            await TableObjectsController.CreateTableObject(
                secondTableObjectUuid,
                secondTableObjectTableId,
                false,
                new Dictionary<string, string>
                {
                    { secondTableObjectFirstPropertyName, secondTableObjectFirstPropertyValue },
                    { secondTableObjectSecondPropertyName, secondTableObjectSecondPropertyValue }
                }
            );

            await davClassLibrary.Dav.Database.CreateTableObjectAsync(new TableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                new List<Property>
                {
                    new Property(firstTableObjectFirstPropertyName, firstTableObjectFirstPropertyValue),
                    new Property(firstTableObjectSecondPropertyName, firstTableObjectSecondPropertyValue)
                },
                TableObjectUploadStatus.Deleted
            ));

            await davClassLibrary.Dav.Database.CreateTableObjectAsync(new TableObject(
                secondTableObjectUuid,
                secondTableObjectTableId,
                new List<Property>
                {
                    new Property(secondTableObjectFirstPropertyName, secondTableObjectFirstPropertyValue),
                    new Property(secondTableObjectSecondPropertyName, secondTableObjectSecondPropertyValue)
                },
                TableObjectUploadStatus.Deleted
            ));

            // Act
            await davClassLibrary.DataAccess.SyncManager.SyncPush();

            // Assert
            var firstTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNull(firstTableObjectFromDatabase);

            var secondTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNull(secondTableObjectFromDatabase);

            var firstTableObjectFromServerResponse = await TableObjectsController.GetTableObject(firstTableObjectUuid);
            Assert.False(firstTableObjectFromServerResponse.Success);
            Assert.AreEqual(404, firstTableObjectFromServerResponse.Status);
            Assert.AreEqual(ErrorCodes.TableObjectDoesNotExist, firstTableObjectFromServerResponse.Errors[0].Code);

            var secondTableObjectFromServerResponse = await TableObjectsController.GetTableObject(secondTableObjectUuid);
            Assert.False(secondTableObjectFromServerResponse.Success);
            Assert.AreEqual(404, secondTableObjectFromServerResponse.Status);
            Assert.AreEqual(ErrorCodes.TableObjectDoesNotExist, secondTableObjectFromServerResponse.Errors[0].Code);
        }

        public async Task SyncPushShouldDeleteUpdatedAndDeletedTableObjectsThatDoNotExistOnTheServer()
        {
            // Arrange
            davClassLibrary.Dav.IsLoggedIn = true;
            davClassLibrary.Dav.AccessToken = Constants.testerXTestAppAccessToken;

            var firstTableObjectUuid = Guid.NewGuid();
            var firstTableObjectTableId = Constants.testAppFirstTestTableId;
            var firstTableObjectFirstPropertyName = "page1";
            var firstTableObjectFirstPropertyValue = "Hello World";
            var firstTableObjectSecondPropertyName = "page2";
            var firstTableObjectSecondPropertyValue = "Hallo Welt";

            var secondTableObjectUuid = Guid.NewGuid();
            var secondTableObjectTableId = Constants.testAppSecondTestTableId;
            var secondTableObjectFirstPropertyName = "test1";
            var secondTableObjectFirstPropertyValue = "First test";
            var secondTableObjectSecondPropertyName = "test2";
            var secondTableObjectSecondPropertyValue = "Second test";

            await davClassLibrary.Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                new List<Property>
                {
                    new Property(firstTableObjectFirstPropertyName, firstTableObjectFirstPropertyValue),
                    new Property(firstTableObjectSecondPropertyName, firstTableObjectSecondPropertyValue)
                },
                TableObjectUploadStatus.Updated
            ));

            await davClassLibrary.Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
                secondTableObjectUuid,
                secondTableObjectTableId,
                new List<Property>
                {
                    new Property(secondTableObjectFirstPropertyName, secondTableObjectFirstPropertyValue),
                    new Property(secondTableObjectSecondPropertyName, secondTableObjectSecondPropertyValue)
                },
                TableObjectUploadStatus.Deleted
            ));

            // Act
            await davClassLibrary.DataAccess.SyncManager.SyncPush();

            // Assert
            var firstTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNull(firstTableObjectFromDatabase);

            var secondTableObjectFromDatabase = await davClassLibrary.Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNull(secondTableObjectFromDatabase);
        }
        #endregion
    }
}
