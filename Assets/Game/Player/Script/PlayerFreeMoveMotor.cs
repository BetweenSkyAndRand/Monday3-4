using Game.Player.Parameter;
using UnityEngine;

namespace Game.Player.Script
{
    /// <summary>
    /// CharacterController への移動命令とワープ処理を担当する。
    /// PlayerFreeMover 本体から Unity 物理コンポーネントへの直接操作を切り離す。
    /// </summary>
    public class PlayerFreeMoveMotor
    {
        private readonly CharacterController characterController;

        public PlayerFreeMoveMotor(CharacterController characterController)
        {
            this.characterController = characterController;
        }

        public void ApplyParameter(PlayerFreeMoveParameter playerFreeMoveParameter)
        {
            if (characterController == null || playerFreeMoveParameter == null)
            {
                return;
            }

            characterController.radius = playerFreeMoveParameter.ControllerRadius;
            characterController.height = playerFreeMoveParameter.ControllerHeight;
            characterController.skinWidth = playerFreeMoveParameter.ControllerSkinWidth;
            characterController.stepOffset = playerFreeMoveParameter.ControllerStepOffset;
            characterController.slopeLimit = playerFreeMoveParameter.ControllerSlopeLimit;
        }

        public Vector3 Move(Vector3 moveDirection, float moveSpeed, float groundedStickVelocity, float deltaTime)
        {
            if (characterController == null)
            {
                return Vector3.zero;
            }

            var planarVelocity = moveDirection * moveSpeed;
            var moveVelocity = planarVelocity;
            if (characterController.isGrounded)
            {
                moveVelocity.y = groundedStickVelocity;
            }

            characterController.Move(moveVelocity * deltaTime);
            return characterController.velocity;
        }

        public void Warp(Transform targetTransform, Vector3 worldPosition, Quaternion worldRotation)
        {
            if (targetTransform == null)
            {
                return;
            }

            if (characterController != null)
            {
                characterController.enabled = false;
            }

            targetTransform.SetPositionAndRotation(worldPosition, worldRotation);

            if (characterController != null)
            {
                characterController.enabled = true;
            }
        }
    }
}

