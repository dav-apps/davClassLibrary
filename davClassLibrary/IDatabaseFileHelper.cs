using System;
using System.Collections.Generic;
using System.Text;

namespace davClassLibrary
{
    public interface IDatabaseFileHelper
    {
        string GetLocalFilePath(string filename);
    }
}
