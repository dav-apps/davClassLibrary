﻿using davClassLibrary.Common;
using davClassLibrary.Models;
using System;

namespace davClassLibrary.Tests.Common
{
    public class Callbacks : ICallbacks
    {
        public void DeleteTableObject(Guid uuid, int tableId) { }

        public void UpdateAllOfTable(int tableId, bool changed, bool complete) { }

        public void UpdateTableObject(TableObject tableObject, bool fileDownloaded) { }

        public void TableObjectDownloadProgress(Guid uuid, int progress) { }

        public void UserSyncFinished() { }

        public void SyncFinished() { }
    }
}
