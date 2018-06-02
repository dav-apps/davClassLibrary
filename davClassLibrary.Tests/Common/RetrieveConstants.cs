using davClassLibrary.Common;
using davClassLibrary.Models;
using System.IO;
using System.Reflection;

namespace davClassLibrary.Tests.Common
{
    public class RetrieveConstants : IRetrieveConstants
    {
        public string GetApiKey()
        {
            throw new System.NotImplementedException();
        }

        public int GetAppId()
        {
            throw new System.NotImplementedException();
        }

        public string GetDataPath()
        {
            return Dav.GetDavDataPath();
        }
    }
}
