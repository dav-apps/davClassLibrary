using SQLite;
using System;
using System.Collections.Generic;
using System.Text;
using static davClassLibrary.Models.SyncObject;

namespace davClassLibrary.Models
{
    public class SyncTableObject
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [NotNull]
        public Guid Uuid { get; set; }
        [NotNull]
        public int Operation { get; set; }

        public SyncTableObject()
        {

        }

        public SyncTableObject(Guid uuid, SyncOperation operation)
        {
            Uuid = uuid;
            Operation = ParseSyncOperationToInt(operation);
        }
    }

    public class SyncProperty
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }
        [NotNull]
        public int PropertyId { get; set; }
        [NotNull]
        public int Operation { get; set; }

        public SyncProperty()
        {

        }

        public SyncProperty(int propertyId, SyncOperation operation)
        {
            PropertyId = propertyId;
            Operation = ParseSyncOperationToInt(operation);
        }
    }

    public class SyncObject
    {
        public int Id { get; set; }
        public Guid Uuid { get; set; }
        public SyncOperation Operation { get; set; }

        public SyncObject(int id, Guid uuid, SyncOperation operation)
        {
            Id = id;
            Uuid = uuid;
            Operation = operation;
        }

        public enum SyncTable
        {
            SyncTableObject,
            SyncProperty
        }

        public enum SyncOperation
        {
            Create,
            Update,
            Delete
        }

        public static int ParseSyncOperationToInt(SyncOperation operation)
        {
            switch (operation)
            {
                case SyncOperation.Update:
                    return 1;
                case SyncOperation.Delete:
                    return 2;
                default:
                    return 0;
            }
        }
    }
}
