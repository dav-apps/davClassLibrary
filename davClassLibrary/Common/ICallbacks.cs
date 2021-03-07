using davClassLibrary.Models;

namespace davClassLibrary.Common
{
    public interface ICallbacks
    {
        void UpdateAllOfTable(int tableId, bool changed);
        void UpdateTableObject(TableObject tableObject, bool fileDownloaded);
        void DeleteTableObject(TableObject tableObject);
        void TableObjectDownloadProgress(TableObject tableObject, int progress);
        void SyncFinished();
    }
}
