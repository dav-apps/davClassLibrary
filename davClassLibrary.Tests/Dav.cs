using davClassLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using static davClassLibrary.Models.DavUser;

namespace davClassLibrary.Tests
{
    public class Dav
    {
        public const string ProjectDirectory = @"C:\Users\dav20\source\repos\davClassLibrary\davClassLibrary.Tests";
        public const int AppId = 3;
        public const int TestDataTableId = 3;
        public const int TestFileTableId = 4;
        public const string ApiKey = "MhKSDyedSw8WXfLk2hkXzmElsiVStD7C8JU3KNGp";
        public const string Jwt = "eyJhbGciOiJIUzI1NiJ9.eyJlbWFpbCI6ImRhdmNsYXNzbGlicmFyeXRlc3RAZGF2LWFwcHMudGVjaCIsInVzZXJfaWQiOjUsImRldl9pZCI6MiwiZXhwIjozNzU2MTA1MDAyMn0.jZpdLre_ZMWGN2VNbZOn2Xg51RLAT6ocGnyM38jljHI.1";
        public const string TestUserEmail = "davclasslibrarytest@dav-apps.tech";
        public const string TestUserUsername = "davClassLibraryTestUser";
        public const DavPlan TestUserPlan = DavPlan.Free;
        public const string TestDataFirstPropertyName = "page1";
        public const string TestDataSecondPropertyName = "page2";
        public const string TestDataFirstTableObjectFirstPropertyValue = "Hello World";
        public const string TestDataFirstTableObjectSecondPropertyValue = "Hallo Welt";
        public const string TestDataSecondTableObjectFirstPropertyValue = "Table";
        public const string TestDataSecondTableObjectSecondPropertyValue = "Tabelle";
        public static List<int> TableIds = new List<int>{ TestDataTableId, TestFileTableId };

        public static TableObjectData TestDataFirstTableObject = new TableObjectData {
            table_id = TestDataTableId,
            uuid = new Guid("642e6407-f357-4e03-b9c2-82f754931161"),
            properties = new Dictionary<string, string>
            {
                { TestDataFirstPropertyName, TestDataFirstTableObjectFirstPropertyValue },
                { TestDataSecondPropertyName, TestDataFirstTableObjectSecondPropertyValue }
            }
        };
        public static TableObjectData TestDataSecondTableObject = new TableObjectData
        {
            table_id = TestDataTableId,
            uuid = new Guid("8d29f002-9511-407b-8289-5ebdcb5a5559"),
            properties = new Dictionary<string, string>
            {
                { TestDataFirstPropertyName, TestDataSecondTableObjectFirstPropertyValue },
                { TestDataSecondPropertyName, TestDataSecondTableObjectSecondPropertyValue }
            }
        };

        public static string GetDavDataPath()
        {
            var directory = Directory.CreateDirectory(Path.Combine(ProjectDirectory, "bin", "dav"));
            return directory.FullName;
        }

        public static void ClearData()
        {
            DirectoryInfo directory = new DirectoryInfo(GetDavDataPath());
            foreach(var dir in directory.GetDirectories())
            {
                dir.Delete(true);
            }

            foreach(var file in directory.GetFiles())
            {
                file.Delete();
            }
        }
    }
}
