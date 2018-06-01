using System;

namespace davClassLibrary.Common
{
    public interface ITriggerAction
    {
        void UpdateAll();
        void UpdateTableObject(Guid uuid);
    }
}
