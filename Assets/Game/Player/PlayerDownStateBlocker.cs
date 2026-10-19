using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Player
{
    /// <summary>
    /// プレイヤーのダウン状態を管理し、移動・インタラクトを禁止する。
    /// IMovementBlocker を実装し、RogueMover の movementBlockerObjects にアサインして使用する。
    /// IInteractBlocker を実装し、PlayerInteract の interactBlockerObjects にアサインして使用する。
    ///
    /// ダウン状態は SetDown() で有効化し、Recover() で解除する。
    /// プレイヤーHPが0になったとき PlayerStatusController がこのクラスの SetDown() を呼ぶ。
    /// </summary>
    public class PlayerDownStateBlocker : MonoBehaviour, IMovementBlocker, IInteractBlocker
    {
        /// <summary>ダウン状態かどうか。true のとき移動・インタラクトを禁止する。</summary>
        public bool IsDown { get; private set; }

        // ─── IMovementBlocker ─────────────────────────────────────────

        /// <summary>
        /// ダウン状態なら true を返す。
        /// RogueMover が毎フレーム参照し、true の場合は入力を無視して移動を止める。
        /// </summary>
        public bool IsMovementBlocked => IsDown;

        // ─── IInteractBlocker ─────────────────────────────────────────

        /// <summary>
        /// ダウン状態なら true を返す。
        /// PlayerInteract が毎フレーム参照し、true の場合はインタラクト UI を非表示にして操作を禁止する。
        /// </summary>
        public bool IsInteractBlocked => IsDown;

        // ─── 状態操作 ──────────────────────────────────────────────────

        /// <summary>
        /// プレイヤーをダウン状態にする。
        /// プレイヤーHPが0になったとき PlayerStatusController から呼ばれる。
        /// 既にダウン状態の場合は何もしない。
        /// </summary>
        public void SetDown()
        {
            if (IsDown)
            {
                return;
            }

            IsDown = true;
            UnityEngine.Debug.Log("[PlayerDownStateBlocker] プレイヤーがダウン状態になりました。移動・インタラクトを禁止します。");
        }

        /// <summary>
        /// ダウン状態を解除する。
        /// 将来のリスポーン・復活演出などで使用する。
        /// </summary>
        public void Recover()
        {
            if (!IsDown)
            {
                return;
            }

            IsDown = false;
            UnityEngine.Debug.Log("[PlayerDownStateBlocker] プレイヤーのダウン状態が解除されました。");
        }
    }
}

