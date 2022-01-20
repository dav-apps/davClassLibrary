using davClassLibrary.Controllers;
using davClassLibrary.DataAccess;
using davClassLibrary.Models;
using NUnit.Framework;
using System;
using System.Collections.Generic;
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
            await Utils.Setup();
        }

        [TearDown]
        public async Task TearDown()
        {
            await DeleteTableObjectsOfTable(Constants.testAppFirstTestTableId);
            await DeleteTableObjectsOfTable(Constants.testAppSecondTestTableId);
        }

        private async Task DeleteTableObjectsOfTable(int tableId)
        {
            var getTableResponse = await TablesController.GetTable(tableId);
            if (!getTableResponse.Success) return;
  
            foreach(var tableObject in getTableResponse.Data.TableObjects)
                await TableObjectsController.DeleteTableObject(tableObject.Uuid);
        }
        #endregion

        #region SessionSyncPush
        [Test]
        public async Task SessionSyncPushShouldDeleteTheSessionOnTheServer()
        {
            var createSessionResponse = await SessionsController.CreateSession(
                Constants.davDevAuth,
                Constants.testerUserEmail,
                Constants.testerUserPassword,
                Constants.testAppId,
                Constants.testerDevApiKey
            );
            Assert.True(createSessionResponse.Success);
            string accessToken = createSessionResponse.Data.AccessToken;

            SettingsManager.SetAccessToken(accessToken);
            SettingsManager.SetSessionUploadStatus(SessionUploadStatus.Deleted);

            // Act
            await SyncManager.SessionSyncPush();

            // Assert
            var accessTokenFromDatabase = SettingsManager.GetAccessToken();
            Assert.IsNull(accessTokenFromDatabase);

            var sessionUploadStatusFromDatabase = SettingsManager.GetSessionUploadStatus();
            Assert.AreEqual(SessionUploadStatus.UpToDate, sessionUploadStatusFromDatabase);

            var deleteSessionResponse = await SessionsController.DeleteSession(accessToken);
            Assert.False(deleteSessionResponse.Success);
            Assert.AreEqual(404, deleteSessionResponse.Status);
            Assert.AreEqual(ErrorCodes.SessionDoesNotExist, deleteSessionResponse.Errors[0].Code);
        }

        [Test]
        public async Task SessionSyncPushShouldRemoveTheSessionFromTheDatabaseIfTheSessionDoesNotExistOnTheServer()
        {
            // Arrange
            SettingsManager.SetAccessToken("hiodahoisaoias");
            SettingsManager.SetSessionUploadStatus(SessionUploadStatus.Deleted);

            // Act
            await SyncManager.SessionSyncPush();

            // Assert
            var accessTokenFromDatabase = SettingsManager.GetAccessToken();
            Assert.IsNull(accessTokenFromDatabase);

            var sessionUploadStatusFromDatabase = SettingsManager.GetSessionUploadStatus();
            Assert.AreEqual(SessionUploadStatus.UpToDate, sessionUploadStatusFromDatabase);
        }
        #endregion

        #region LoadUser
        [Test]
        public void LoadUserShouldLoadTheUserDetailsFromTheLocalSettings()
        {
            // Arrange
            string email = "test@example.com";
            string firstName = "testUser";
            long totalStorage = 2309234234;
            long usedStorage = 23422;
            Plan plan = Plan.Plus;

            SettingsManager.SetEmail(email);
            SettingsManager.SetFirstName(firstName);
            SettingsManager.SetTotalStorage(totalStorage);
            SettingsManager.SetUsedStorage(usedStorage);
            SettingsManager.SetPlan(plan);

            // Act
            SyncManager.LoadUser();

            // Assert
            Assert.AreEqual(email, Dav.User.Email);
            Assert.AreEqual(firstName, Dav.User.FirstName);
            Assert.AreEqual(totalStorage, Dav.User.TotalStorage);
            Assert.AreEqual(usedStorage, Dav.User.UsedStorage);
            Assert.AreEqual(plan, Dav.User.Plan);
        }
        #endregion

        #region UserSync
        [Test]
        public async Task UserSyncShouldDownloadTheUserDetailsAndSaveThemInTheLocalSettings()
        {
            // Arrange
            Dav.IsLoggedIn = true;
            Dav.AccessToken = Constants.testerXTestAppAccessToken;

            // Act
            await SyncManager.UserSync();

            // Assert
            Assert.AreEqual(Constants.testerUserEmail, SettingsManager.GetEmail());
            Assert.AreEqual(Constants.testerUserFirstName, SettingsManager.GetFirstName());
            Assert.AreEqual(Constants.testerUserTotalStorage, SettingsManager.GetTotalStorage());
            Assert.AreEqual(Constants.testerUserUsedStorage, SettingsManager.GetUsedStorage());
            Assert.AreEqual(Constants.testerUserPlan, SettingsManager.GetPlan());

            Assert.AreEqual(Constants.testerUserEmail, Dav.User.Email);
            Assert.AreEqual(Constants.testerUserFirstName, Dav.User.FirstName);
            Assert.AreEqual(Constants.testerUserTotalStorage, Dav.User.TotalStorage);
            Assert.AreEqual(Constants.testerUserUsedStorage, Dav.User.UsedStorage);
            Assert.AreEqual(Constants.testerUserPlan, Dav.User.Plan);
        }

        [Test]
        public async Task UserSyncShouldDownloadTheUserDetailsAndUpdateTheExistingUserDetailsInTheLocalSettings()
        {
            // Arrange
            Dav.IsLoggedIn = true;
            Dav.AccessToken = Constants.testerXTestAppAccessToken;

            SettingsManager.SetEmail("test@example.com");
            SettingsManager.SetFirstName("testUser");
            SettingsManager.SetTotalStorage(2309234234);
            SettingsManager.SetUsedStorage(23422);
            SettingsManager.SetPlan(Plan.Plus);
            SyncManager.LoadUser();

            // Act
            await SyncManager.UserSync();

            // Assert
            Assert.AreEqual(Constants.testerUserEmail, SettingsManager.GetEmail());
            Assert.AreEqual(Constants.testerUserFirstName, SettingsManager.GetFirstName());
            Assert.AreEqual(Constants.testerUserTotalStorage, SettingsManager.GetTotalStorage());
            Assert.AreEqual(Constants.testerUserUsedStorage, SettingsManager.GetUsedStorage());
            Assert.AreEqual(Constants.testerUserPlan, SettingsManager.GetPlan());

            Assert.AreEqual(Constants.testerUserEmail, Dav.User.Email);
            Assert.AreEqual(Constants.testerUserFirstName, Dav.User.FirstName);
            Assert.AreEqual(Constants.testerUserTotalStorage, Dav.User.TotalStorage);
            Assert.AreEqual(Constants.testerUserUsedStorage, Dav.User.UsedStorage);
            Assert.AreEqual(Constants.testerUserPlan, Dav.User.Plan);
        }

        [Test]
        public async Task UserSyncShouldLogTheUserOutIfTheSessionDoesNotExist()
        {
            // Arrange
            Dav.IsLoggedIn = true;
            Dav.AccessToken = "siodfhiosdghiosgd";

            SettingsManager.SetEmail("test@example.com");
            SettingsManager.SetFirstName("testUser");
            SettingsManager.SetTotalStorage(2309234234);
            SettingsManager.SetUsedStorage(23422);
            SettingsManager.SetPlan(Plan.Plus);
            SyncManager.LoadUser();

            // Act
            await SyncManager.UserSync();

            // Assert
            Assert.False(Dav.IsLoggedIn);
            Assert.IsNull(Dav.AccessToken);

            Assert.IsNull(SettingsManager.GetEmail());
            Assert.IsNull(SettingsManager.GetFirstName());
            Assert.AreEqual(0, SettingsManager.GetTotalStorage());
            Assert.AreEqual(0, SettingsManager.GetUsedStorage());
            Assert.AreEqual(Plan.Free, SettingsManager.GetPlan());
        }
        #endregion

        #region Sync
        [Test]
        public async Task SyncShouldDownloadAllTableObjectsFromTheServerAndUpdateThePropertiesOfExistingTableObjects()
        {
            // Arrange
            Dav.IsLoggedIn = true;
            Dav.AccessToken = Constants.testerXTestAppAccessToken;

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
            await SyncManager.Sync();

            // Assert (1)
            var allTableObjects = await Dav.Database.GetAllTableObjectsAsync(false);
            Assert.AreEqual(2, allTableObjects.Count);

            var firstTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromDatabase.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, firstTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectSecondPropertyName));

            var secondTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
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
            await SyncManager.Sync();

            // Assert (2)
            allTableObjects = await Dav.Database.GetAllTableObjectsAsync(false);
            Assert.AreEqual(2, allTableObjects.Count);

            firstTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromDatabase.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, firstTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstUpdatedPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondUpdatedPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectSecondPropertyName));

            secondTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNotNull(secondTableObjectFromDatabase);
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromDatabase.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, secondTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(secondTableObjectFirstUpdatedPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondUpdatedPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectSecondPropertyName));
        }

        [Test]
        public async Task SyncShouldRemoveTheTableObjectsThatAreNotOnTheServer()
        {
            // Arrange
            Dav.IsLoggedIn = true;
            Dav.AccessToken = Constants.testerXTestAppAccessToken;

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

            await Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
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
            await SyncManager.Sync();

            // Assert
            var allTableObjects = await Dav.Database.GetAllTableObjectsAsync(false);
            Assert.AreEqual(2, allTableObjects.Count);

            var firstTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromDatabase.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, firstTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectSecondPropertyName));

            var secondTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNotNull(secondTableObjectFromDatabase);
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromDatabase.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, secondTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(secondTableObjectFirstPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectSecondPropertyName));
        }
        #endregion

        #region SyncPush
        [Test]
        public async Task SyncPushShouldCreateTableObjectsOnTheServer()
        {
            // Arrange
            Dav.IsLoggedIn = true;
            Dav.AccessToken = Constants.testerXTestAppAccessToken;

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

            await Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                new List<Property>
                {
                    new Property(firstTableObjectFirstPropertyName, firstTableObjectFirstPropertyValue),
                    new Property(firstTableObjectSecondPropertyName, firstTableObjectSecondPropertyValue)
                },
                TableObjectUploadStatus.New
            ));

            await Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
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
            await SyncManager.SyncPush();

            // Assert
            var firstTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.AreEqual(TableObjectUploadStatus.UpToDate, firstTableObjectFromDatabase.UploadStatus);

            var secondTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNotNull(secondTableObjectFromDatabase);
            Assert.AreEqual(TableObjectUploadStatus.UpToDate, secondTableObjectFromDatabase.UploadStatus);

            var firstTableObjectFromServerResponse = await TableObjectsController.GetTableObject(firstTableObjectUuid);
            Assert.True(firstTableObjectFromServerResponse.Success);

            var firstTableObjectFromServer = firstTableObjectFromServerResponse.Data.TableObject;
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromServer.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromServer.TableId);
            Assert.AreEqual(2, firstTableObjectFromServer.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstPropertyValue, firstTableObjectFromServer.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondPropertyValue, firstTableObjectFromServer.GetPropertyValue(firstTableObjectSecondPropertyName));

            var secondTableObjectFromServerResponse = await TableObjectsController.GetTableObject(secondTableObjectUuid);
            Assert.True(secondTableObjectFromServerResponse.Success);

            var secondTableObjectFromServer = secondTableObjectFromServerResponse.Data.TableObject;
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromServer.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromServer.TableId);
            Assert.AreEqual(2, secondTableObjectFromServer.Properties.Count);
            Assert.AreEqual(secondTableObjectFirstPropertyValue, secondTableObjectFromServer.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondPropertyValue, secondTableObjectFromServer.GetPropertyValue(secondTableObjectSecondPropertyName));
        }

        [Test]
        public async Task SyncPushShouldUpdateTableObjectsOnTheServer()
        {
            // Arrange
            Dav.IsLoggedIn = true;
            Dav.AccessToken = Constants.testerXTestAppAccessToken;

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

            await Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                new List<Property>
                {
                    new Property(firstTableObjectFirstPropertyName, firstTableObjectFirstUpdatedPropertyValue),
                    new Property(firstTableObjectSecondPropertyName, firstTableObjectSecondUpdatedPropertyValue)
                },
                TableObjectUploadStatus.Updated
            ));

            await Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
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
            await SyncManager.SyncPush();

            // Assert
            var firstTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNotNull(firstTableObjectFromDatabase);
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromDatabase.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, firstTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstUpdatedPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondUpdatedPropertyValue, firstTableObjectFromDatabase.GetPropertyValue(firstTableObjectSecondPropertyName));
            Assert.AreEqual(TableObjectUploadStatus.UpToDate, firstTableObjectFromDatabase.UploadStatus);

            var secondTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNotNull(secondTableObjectFromDatabase);
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromDatabase.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromDatabase.TableId);
            Assert.AreEqual(2, secondTableObjectFromDatabase.Properties.Count);
            Assert.AreEqual(secondTableObjectFirstUpdatedPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondUpdatedPropertyValue, secondTableObjectFromDatabase.GetPropertyValue(secondTableObjectSecondPropertyName));
            Assert.AreEqual(TableObjectUploadStatus.UpToDate, secondTableObjectFromDatabase.UploadStatus);

            var firstTableObjectFromServerResponse = await TableObjectsController.GetTableObject(firstTableObjectUuid);
            Assert.True(firstTableObjectFromServerResponse.Success);

            var firstTableObjectFromServer = firstTableObjectFromServerResponse.Data.TableObject;
            Assert.AreEqual(firstTableObjectUuid, firstTableObjectFromServer.Uuid);
            Assert.AreEqual(firstTableObjectTableId, firstTableObjectFromServer.TableId);
            Assert.AreEqual(2, firstTableObjectFromServer.Properties.Count);
            Assert.AreEqual(firstTableObjectFirstUpdatedPropertyValue, firstTableObjectFromServer.GetPropertyValue(firstTableObjectFirstPropertyName));
            Assert.AreEqual(firstTableObjectSecondUpdatedPropertyValue, firstTableObjectFromServer.GetPropertyValue(firstTableObjectSecondPropertyName));

            var secondTableObjectFromServerResponse = await TableObjectsController.GetTableObject(secondTableObjectUuid);
            Assert.True(secondTableObjectFromServerResponse.Success);

            var secondTableObjectFromServer = secondTableObjectFromServerResponse.Data.TableObject;
            Assert.AreEqual(secondTableObjectUuid, secondTableObjectFromServer.Uuid);
            Assert.AreEqual(secondTableObjectTableId, secondTableObjectFromServer.TableId);
            Assert.AreEqual(2, secondTableObjectFromServer.Properties.Count);
            Assert.AreEqual(secondTableObjectFirstUpdatedPropertyValue, secondTableObjectFromServer.GetPropertyValue(secondTableObjectFirstPropertyName));
            Assert.AreEqual(secondTableObjectSecondUpdatedPropertyValue, secondTableObjectFromServer.GetPropertyValue(secondTableObjectSecondPropertyName));
        }

        [Test]
        public async Task SyncPushShouldDeleteTableObjectsOnTheServer()
        {
            // Arrange
            Dav.IsLoggedIn = true;
            Dav.AccessToken = Constants.testerXTestAppAccessToken;

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

            await Dav.Database.CreateTableObjectAsync(new TableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                new List<Property>
                {
                    new Property(firstTableObjectFirstPropertyName, firstTableObjectFirstPropertyValue),
                    new Property(firstTableObjectSecondPropertyName, firstTableObjectSecondPropertyValue)
                },
                TableObjectUploadStatus.Deleted
            ));

            await Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
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
            await SyncManager.SyncPush();

            // Assert
            var firstTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNull(firstTableObjectFromDatabase);

            var secondTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
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

        [Test]
        public async Task SyncPushShouldDeleteUpdatedAndDeletedTableObjectsThatDoNotExistOnTheServer()
        {
            // Arrange
            Dav.IsLoggedIn = true;
            Dav.AccessToken = Constants.testerXTestAppAccessToken;

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

            await Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
                firstTableObjectUuid,
                firstTableObjectTableId,
                new List<Property>
                {
                    new Property(firstTableObjectFirstPropertyName, firstTableObjectFirstPropertyValue),
                    new Property(firstTableObjectSecondPropertyName, firstTableObjectSecondPropertyValue)
                },
                TableObjectUploadStatus.Updated
            ));

            await Dav.Database.CreateTableObjectWithPropertiesAsync(new TableObject(
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
            await SyncManager.SyncPush();

            // Assert
            var firstTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(firstTableObjectUuid);
            Assert.IsNull(firstTableObjectFromDatabase);

            var secondTableObjectFromDatabase = await Dav.Database.GetTableObjectAsync(secondTableObjectUuid);
            Assert.IsNull(secondTableObjectFromDatabase);
        }
        #endregion
    }
}
