using UnityEngine;

namespace Game.Player
{
    /// <summary>
    /// プレイヤー移動コンポーネントの共通インターフェース。
    /// 既存のグリッド移動と新しい自由移動の両方を同じ参照口で扱えるようにする。
    /// </summary>
    public interface IPlayerMover
    {
        Vector3 FacingDirection { get; }
        Vector3 CurrentVelocity { get; }
        bool IsMoving { get; }

        void Warp(Vector3 worldPosition, Quaternion worldRotation);
    }
}

