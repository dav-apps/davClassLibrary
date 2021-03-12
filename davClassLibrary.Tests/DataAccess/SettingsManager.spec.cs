using davClassLibrary.DataAccess;
using NUnit.Framework;
using System.Threading.Tasks;

namespace davClassLibrary.Tests.DataAccess
{
    [TestFixture][SingleThreaded]
    public class SettingsManagerTest
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

        #region SetAccessToken & GetAccessToken
        [Test]
        public void ShouldSetAndGetAccessToken()
        {
            // Arrange
            string accessToken = "spgjodpsjodpjosgd";

            // Act
            SettingsManager.SetAccessToken(accessToken);
            string accessTokenFromSettings = SettingsManager.GetAccessToken();

            // Assert
            Assert.AreEqual(accessToken, accessTokenFromSettings);
        }
        #endregion

        #region SetSessionUploadStatus & GetSessionUploadStatus
        [Test]
        public void ShouldSetAndGetSessionUploadStatus()
        {
            // Arrange
            var sessionUploadStatus = SessionUploadStatus.Deleted;

            // Act
            SettingsManager.SetSessionUploadStatus(sessionUploadStatus);
            var sessionUploadStatusFromSettings = SettingsManager.GetSessionUploadStatus();

            // Assert
            Assert.AreEqual(sessionUploadStatus, sessionUploadStatusFromSettings);
        }
        #endregion

        #region SetEmail & GetEmail
        [Test]
        public void ShouldSetAndGetEmail()
        {
            // Arrange
            string email = "test@dav-apps.tech";

            // Act
            SettingsManager.SetEmail(email);
            string emailFromSettings = SettingsManager.GetEmail();

            // Assert
            Assert.AreEqual(email, emailFromSettings);
        }
        #endregion

        #region SetFirstName & GetFirstName
        [Test]
        public void ShouldSetAndGetFirstName()
        {
            // Arrange
            string firstName = "testUser";

            // Act
            SettingsManager.SetFirstName(firstName);
            string firstNameFromSettings = SettingsManager.GetFirstName();

            // Assert
            Assert.AreEqual(firstName, firstNameFromSettings);
        }
        #endregion

        #region SetTotalStorage & GetTotalStorage
        [Test]
        public void ShouldSetAndGetTotalStorage()
        {
            // Arrange
            long totalStorage = 29342943;

            // Act
            SettingsManager.SetTotalStorage(totalStorage);
            long totalStorageFromSettings = SettingsManager.GetTotalStorage();

            // Assert
            Assert.AreEqual(totalStorage, totalStorageFromSettings);
        }
        #endregion

        #region SetUsedStorage & GetUsedStorage
        [Test]
        public void ShouldSetAndGetUsedStorage()
        {
            // Arrange
            long usedStorage = 982598253;

            // Act
            SettingsManager.SetUsedStorage(usedStorage);
            long usedStorageFromSettings = SettingsManager.GetUsedStorage();

            // Assert
            Assert.AreEqual(usedStorage, usedStorageFromSettings);
        }
        #endregion

        #region SetPlan & GetPlan
        [Test]
        public void ShouldSetAndGetPlan()
        {
            // Arrange
            Plan plan = Plan.Plus;

            // Act
            SettingsManager.SetPlan(plan);
            Plan planFromSettings = SettingsManager.GetPlan();

            // Assert
            Assert.AreEqual(plan, planFromSettings);
        }
        #endregion

        #region SetProfileImageEtag & GetProfileImageEtag
        [Test]
        public void ShouldSetAndGetProfileImageEtag()
        {
            // Arrange
            string profileImageEtag = "jiosdosgdsghod";

            // Act
            SettingsManager.SetProfileImageEtag(profileImageEtag);
            string profileImageEtagFromSettings = SettingsManager.GetProfileImageEtag();

            // Assert
            Assert.AreEqual(profileImageEtag, profileImageEtagFromSettings);
        }
        #endregion

        #region RemoveSession
        [Test]
        public void RemoveSessionShouldRemoveAllSessionSettings()
        {
            // Arrange
            string accessToken = "sfiodshiodg";
            var sessionUploadStatus = SessionUploadStatus.Deleted;
            string email = "test@example.com";
            string firstName = "firstname";
            long totalStorage = 234235253;
            long usedStorage = 45454;
            var plan = Plan.Pro;

            SettingsManager.SetAccessToken(accessToken);
            SettingsManager.SetSessionUploadStatus(sessionUploadStatus);
            SettingsManager.SetEmail(email);
            SettingsManager.SetFirstName(firstName);
            SettingsManager.SetTotalStorage(totalStorage);
            SettingsManager.SetUsedStorage(usedStorage);
            SettingsManager.SetPlan(plan);

            // Act
            SettingsManager.RemoveSession();

            // Assert
            Assert.IsNull(SettingsManager.GetAccessToken());
            Assert.AreEqual(SessionUploadStatus.UpToDate, SettingsManager.GetSessionUploadStatus());
            Assert.AreEqual(email, SettingsManager.GetEmail());
            Assert.AreEqual(firstName, SettingsManager.GetFirstName());
            Assert.AreEqual(totalStorage, SettingsManager.GetTotalStorage());
            Assert.AreEqual(usedStorage, SettingsManager.GetUsedStorage());
            Assert.AreEqual(plan, SettingsManager.GetPlan());
        }
        #endregion

        #region RemoveUser
        [Test]
        public void RemoveUserShouldRemoveAllUserSettings()
        {
            // Arrange
            string accessToken = "sfiodshiodg";
            var sessionUploadStatus = SessionUploadStatus.Deleted;
            string email = "test@example.com";
            string firstName = "firstname";
            long totalStorage = 234235253;
            long usedStorage = 45454;
            var plan = Plan.Pro;

            SettingsManager.SetAccessToken(accessToken);
            SettingsManager.SetSessionUploadStatus(sessionUploadStatus);
            SettingsManager.SetEmail(email);
            SettingsManager.SetFirstName(firstName);
            SettingsManager.SetTotalStorage(totalStorage);
            SettingsManager.SetUsedStorage(usedStorage);
            SettingsManager.SetPlan(plan);

            // Act
            SettingsManager.RemoveUser();

            // Assert
            Assert.AreEqual(accessToken, SettingsManager.GetAccessToken());
            Assert.AreEqual(sessionUploadStatus, SettingsManager.GetSessionUploadStatus());
            Assert.IsNull(SettingsManager.GetEmail());
            Assert.IsNull(SettingsManager.GetFirstName());
            Assert.AreEqual(0, SettingsManager.GetTotalStorage());
            Assert.AreEqual(0, SettingsManager.GetUsedStorage());
            Assert.AreEqual(Plan.Free, SettingsManager.GetPlan());
        }
        #endregion
    }
}
