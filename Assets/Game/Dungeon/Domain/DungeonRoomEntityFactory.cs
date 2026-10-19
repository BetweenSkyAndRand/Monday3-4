using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// DungeonRoomEntity を生成するファクトリ。
    /// セル座標系の仕様：
    ///   - 部屋グリッド座標 (rx, ry) の部屋中心 = ワールド (rx*w*cs, 0, ry*h*cs)
    ///   - セルグリッド座標 = (rx*w + lx - half, ry*h + ly - half)  half=(w-1)/2=6
    ///   - 既存の GridManager.RoomSize=13 と同じ座標系で互換性を保つ
    /// </summary>
    public static class DungeonRoomEntityFactory
    {
        /// <summary>
        /// 指定した部屋グリッド座標の DungeonRoomEntity を生成する。
        /// </summary>
        public static DungeonRoomEntity Create(
            Vector2Int roomGridPos,
            DungeonRoomParameter roomParam,
            DungeonCellParameter cellParam,
            Color cellColor)
        {
            int w  = roomParam.RoomWidth;
            int h  = roomParam.RoomHeight;
            float cs = cellParam.CellSize;
            int halfW = (w - 1) / 2; // 6 for w=13
            int halfH = (h - 1) / 2;

            // 部屋を構成する w*h セルを生成
            var cells = new DungeonCellEntity[w * h];
            for (int ly = 0; ly < h; ly++)
            {
                for (int lx = 0; lx < w; lx++)
                {
                    var gridPos  = new Vector2Int(roomGridPos.x * w + lx - halfW,
                                                  roomGridPos.y * h + ly - halfH);
                    var worldPos = new Vector3(gridPos.x * cs, 0f, gridPos.y * cs);
                    cells[ly * w + lx] = new DungeonCellEntity(worldPos, gridPos, cellColor);
                }
            }

            // 外側 1 マスの輪郭セルを生成（15x15 外周 - 13x13 = 56 セル）
            var outline = CreateOutlineCells(roomGridPos, w, h, halfW, halfH, cs, cellColor);

            return new DungeonRoomEntity(cells, outline, roomGridPos);
        }

        /// <summary>
        /// 部屋中心のワールド座標を返す。
        /// </summary>
        public static Vector3 GetRoomWorldCenter(
            Vector2Int roomGridPos,
            DungeonRoomParameter roomParam,
            DungeonCellParameter cellParam)
        {
            float cs = cellParam.CellSize;
            return new Vector3(
                roomGridPos.x * roomParam.RoomWidth  * cs,
                0f,
                roomGridPos.y * roomParam.RoomHeight * cs
            );
        }

        // ワールド座標から部屋グリッド座標へ変換するユーティリティ
        public static Vector2Int WorldToRoomGrid(
            Vector3 worldPos,
            DungeonRoomParameter roomParam,
            DungeonCellParameter cellParam)
        {
            float roomSize = roomParam.RoomWidth * cellParam.CellSize;
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / roomSize),
                Mathf.RoundToInt(worldPos.z / roomSize)
            );
        }

        /// <summary>
        /// 指定したワールド座標が、指定した部屋グリッド座標の部屋の範囲内にあるかを判定する。
        /// ダンジョン崩壊システムなど、「対象がどの部屋にいるか」を判定する用途で使用する。
        /// </summary>
        public static bool IsPositionInRoom(
            Vector3 worldPos,
            Vector2Int roomGridPos,
            DungeonRoomParameter roomParam,
            DungeonCellParameter cellParam)
        {
            return WorldToRoomGrid(worldPos, roomParam, cellParam) == roomGridPos;
        }

        // ──────────────────────────────────────────
        // private
        // ──────────────────────────────────────────

        private static DungeonCellEntity[] CreateOutlineCells(
            Vector2Int roomGridPos, int w, int h, int halfW, int halfH,
            float cs, Color color)
        {
            int minX = roomGridPos.x * w - halfW;
            int maxX = minX + w - 1;
            int minY = roomGridPos.y * h - halfH;
            int maxY = minY + h - 1;

            var list = new List<DungeonCellEntity>(56);

            // 上辺・下辺（角を含む：minX-1 ～ maxX+1 の幅）
            for (int x = minX - 1; x <= maxX + 1; x++)
            {
                list.Add(MakeCell(x, maxY + 1, cs, color)); // 上辺
                list.Add(MakeCell(x, minY - 1, cs, color)); // 下辺
            }

            // 左辺・右辺（角を除く：minY ～ maxY の高さ）
            for (int y = minY; y <= maxY; y++)
            {
                list.Add(MakeCell(maxX + 1, y, cs, color)); // 右辺
                list.Add(MakeCell(minX - 1, y, cs, color)); // 左辺
            }

            return list.ToArray();
        }

        private static DungeonCellEntity MakeCell(int gx, int gy, float cs, Color color)
        {
            var gridPos  = new Vector2Int(gx, gy);
            var worldPos = new Vector3(gx * cs, 0f, gy * cs);
            return new DungeonCellEntity(worldPos, gridPos, color);
        }
    }
}

