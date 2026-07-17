using System;

namespace _001_Scripts._005_Data.Hall
{
    public readonly struct TableSnapshot
    {
        public readonly Guid TableId;
        public readonly string TableName;
        public readonly int TotalSeats;
        public readonly int FreeSeats;
        public readonly int DirtySeats;

        public TableSnapshot(Guid tableId, string tableName, int totalSeats, int freeSeats, int dirtySeats)
        {
            TableId = tableId;
            TableName = tableName;
            TotalSeats = totalSeats;
            FreeSeats = freeSeats;
            DirtySeats = dirtySeats;
        }
    }
}
