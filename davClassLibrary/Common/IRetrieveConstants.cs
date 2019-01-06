using System.Collections.Generic;

namespace davClassLibrary.Common
{
    public interface IRetrieveConstants
    {
        string GetDataPath();
        string GetApiKey();
        int GetAppId();
        List<int> GetTableIds();
        List<int> GetParallelTableIds();
    }
}
