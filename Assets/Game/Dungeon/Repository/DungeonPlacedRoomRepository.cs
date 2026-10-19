using System.Collections.Generic;
using UnityEngine;
namespace Game.Dungeon
{
    /// <summary>
    /// 配置済み部屋エンティティを管理するリポジトリ。
    /// 情報の保存・取得のみを担い、ロジックは持たない。
    /// </summary>
    public class DungeonPlacedRoomRepository : IReadOnlyDungeonRoomRepository
    {
        private readonly List<DungeonRoomEntity> placedRooms = new();
        private readonly Dictionary<Vector2Int, DungeonRoomEntity> cellToRoom = new();
        private readonly Dictionary<Vector2Int, DungeonRoomEntity> roomGridToRoom = new();

        public void Cache(DungeonRoomEntity roomEntity)
        {
            placedRooms.Add(roomEntity);
            roomGridToRoom[roomEntity.RoomGridPosition] = roomEntity;
            foreach (var cell in roomEntity.CellEntities)
            {
                cellToRoom[cell.GridPosition] = roomEntity;
            }
        }

        /// <summary>
        /// 指定した部屋グリッド座標のエンティティをキャッシュから完全に除去する。
        /// ダンジョン崩壊システムなど、配置済み部屋を消滅させる用途で使用する。
        /// Cache() の対称操作として、保存情報の削除のみを行う（判定ロジックは持たない）。
        /// </summary>
        /// <returns>削除対象が存在し削除できた場合は true。</returns>
        public bool Remove(Vector2Int roomGridPosition)
        {
            if (!roomGridToRoom.TryGetValue(roomGridPosition, out var roomEntity))
            {
                return false;
            }

            roomGridToRoom.Remove(roomGridPosition);
            foreach (var cell in roomEntity.CellEntities)
            {
                cellToRoom.Remove(cell.GridPosition);
            }

            // readonly struct の既定の等値比較（リフレクション）を避けるため、
            // インデックス検索で該当エントリのみを除去する。
            for (var i = 0; i < placedRooms.Count; i++)
            {
                if (placedRooms[i].RoomGridPosition == roomGridPosition)
                {
                    placedRooms.RemoveAt(i);
                    break;
                }
            }

            return true;
        }

        public IReadOnlyList<DungeonRoomEntity> GetAll() => placedRooms;
        public bool IsCellOccupied(Vector2Int cellGridPos)
            => cellToRoom.ContainsKey(cellGridPos);
        public bool TryGetRoomByCell(Vector2Int cellGridPos, out DungeonRoomEntity room)
            => cellToRoom.TryGetValue(cellGridPos, out room);
        public bool TryGetRoomByGridPos(Vector2Int roomGridPos, out DungeonRoomEntity room)
            => roomGridToRoom.TryGetValue(roomGridPos, out room);
    }
}
