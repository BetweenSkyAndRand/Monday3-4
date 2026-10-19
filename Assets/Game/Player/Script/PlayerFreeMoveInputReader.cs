using UnityEngine;

namespace Game.Player.Script
{
    /// <summary>
    /// GameInputReader から自由移動用の平面移動方向を解決する。
    /// 入力取得の詳細を PlayerFreeMover 本体から分離し、移動オーケストレーションの責務を軽くする。
    /// </summary>
    public class PlayerFreeMoveInputReader
    {
        public Vector3 ResolveMoveDirection(GameInputReader inputReader)
        {
            if (inputReader == null)
            {
                return Vector3.zero;
            }

            var rawInput = inputReader.MoveInput;
            var moveDirection = new Vector3(rawInput.x, 0f, rawInput.y);
            if (moveDirection.sqrMagnitude > 1f)
            {
                moveDirection.Normalize();
            }

            return moveDirection;
        }
    }
}

