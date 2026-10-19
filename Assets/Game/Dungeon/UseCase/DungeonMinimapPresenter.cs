using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// ミニマップに表示する部屋の配置を計算する純粋 C# クラス。
    /// 部屋の座標とプレイヤー位置だけを扱い、敵や宝箱の情報は一切受け取らない。
    /// プレイヤーのいる部屋を常に中心にして、タイルの大きさは固定とする。
    /// </summary>
    public class DungeonMinimapPresenter
    {
        /// <summary>ワールド座標を部屋のグリッド座標へ変換する。</summary>
        public Vector2Int WorldToRoomGrid(Vector3 worldPosition, float roomWorldSize)
        {
            if (roomWorldSize <= 0f)
            {
                return Vector2Int.zero;
            }

            return new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / roomWorldSize),
                Mathf.RoundToInt(worldPosition.z / roomWorldSize));
        }

        /// <summary>プレイヤーの部屋を原点としたときの、部屋タイルの UI 座標を返す。</summary>
        public Vector2 ToAnchoredPosition(Vector2Int roomGridPosition, Vector2Int playerRoomGridPosition, float tileSize)
        {
            return (Vector2)(roomGridPosition - playerRoomGridPosition) * tileSize;
        }

        /// <summary>タイル全体が表示領域（中心が原点）に収まっているか。</summary>
        public bool IsInsideView(Vector2 anchoredPosition, float tileSize, Vector2 viewSize)
        {
            var halfTileSize = tileSize * 0.5f;
            return Mathf.Abs(anchoredPosition.x) + halfTileSize <= viewSize.x * 0.5f
                && Mathf.Abs(anchoredPosition.y) + halfTileSize <= viewSize.y * 0.5f;
        }
    }
}
