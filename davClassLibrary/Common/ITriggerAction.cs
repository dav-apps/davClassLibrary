using System;

namespace davClassLibrary.Common
{
    public interface ITriggerAction
    {
        void UpdateAll();
        void UpdateAllOfTable(int tableId);
        void UpdateTableObject(Guid uuid);
    }
}
