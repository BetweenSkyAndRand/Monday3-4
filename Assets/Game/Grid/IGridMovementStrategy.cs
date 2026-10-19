using UnityEngine;

namespace Game.Grid
{
    /// <summary>
    /// グリッド移動時の振る舞い戦略を定義するインターフェース。
    /// プレイヤーと敵で異なる移動ルールを適用する。
    /// </summary>
    public interface IGridMovementStrategy
    {
        /// <summary>
        /// 占有されているグリッドに移動できるかどうかを判定する。
        /// </summary>
        /// <param name="occupant">占有しているGameObject</param>
        /// <param name="self">移動しようとしているGameObject</param>
        /// <returns>移動可能ならtrue</returns>
        bool CanMoveToOccupiedGrid(GameObject occupant, GameObject self);

        /// <summary>
        /// 移動確定時に占有者に対して通知するかどうか。
        /// </summary>
        /// <returns>通知する場合true（プレイヤーが敵を押しのける場合など）</returns>
        bool ShouldNotifyOccupant();
    }
}

