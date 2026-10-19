using UnityEngine;

namespace Game.Player.Script
{
    /// <summary>
    /// 自由移動中の向き・速度・移動状態を保持するランタイム状態クラス。
    /// MonoBehaviour から切り離し、値更新ルールをひとまとめにする。
    /// </summary>
    public class PlayerFreeMoveState
    {
        public Vector3 FacingDirection { get; private set; } = Vector3.forward;
        public Vector3 CurrentVelocity { get; private set; }
        public bool IsMoving { get; private set; }

        public void UpdateVelocity(Vector3 currentVelocity, float moveThreshold)
        {
            CurrentVelocity = currentVelocity;

            var planarVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            IsMoving = planarVelocity.magnitude >= moveThreshold;
        }

        public void UpdateFacingDirection(Vector3 currentVelocity, float moveThreshold)
        {
            var planarVelocity = new Vector3(currentVelocity.x, 0f, currentVelocity.z);
            if (planarVelocity.sqrMagnitude < moveThreshold * moveThreshold)
            {
                return;
            }

            FacingDirection = planarVelocity.normalized;
        }

        public void ForceFacingDirection(Vector3 facingDirection)
        {
            var planarDirection = new Vector3(facingDirection.x, 0f, facingDirection.z);
            if (planarDirection.sqrMagnitude <= 0f)
            {
                return;
            }

            FacingDirection = planarDirection.normalized;
        }

        public void ResetVelocity()
        {
            CurrentVelocity = Vector3.zero;
            IsMoving = false;
        }
    }
}

