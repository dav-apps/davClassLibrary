using System;
using System.Collections.Generic;
using System.Text;

namespace davClassLibrary.Common
{
    public interface ILocalDataSettings
    {
        void SaveValue(string key, string value);
        string GetValue(string key);
    }
}
