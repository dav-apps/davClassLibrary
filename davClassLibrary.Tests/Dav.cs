using davClassLibrary.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using static davClassLibrary.Models.DavUser;

namespace davClassLibrary.Tests
{
    public class Dav
    {
        public const string ProjectDirectory = @"C:\Users\dav20\source\repos\davClassLibrary\davClassLibrary.Tests";
        public const int AppId = 12;
        public const string ApiKey = "MhKSDyedSw8WXfLk2hkXzmElsiVStD7C8JU3KNGp";
        public const string Jwt = "eyJhbGciOiJIUzI1NiJ9.eyJlbWFpbCI6ImRhdmNsYXNzbGlicmFyeXRlc3RAZGF2LWFwcHMudGVjaCIsInVzZXJuYW1lIjoiZGF2Q2xhc3NMaWJyYXJ5VGVzdFVzZXIiLCJ1c2VyX2lkIjoxMiwiZGV2X2lkIjoyLCJleHAiOjM3NTI5MTgzODQxfQ.lO-iq5UHk25wnysbrEw1PirgGBhz-rFqrK6iRGkcFnU";
        public const string TestUserEmail = "davclasslibrarytest@dav-apps.tech";
        public const string TestUserUsername = "davClassLibraryTestUser";
        public const DavPlan TestUserPlan = DavPlan.Free;
        public const string TestDataFirstPropertyName = "page1";
        public const string TestDataSecondPropertyName = "page2";
        public const string TestDataFirstTableObjectFirstPropertyValue = "Hello World";
        public const string TestDataFirstTableObjectSecondPropertyValue = "Hallo Welt";
        public const string TestDataSecondTableObjectFirstPropertyValue = "Table";
        public const string TestDataSecondTableObjectSecondPropertyValue = "Tabelle";
        public static List<int> TableIds = new List<int>{ 23, 24 };

        public static TableObjectData TestDataFirstTableObject = new TableObjectData {
            table_id = 23,
            uuid = new Guid("642e6407-f357-4e03-b9c2-82f754931161"),
            properties = new Dictionary<string, string>
            {
                { TestDataFirstPropertyName, TestDataFirstTableObjectFirstPropertyValue },
                { TestDataSecondPropertyName, TestDataFirstTableObjectSecondPropertyValue }
            }
        };
        public static TableObjectData TestDataSecondTableObject = new TableObjectData
        {
            table_id = 23,
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
