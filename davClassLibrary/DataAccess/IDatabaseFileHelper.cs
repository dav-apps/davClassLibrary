using System;
using System.Collections.Generic;
using System.Text;

namespace davClassLibrary.DataAccess
{
    public interface IDatabaseFileHelper
    {
        string GetLocalFilePath(string filename);
    }
}
