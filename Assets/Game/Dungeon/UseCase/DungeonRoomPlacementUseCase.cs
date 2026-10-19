using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
// プロジェクト規約でフォルダ階層より短い名前空間を使用している (例: Game.Dungeon)
namespace Game.Dungeon
{
    /// <summary>
    /// 部屋の配置可否を判定するユースケース。
    /// 判定条件：
    ///   1. cellEntities が既存部屋と重複しない（重複なし）
    ///   2. outLineCellEntity が既存部屋のセルとヒットする（隣接部屋あり）
    ///   3. 実際に隣接している（Manhattan距離が1）部屋が存在する（斜め配置を防ぐ）
    /// </summary>
    public class DungeonRoomPlacementUseCase
    {
        private readonly DungeonPlacedRoomRepository repository;
        private readonly int maxPlacedRoomCount;

        public DungeonRoomPlacementUseCase(DungeonPlacedRoomRepository repository, int maxPlacedRoomCount = int.MaxValue)
        {
            this.repository = repository;
            this.maxPlacedRoomCount = maxPlacedRoomCount;
        }

        /// <summary>配置済みの部屋数が上限に達しているか。中央の拠点部屋は数えない。</summary>
        public bool IsRoomLimitReached()
        {
            var placedRoomCount = 0;
            foreach (var placedRoom in repository.GetAll())
            {
                if (placedRoom.RoomGridPosition != Vector2Int.zero)
                {
                    placedRoomCount++;
                }
            }

            return placedRoomCount >= maxPlacedRoomCount;
        }

        /// <summary>
        /// 指定エンティティを配置できるか判定する。
        /// </summary>
        /// <returns>配置可能であれば true</returns>
        public bool Execute(DungeonRoomEntity roomEntity)
        {
            return Evaluate(roomEntity) == DungeonPlacementResult.Placeable;
        }

        /// <summary>
        /// 指定エンティティの配置可否を、配置不可の理由つきで判定する。
        /// </summary>
        public DungeonPlacementResult Evaluate(DungeonRoomEntity roomEntity)
        {
            // 上限判定は重なり・隣接より先に行い、上限到達の理由を最優先で伝える
            if (IsRoomLimitReached())
            {
                return DungeonPlacementResult.RoomLimitReached;
            }

            // 条件 1: 新部屋のセルが既存部屋と重複していないか確認
            foreach (var cell in roomEntity.CellEntities)
            {
                if (repository.IsCellOccupied(cell.GridPosition))
                {
                    return DungeonPlacementResult.Overlapped;
                }
            }

            // 条件 2 & 3: 輪郭セルが既存部屋のセルとヒットし、かつ実際に隣接している部屋が存在するか
            var neighborRooms = new HashSet<Vector2Int>();
            
            foreach (var outlineCell in roomEntity.OutLineCellEntity)
            {
                if (repository.TryGetRoomByCell(outlineCell.GridPosition, out var neighborRoom))
                {
                    neighborRooms.Add(neighborRoom.RoomGridPosition);
                }
            }

            // 隣接部屋が見つからない場合は配置不可
            if (neighborRooms.Count == 0)
            {
                return DungeonPlacementResult.NoNeighbor;
            }

            // 見つかった隣接候補のうち、実際に隣接している（Manhattan距離が1）部屋が存在するか確認
            foreach (var neighborRoomPos in neighborRooms)
            {
                if (IsActuallyAdjacent(roomEntity.RoomGridPosition, neighborRoomPos))
                {
                    return DungeonPlacementResult.Placeable;
                }
            }

            return DungeonPlacementResult.NoNeighbor;
        }
        
        /// <summary>
        /// 2つの部屋が実際に隣接しているか判定する。
        /// Manhattan距離が1で、かつ直線上（横または縦のみ）に隣接している必要がある。
        /// </summary>
        private bool IsActuallyAdjacent(Vector2Int roomA, Vector2Int roomB)
        {
            int dx = Mathf.Abs(roomA.x - roomB.x);
            int dy = Mathf.Abs(roomA.y - roomB.y);
            
            // Manhattan距離が1で、かつ一方向のみに隣接（斜めは除外）
            return (dx + dy == 1) && (dx * dy == 0);
        }
    }
}

