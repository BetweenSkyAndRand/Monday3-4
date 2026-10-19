using System.Collections.Generic;
using UnityEngine;

namespace Game.Grid
{
    /// <summary>
    /// グリッド上の占有状態を管理する静的クラス。
    /// プレイヤーと敵がどのグリッドにいるかを追跡し、衝突を防ぐ。
    /// </summary>
    public static class GridOccupationManager
    {
        /// <summary>グリッド座標 → 占有しているGameObjectのマップ</summary>
        private static readonly Dictionary<Vector2Int, GameObject> occupiedGrids = new Dictionary<Vector2Int, GameObject>();

        /// <summary>
        /// 指定したグリッドが占有されているかチェックする。
        /// </summary>
        /// <param name="gridPos">チェックするグリッド座標</param>
        /// <param name="occupant">占有しているGameObject（占有されていない場合はnull）</param>
        /// <returns>占有されている場合true</returns>
        public static bool IsOccupied(Vector2Int gridPos, out GameObject occupant)
        {
            return occupiedGrids.TryGetValue(gridPos, out occupant);
        }

        /// <summary>
        /// グリッドを占有する。
        /// </summary>
        /// <param name="gridPos">占有するグリッド座標</param>
        /// <param name="occupant">占有するGameObject</param>
        public static void Occupy(Vector2Int gridPos, GameObject occupant)
        {
            if (occupant == null)
            {
                return;
            }

            occupiedGrids[gridPos] = occupant;
        }

        /// <summary>
        /// グリッドの占有を解除する。
        /// </summary>
        /// <param name="gridPos">解除するグリッド座標</param>
        public static void Release(Vector2Int gridPos)
        {
            occupiedGrids.Remove(gridPos);
        }

        /// <summary>
        /// 指定したGameObjectが占有しているグリッドを解放する。
        /// </summary>
        /// <param name="occupant">解放するGameObject</param>
        public static void ReleaseByOccupant(GameObject occupant)
        {
            if (occupant == null)
            {
                return;
            }

            var keysToRemove = new List<Vector2Int>();
            foreach (var kvp in occupiedGrids)
            {
                if (kvp.Value == occupant)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                occupiedGrids.Remove(key);
            }
        }

        /// <summary>
        /// すべての占有情報をクリアする（シーン切り替え時などに使用）。
        /// </summary>
        public static void Clear()
        {
            occupiedGrids.Clear();
        }

        /// <summary>
        /// デバッグ用：現在の占有状況をログ出力
        /// </summary>
        public static void DebugLogOccupation()
        {
            UnityEngine.Debug.Log($"[GridOccupationManager] 占有グリッド数: {occupiedGrids.Count}");
            foreach (var kvp in occupiedGrids)
            {
                UnityEngine.Debug.Log($"  グリッド {kvp.Key} → {kvp.Value.name}");
            }
        }
    }
}

