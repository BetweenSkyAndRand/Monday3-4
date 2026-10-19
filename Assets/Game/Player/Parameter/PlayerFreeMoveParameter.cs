using UnityEngine;

namespace Game.Player.Parameter
{
    [CreateAssetMenu(
        fileName = "PlayerFreeMoveParameter",
        menuName = "Game/Player/Player Free Move Parameter")]
    public class PlayerFreeMoveParameter : ScriptableObject
    {
        [Header("移動")]
        [SerializeField] private float baseMoveSpeed = 2.0f;
        [SerializeField] private float moveThreshold = 0.05f;

        [Header("回転")]
        [SerializeField] private float rotationSpeed = 10.0f;

        [Header("接地")]
        [SerializeField] private float groundedStickVelocity = -2.0f;

        [Header("CharacterController")]
        [SerializeField] private float controllerRadius = 0.35f;
        [SerializeField] private float controllerHeight = 1.8f;
        [SerializeField] private float controllerSkinWidth = 0.03f;
        [SerializeField] private float controllerStepOffset = 0.1f;
        [SerializeField] private float controllerSlopeLimit = 45f;

        public float BaseMoveSpeed => baseMoveSpeed;
        public float MoveThreshold => moveThreshold;
        public float RotationSpeed => rotationSpeed;
        public float GroundedStickVelocity => groundedStickVelocity;
        public float ControllerRadius => controllerRadius;
        public float ControllerHeight => controllerHeight;
        public float ControllerSkinWidth => controllerSkinWidth;
        public float ControllerStepOffset => controllerStepOffset;
        public float ControllerSlopeLimit => controllerSlopeLimit;
    }
}

