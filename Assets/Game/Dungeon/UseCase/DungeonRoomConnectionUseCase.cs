using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
// プロジェクト規約でフォルダ階層より短い名前空間を使用している (例: Game.Dungeon)
namespace Game.Dungeon
{
    public class DungeonRoomConnectionUseCase
    {
        public readonly struct ConnectionResult
        {
            public Direction NewRoomDirection   { get; }
            public DungeonRoomEntity NeighborRoomEntity { get; }
            public Direction NeighborDirection  { get; }
            public ConnectionResult(Direction newDir, DungeonRoomEntity neighbor, Direction neighborDir)
            {
                NewRoomDirection   = newDir;
                NeighborRoomEntity = neighbor;
                NeighborDirection  = neighborDir;
            }
        }
        
        private readonly DungeonPlacedRoomRepository repository;
        private readonly DungeonRoomParameter roomParam;
        
        public DungeonRoomConnectionUseCase(DungeonPlacedRoomRepository repository, DungeonRoomParameter roomParam)
        {
            this.repository = repository;
            this.roomParam  = roomParam;
        }
        
        public ConnectionResult[] Execute(DungeonRoomEntity newRoom)
        {
            var results        = new List<ConnectionResult>();
            var processedRooms = new HashSet<Vector2Int>();
            foreach (var outlineCell in newRoom.OutLineCellEntity)
            {
                if (!repository.TryGetRoomByCell(outlineCell.GridPosition, out var neighborRoom))
                {
                    continue;
                }
                if (processedRooms.Contains(neighborRoom.RoomGridPosition))
                {
                    continue;
                }
                
                // 実際に隣接しているか検証（Manhattan距離が1かつ直線上）
                if (!IsActuallyAdjacent(newRoom.RoomGridPosition, neighborRoom.RoomGridPosition))
                {
                    continue;
                }
                
                processedRooms.Add(neighborRoom.RoomGridPosition);
                
                // 部屋のグリッド座標から直接方向を判定（より正確）
                var newDir      = GetDirectionFromRoomPositions(newRoom.RoomGridPosition, neighborRoom.RoomGridPosition);
                var neighborDir = Opposite(newDir);
                results.Add(new ConnectionResult(newDir, neighborRoom, neighborDir));
            }
            return results.ToArray();
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
        
        /// <summary>
        /// 部屋Aから見て部屋Bがどの方向にあるかを返す。
        /// </summary>
        private Direction GetDirectionFromRoomPositions(Vector2Int roomA, Vector2Int roomB)
        {
            var diff = roomB - roomA;
            if (diff.y > 0) return Direction.North;
            if (diff.y < 0) return Direction.South;
            if (diff.x > 0) return Direction.East;
            if (diff.x < 0) return Direction.West;
            // 同じ座標（ありえないが念のため）
            return Direction.North;
        }
        
        private static Direction Opposite(Direction d) => d switch
        {
            Direction.North => Direction.South,
            Direction.South => Direction.North,
            Direction.East  => Direction.West,
            Direction.West  => Direction.East,
            _               => d
        };
    }
}
