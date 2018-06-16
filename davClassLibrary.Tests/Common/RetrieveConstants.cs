using davClassLibrary.Common;
using System.Collections.Generic;

namespace davClassLibrary.Tests.Common
{
    public class RetrieveConstants : IRetrieveConstants
    {
        public string GetApiKey()
        {
            return Dav.ApiKey;
        }

        public int GetAppId()
        {
            return Dav.AppId;
        }

        public string GetDataPath()
        {
            return Dav.GetDavDataPath();
        }

        public List<int> GetTableIds()
        {
            return Dav.TableIds;
        }
    }
}
