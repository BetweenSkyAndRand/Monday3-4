using UnityEngine;
namespace Game.Dungeon
{
    public readonly struct DungeonRoomEntity
    {
        public DungeonCellEntity[] CellEntities { get; }
        public DungeonCellEntity[] OutLineCellEntity { get; }
        public Vector2Int RoomGridPosition { get; }
        public DungeonRoomEntity(
            DungeonCellEntity[] cellEntities,
            DungeonCellEntity[] outLineCellEntity,
            Vector2Int roomGridPosition)
        {
            CellEntities      = cellEntities;
            OutLineCellEntity = outLineCellEntity;
            RoomGridPosition  = roomGridPosition;
        }
    }
}
