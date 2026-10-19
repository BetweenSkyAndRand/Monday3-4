using Game.Player;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.BuildMode
{
    /// <summary>
    /// ビルドモードがアクティブな間、プレイヤーの移動・インタラクトを禁止する実装。
    /// ビルドモード状態の取得は BuildModeRepository に委譲し、
    /// このクラスは「ビルドモード中かどうかを各ブロッカーとして伝える」責務のみを持つ。
    ///
    /// PlayerGridMover の movementBlockerObjects と
    /// PlayerInteract の interactBlockerObjects の両方にアサインして使う。
    /// </summary>
    public class BuildModeMovementBlocker : MonoBehaviour, IMovementBlocker, IInteractBlocker
    {
        /// <summary>
        /// ビルドモードがアクティブなら true を返す。
        /// PlayerGridMover が毎フレーム参照する。
        /// </summary>
        public bool IsMovementBlocked => BuildModeRepository.GetBuildMode() == BuildModeType.Building;

        /// <summary>
        /// ビルドモードがアクティブなら true を返す。
        /// PlayerInteract が毎フレーム参照する。
        /// </summary>
        public bool IsInteractBlocked => BuildModeRepository.GetBuildMode() == BuildModeType.Building;
    }
}

