using UnityEngine;

namespace Game.Grid
{
    /// <summary>
    /// プレイヤーの移動戦略。
    /// 占有されているグリッドにも移動可能（敵を押しのける）。
    /// ただし、IInteractableObject を実装しているオブジェクト（ボタン等）には移動できない。
    /// </summary>
    public class PlayerMovementStrategy : IGridMovementStrategy
    {
        public bool CanMoveToOccupiedGrid(GameObject occupant, GameObject self)
        {
            if (occupant == null)
            {
                return true;
            }

            // IInteractableObject を実装しているオブジェクト（インタラクト可能なボタン等）には移動できない
            var interactable = occupant.GetComponent<Game.Interaction.IInteractableObject>();
            if (interactable != null)
            {
                return false;
            }

            // それ以外（敵など）は移動可能（押しのける）
            return true;
        }

        public bool ShouldNotifyOccupant()
        {
            // プレイヤーは占有者（敵）に吹き飛ばしを通知する
            return true;
        }
    }

    /// <summary>
    /// 敵の移動戦略。
    /// 占有されているグリッドには移動不可。
    /// </summary>
    public class EnemyMovementStrategy : IGridMovementStrategy
    {
        public bool CanMoveToOccupiedGrid(GameObject occupant, GameObject self)
        {
            // 敵は占有されているグリッドには移動できない
            return false;
        }

        public bool ShouldNotifyOccupant()
        {
            // 敵は占有者に通知しない
            return false;
        }
    }
}

