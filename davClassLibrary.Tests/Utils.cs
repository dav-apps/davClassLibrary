using davClassLibrary.Common;
using davClassLibrary.Tests.Common;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace davClassLibrary.Tests
{
    internal static class Utils
    {
        internal static void GlobalSetup()
        {
            ProjectInterface.LocalDataSettings = new LocalDataSettings();
            ProjectInterface.Callbacks = new Callbacks();

            Dav.Init(
                Environment.Test,
                Constants.testAppId,
                new List<int> { Constants.testAppFirstTestTableId, Constants.testAppSecondTestTableId },
                new List<int>(),
                GetDavDataPath()
            );
        }

        internal static async Task Setup()
        {
            Dav.IsLoggedIn = false;
            Dav.AccessToken = null;

            // Delete all files and folders in the test folder except the database file
            var davFolder = new DirectoryInfo(GetDavDataPath());
            foreach (var folder in davFolder.GetDirectories())
                folder.Delete(true);

            // Clear the database
            var database = new davClassLibrary.DataAccess.DavDatabase();
            await database.DropAsync();
        }

        internal static string GetDavDataPath()
        {
            return Directory.CreateDirectory(Path.Combine(Directory.GetCurrentDirectory(), "../../dav")).FullName;
        }

        internal static string GetProjectDirectory()
        {
            return Path.Combine(Directory.GetCurrentDirectory(), "../../../");
        }

        public static void ClearData()
        {
            DirectoryInfo directory = new DirectoryInfo(GetDavDataPath());
            foreach (var dir in directory.GetDirectories())
            {
                dir.Delete(true);
            }

            foreach (var file in directory.GetFiles())
            {
                file.Delete();
            }
        }
    }
}
