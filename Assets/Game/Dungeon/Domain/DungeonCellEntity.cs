using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// セル 1 つを表すイミュータブルな値型。
    /// 変更が必要な場合は <see cref="WithColor"/> で新しい値を生成する。
    /// </summary>
    public readonly struct DungeonCellEntity
    {
        /// <summary>ワールド座標</summary>
        public Vector3 WorldPosition { get; }

        /// <summary>セルレベルのグリッド座標（1 単位 = 1 セル）</summary>
        public Vector2Int GridPosition { get; }

        /// <summary>セルの表示色</summary>
        public Color CellColor { get; }

        public DungeonCellEntity(Vector3 worldPosition, Vector2Int gridPosition, Color cellColor)
        {
            WorldPosition = worldPosition;
            GridPosition  = gridPosition;
            CellColor     = cellColor;
        }

        /// <summary>
        /// 色だけ差し替えた新しい <see cref="DungeonCellEntity"/> を返す。
        /// struct はイミュータブルなので、変更は常に再生成で行う。
        /// </summary>
        public DungeonCellEntity WithColor(Color newColor)
            => new DungeonCellEntity(WorldPosition, GridPosition, newColor);
    }
}

