using davClassLibrary.Models;
using System;

namespace davClassLibrary.Common
{
    public interface ICallbacks
    {
        void UpdateAllOfTable(int tableId, bool changed, bool complete);
        void UpdateTableObject(TableObject tableObject, bool fileDownloaded);
        void DeleteTableObject(Guid uuid, int tableId);
        void TableObjectDownloadProgress(Guid uuid, int progress);
        void UserSyncFinished();
        void SyncFinished();
    }
}
