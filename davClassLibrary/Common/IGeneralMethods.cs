using davClassLibrary.DataAccess;

namespace davClassLibrary.Common
{
    public interface IGeneralMethods
    {
        bool IsNetworkAvailable();
        DavEnvironment GetEnvironment();
    }
}
