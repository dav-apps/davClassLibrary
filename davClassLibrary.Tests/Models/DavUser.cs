using davClassLibrary.Common;
using davClassLibrary.Tests.Common;
using NUnit.Framework;
using System.IO;
using System.Threading.Tasks;

namespace davClassLibrary.Tests.Models
{
    [TestFixture][SingleThreaded]
    public class DavUser
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

        #region Constructors
        [Test]
        public void ConstructorShouldCreateNewDavUserWithNotLoggedInUserWhenNoJWTIsSaved()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, "");

            // Act
            var user = new davClassLibrary.Models.DavUser();

            // Assert
            Assert.IsFalse(user.IsLoggedIn);
        }

        [Test]
        public void ConstructorShouldCreateNewDavUserWithLoggedInUserWhenJWTIsSaved()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, Dav.Jwt);

            // Act
            var user = new davClassLibrary.Models.DavUser();

            // Assert
            Assert.IsTrue(user.IsLoggedIn);
            Assert.AreEqual(Dav.Jwt, user.JWT);
        }
        #endregion

        #region Login
        [Test]
        public async Task LoginWithValidJwtShouldLogTheUserInAndDownloadTheUserInformation()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, null);
            var user = new davClassLibrary.Models.DavUser();

            // Act
            await user.LoginAsync(Dav.Jwt);

            // Assert
            Assert.IsTrue(user.IsLoggedIn);
            Assert.AreEqual(Dav.Jwt, user.JWT);
            Assert.AreEqual(Dav.TestUserEmail, user.Email);
            Assert.AreEqual(Dav.TestUserUsername, user.Username);
            Assert.AreEqual(Dav.TestUserPlan, user.Plan);
            Assert.IsNotNull(user.Avatar);

            // Check if the avatar was downloaded
            FileAssert.Exists(user.Avatar.FullName);
        }

        [Test]
        public async Task LoginWithInvalidJwtShouldNotLogTheUserIn()
        {
            // Arrange
            ProjectInterface.LocalDataSettings.SetValue(davClassLibrary.Dav.jwtKey, null);
            var user = new davClassLibrary.Models.DavUser();

            // Act
            await user.LoginAsync("asdasdasdasd.asdasd.asdasdasdsad");

            // Assert
            Assert.IsFalse(user.IsLoggedIn);
            Assert.IsNull(user.JWT);
        }
        #endregion

        #region Logout
        [Test]
        public async Task LogoutShouldRemoveAllUserDataAndDeleteTheAvatar()
        {
            // Arrange
            var user = new davClassLibrary.Models.DavUser();
            await user.LoginAsync(Dav.Jwt);

            Assert.IsTrue(user.IsLoggedIn);
            Assert.AreEqual(Dav.TestUserEmail, user.Email);
            Assert.AreEqual(Dav.TestUserUsername, user.Username);
            Assert.AreEqual(Dav.TestUserPlan, user.Plan);
            Assert.IsNotNull(user.Avatar);
            var avatar = user.Avatar;

            // Act
            user.Logout();

            // Assert
            Assert.IsFalse(user.IsLoggedIn);
            Assert.IsNull(user.Email);
            Assert.IsNull(user.Username);
            Assert.IsNull(user.JWT);
            FileAssert.DoesNotExist(avatar);
        }
        #endregion
    }
}
