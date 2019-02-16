using davClassLibrary.Models;

namespace davClassLibrary.Common
{
    public interface ITriggerAction
    {
        void UpdateAllOfTable(int tableId);
        void UpdateTableObject(TableObject tableObject, bool fileDownloaded);
        void DeleteTableObject(TableObject tableObject);
        void SyncFinished();
    }
}
