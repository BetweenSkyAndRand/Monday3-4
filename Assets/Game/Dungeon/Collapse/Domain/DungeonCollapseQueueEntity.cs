using UnityEngine;

// ReSharper disable once CheckNamespace
// プロジェクト規約でフォルダ階層より短い名前空間を使用している (例: Game.Dungeon)
namespace Game.Dungeon
{
    /// <summary>
    /// 崩壊待ちの部屋グリッド座標を「配置順の逆順」で保持する不変エンティティ。
    /// 状態変更（次の部屋へ進める）は常に <see cref="Advance"/> による新インスタンスの再生成で行う。
    /// </summary>
    public readonly struct DungeonCollapseQueueEntity
    {
        /// <summary>崩壊予定の部屋グリッド座標（配置順の逆順で並んでいる）。</summary>
        public Vector2Int[] PendingRoomPositions { get; }

        /// <summary>現在の崩壊対象を指すインデックス。</summary>
        public int CurrentIndex { get; }

        public DungeonCollapseQueueEntity(Vector2Int[] pendingRoomPositions, int currentIndex)
        {
            PendingRoomPositions = pendingRoomPositions;
            CurrentIndex = currentIndex;
        }

        /// <summary>
        /// 崩壊予定の部屋座標配列からキューを作成する（先頭 = 最初に崩壊する部屋）。
        /// </summary>
        public static DungeonCollapseQueueEntity Create(Vector2Int[] orderedRoomPositions)
        {
            return new DungeonCollapseQueueEntity(orderedRoomPositions ?? System.Array.Empty<Vector2Int>(), 0);
        }

        /// <summary>まだ崩壊していない部屋が残っているか。</summary>
        public bool HasNext => PendingRoomPositions != null && CurrentIndex < PendingRoomPositions.Length;

        /// <summary>現在の崩壊対象の部屋グリッド座標。<see cref="HasNext"/> が true のときのみ有効。</summary>
        public Vector2Int Current => PendingRoomPositions[CurrentIndex];

        /// <summary>
        /// 次の部屋へキューを進めた新しいエンティティを返す（イミュータブルのため再生成）。
        /// </summary>
        public DungeonCollapseQueueEntity Advance()
        {
            return new DungeonCollapseQueueEntity(PendingRoomPositions, CurrentIndex + 1);
        }
    }
}

