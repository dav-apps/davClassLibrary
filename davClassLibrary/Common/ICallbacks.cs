using davClassLibrary.Models;
using System;

namespace davClassLibrary.Common
{
    public interface ICallbacks
    {
        void UpdateAllOfTable(int tableId, bool changed);
        void UpdateTableObject(TableObject tableObject, bool fileDownloaded);
        void DeleteTableObject(Guid uuid, int tableId);
        void TableObjectDownloadProgress(TableObject tableObject, int progress);
        void SyncFinished();
    }
}
