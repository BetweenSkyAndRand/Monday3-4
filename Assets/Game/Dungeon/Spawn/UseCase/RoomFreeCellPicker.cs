using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon.Spawn.UseCase
{
    /// <summary>
    /// 部屋の CellEntities から使用済みでないセルをランダムに選んで返すロジック。
    /// 責務: 空きセル探索アルゴリズムのみ（Instantiate や設定取得は行わない）。
    ///
    /// スポーン除外ルール：
    ///   - 部屋外周 1 セル分（gridPos が minX / maxX / minY / maxY に一致するセル）は除外する。
    ///     これらのセルは壁のコライダーと重なる可能性があり、
    ///     スポーンすると壁の中に埋まってしまうバグの原因となる。
    /// </summary>
    public class RoomFreeCellPicker
    {
        /// <summary>
        /// 部屋の CellEntities から使用済みでないセルをランダムに選んで返す。
        /// すべてのセルが使用済みの場合は null を返す。
        /// </summary>
        /// <param name="room">対象部屋エンティティ</param>
        /// <param name="usedCells">使用済みセルの HashSet</param>
        /// <param name="random">乱数生成器（テスト容易性のため外部注入）</param>
        /// <returns>空きセルのグリッド座標。すべて使用済みの場合は null</returns>
        public Vector2Int? Pick(DungeonRoomEntity room, HashSet<Vector2Int> usedCells, System.Random random)
        {
            var cells = room.CellEntities;
            if (cells == null || cells.Length == 0)
            {
                return null;
            }

            // 部屋セルの GridPosition 範囲（最小・最大）を求める
            var minX = int.MaxValue;
            var maxX = int.MinValue;
            var minY = int.MaxValue;
            var maxY = int.MinValue;

            foreach (var cell in cells)
            {
                if (cell.GridPosition.x < minX)
                {
                    minX = cell.GridPosition.x;
                }
                if (cell.GridPosition.x > maxX)
                {
                    maxX = cell.GridPosition.x;
                }
                if (cell.GridPosition.y < minY)
                {
                    minY = cell.GridPosition.y;
                }
                if (cell.GridPosition.y > maxY)
                {
                    maxY = cell.GridPosition.y;
                }
            }

            // 外周 1 セル分を除外した内部セルのインデックスを収集する
            var indices = new List<int>(cells.Length);
            for (var i = 0; i < cells.Length; i++)
            {
                var gridPos = cells[i].GridPosition;
                var isBorderCell = gridPos.x == minX || gridPos.x == maxX
                                || gridPos.y == minY || gridPos.y == maxY;
                if (!isBorderCell)
                {
                    indices.Add(i);
                }
            }

            // 全セルが境界セルだった場合（部屋が極端に小さいケース）は全セルを候補にする
            if (indices.Count == 0)
            {
                for (var i = 0; i < cells.Length; i++)
                {
                    indices.Add(i);
                }
            }

            // フィッシャー・イェーツシャッフルで順序をランダム化し、空きセルを探す
            for (var i = indices.Count - 1; i > 0; i--)
            {
                var j = random.Next(0, i + 1);
                (indices[i], indices[j]) = (indices[j], indices[i]);
            }

            foreach (var index in indices)
            {
                var gridPos = cells[index].GridPosition;
                if (!usedCells.Contains(gridPos))
                {
                    return gridPos;
                }
            }

            return null;
        }
    }
}

