using System.Threading.Tasks;

namespace davClassLibrary.Common
{
    public interface IRetrieveConstants
    {
        string GetDataPath();
        string GetApiKey();
        int GetAppId();
    }
}
