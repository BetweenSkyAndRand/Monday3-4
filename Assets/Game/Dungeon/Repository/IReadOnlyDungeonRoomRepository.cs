using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// 配置済み部屋データへの読み取り専用アクセスを定義するインターフェース。
    /// Cache() を含めないことで、外部クラスからの書き込みをコンパイル時に禁止する。
    /// </summary>
    public interface IReadOnlyDungeonRoomRepository
    {
        IReadOnlyList<DungeonRoomEntity> GetAll();
        bool IsCellOccupied(Vector2Int cellGridPos);
        bool TryGetRoomByCell(Vector2Int cellGridPos, out DungeonRoomEntity room);
        bool TryGetRoomByGridPos(Vector2Int roomGridPos, out DungeonRoomEntity room);
    }
}

