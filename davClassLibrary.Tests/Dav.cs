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
        public const string Jwt = "eyJhbGciOiJIUzI1NiJ9.eyJlbWFpbCI6ImRhdmNsYXNzbGlicmFyeXRlc3RAZGF2LWFwcHMudGVjaCIsInVzZXJuYW1lIjoiZGF2Q2xhc3NMaWJyYXJ5VGVzdFVzZXIiLCJ1c2VyX2lkIjoxMiwiZGV2X2lkIjoyLCJleHAiOjM3NTI5MTU4NDQ5fQ.ZEy4Ul_D9iNrzaoIJguXtX_EFDzcvejn3Z6EuNtuNCE";
        public const string TestUserEmail = "davclasslibrarytest@dav-apps.tech";
        public const string TestUserUsername = "davClassLibraryTestUser";
        public const DavPlan TestUserPlan = DavPlan.Free;

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
