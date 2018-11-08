using davClassLibrary.Common;
using davClassLibrary.DataAccess;

namespace davClassLibrary.Tests.Common
{
    public class GeneralMethods : IGeneralMethods
    {
        public bool IsNetworkAvailable()
        {
            return true;
        }

        public Environment GetEnvironment()
        {
            return Environment.Test;
        }
    }
}
