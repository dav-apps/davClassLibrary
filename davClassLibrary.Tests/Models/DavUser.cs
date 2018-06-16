using davClassLibrary.Common;
using davClassLibrary.Tests.Common;
using NUnit.Framework;
using System.Threading.Tasks;

namespace davClassLibrary.Tests.Models
{
    [TestFixture][SingleThreaded]
    public class DavUser
    {
        #region Setup
        [SetUp]
        public void Setup()
        {

        }

        [OneTimeSetUp]
        public void GlobalSetup()
        {
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
            await user.Login(Dav.Jwt);

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
            await user.Login(Dav.Jwt + "asdasdsad");

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
            await user.Login(Dav.Jwt);

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

        #region ParseIntToDavPlan
        [Test]
        public void ParseIntToDavPlanShouldParse0ToFree()
        {
            // Arrange
            int planInt = 0;

            // Act
            var plan = davClassLibrary.Models.DavUser.ParseIntToDavPlan(planInt);

            // Assert
            Assert.AreEqual(davClassLibrary.Models.DavUser.DavPlan.Free, plan);
        }

        [Test]
        public void ParseIntToDavPlanShouldParse1ToPlus()
        {
            // Arrange
            int planInt = 1;

            // Act
            var plan = davClassLibrary.Models.DavUser.ParseIntToDavPlan(planInt);

            // Assert
            Assert.AreEqual(davClassLibrary.Models.DavUser.DavPlan.Plus, plan);
        }
        #endregion

        #region ParseDavPlanToInt
        [Test]
        public void ParseDavPlanToIntShouldParseFreeTo1()
        {
            // Arrange
            var plan = davClassLibrary.Models.DavUser.DavPlan.Free;

            // Act
            int planInt = davClassLibrary.Models.DavUser.ParseDavPlanToInt(plan);

            // Assert
            Assert.AreEqual(0, planInt);
        }

        [Test]
        public void ParseDavPlanToIntShouldParsePlusTo0()
        {
            // Arrange
            var plan = davClassLibrary.Models.DavUser.DavPlan.Plus;

            // Act
            int planInt = davClassLibrary.Models.DavUser.ParseDavPlanToInt(plan);

            // Assert
            Assert.AreEqual(1, planInt);
        }
        #endregion
    }
}
