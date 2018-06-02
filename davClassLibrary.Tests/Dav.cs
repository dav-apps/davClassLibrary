using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace davClassLibrary.Tests
{
    public class Dav
    {
        public const string ProjectDirectory = @"C:\Users\dav20\source\repos\davClassLibrary\davClassLibrary.Tests";

        public static string GetDavDataPath()
        {
            var directory = Directory.CreateDirectory(Path.Combine(ProjectDirectory, "bin", "dav"));
            return directory.FullName;
        }
    }
}
