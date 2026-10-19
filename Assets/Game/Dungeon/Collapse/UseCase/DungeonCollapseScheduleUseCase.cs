using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// 配置済み部屋（中央拠点を除く）から、崩壊キューを「配置順の逆順」で構築するユースケース。
    /// 例：A→B→C の順で配置された場合、崩壊は C→B→A の順で発生する。
    /// </summary>
    public class DungeonCollapseScheduleUseCase
    {
        /// <summary>拠点（中央）部屋のグリッド座標。崩壊対象から常に除外する。</summary>
        private static readonly Vector2Int centralRoomGridPosition = Vector2Int.zero;

        /// <summary>
        /// 配置済み部屋一覧から崩壊キューを生成する。
        /// </summary>
        /// <param name="repository">配置済み部屋の読み取り専用リポジトリ。</param>
        public DungeonCollapseQueueEntity Execute(IReadOnlyDungeonRoomRepository repository)
        {
            var allRooms = repository.GetAll();
            var pendingPositions = new List<Vector2Int>();

            // repository.GetAll() は Cache() が呼ばれた順＝配置順のリストであるため、
            // 逆順に読み取ることで「最後に作った部屋から崩壊する」という要件をそのまま満たせる。
            for (var i = allRooms.Count - 1; i >= 0; i--)
            {
                var roomGridPosition = allRooms[i].RoomGridPosition;
                if (roomGridPosition == centralRoomGridPosition)
                {
                    continue;
                }

                pendingPositions.Add(roomGridPosition);
            }

            return DungeonCollapseQueueEntity.Create(pendingPositions.ToArray());
        }
    }
}



