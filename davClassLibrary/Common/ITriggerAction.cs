using davClassLibrary.Models;

namespace davClassLibrary.Common
{
    public interface ITriggerAction
    {
        void UpdateAll();
        void UpdateAllOfTable(int tableId);
        void UpdateTableObject(TableObject tableObject, bool fileDownloaded);
    }
}
