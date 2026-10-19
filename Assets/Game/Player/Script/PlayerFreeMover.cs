using Game.Player;
using Game.Player.Parameter;
using Game.Utility;
using UnityEngine;

namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤーの自由移動を統括するプレゼンテーション層コンポーネント。
    /// 入力解決・CharacterController 移動・ランタイム状態更新・移動ブロック判定を束ねるが、
    /// 個別ロジックは専用クラスへ委譲して責務を限定する。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerFreeMover : MonoBehaviour, IPlayerMover
    {
        [Header("参照")]
        [SerializeField] private CharacterController characterController;
        [SerializeField] private GameInputReader inputReader;
        [SerializeField] private PlayerFreeMoveParameter playerFreeMoveParameter;

        [Header("移動ブロッカー")]
        [SerializeField] private MonoBehaviour[] movementBlockerObjects;

        [Header("デバッグ")]
        [SerializeField] private bool enableDebugLog;

        private IMovementBlocker[] movementBlockers;
        private PlayerFreeMoveInputReader playerFreeMoveInputReader;
        private PlayerFreeMoveMotor playerFreeMoveMotor;
        private PlayerFreeMoveState playerFreeMoveState;
        private bool hasLoggedMissingSetup;
        private Rigidbody attachedRigidbody;

        public Vector3 FacingDirection => playerFreeMoveState != null ? playerFreeMoveState.FacingDirection : Vector3.forward;
        public Vector3 CurrentVelocity => playerFreeMoveState != null ? playerFreeMoveState.CurrentVelocity : Vector3.zero;
        public bool IsMoving => playerFreeMoveState != null && playerFreeMoveState.IsMoving;

        private void Awake()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            attachedRigidbody = GetComponent<Rigidbody>();
            if (attachedRigidbody != null)
            {
                // 移動は CharacterController が行うため、物理演算による慣性や押し戻しを無効化する
                attachedRigidbody.isKinematic = true;
            }

            if (inputReader == null)
            {
                inputReader = GetComponent<GameInputReader>();
            }

            movementBlockers = MonoBehaviourCaster.CastArray<IMovementBlocker>(movementBlockerObjects, nameof(PlayerFreeMover));
            if (movementBlockers.Length == 0)
            {
                // アサイン漏れで BuildModeMovementBlocker などが効かなくならないよう、同じオブジェクトのブロッカーを自動で拾う
                movementBlockers = GetComponents<IMovementBlocker>();
                UnityEngine.Debug.LogWarning("[PlayerFreeMover] movementBlockerObjects が空のため、同じオブジェクトの IMovementBlocker を自動で使用します。Inspector でアサインしてください。", this);
            }
            playerFreeMoveInputReader = new PlayerFreeMoveInputReader();
            playerFreeMoveMotor = new PlayerFreeMoveMotor(characterController);
            playerFreeMoveState = new PlayerFreeMoveState();
            ApplyMovementParameter();
        }

        private void OnValidate()
        {
            if (characterController == null)
            {
                characterController = GetComponent<CharacterController>();
            }

            ApplyMovementParameter();
        }

        private void Update()
        {
            if (!CanProcessMovement())
            {
                return;
            }

            if (IsAnyMovementBlocked())
            {
                playerFreeMoveState.ResetVelocity();
                return;
            }

            var moveDirection = playerFreeMoveInputReader.ResolveMoveDirection(inputReader);
            if (enableDebugLog)
            {
                var rawInput = inputReader != null ? inputReader.MoveInput : Vector2.zero;
                UnityEngine.Debug.Log(
                    $"[PlayerFreeMover] RawInput={rawInput}, MoveDir={moveDirection}, dt={Time.deltaTime:F4}");
            }
            var currentVelocity = playerFreeMoveMotor.Move(
                moveDirection,
                playerFreeMoveParameter.BaseMoveSpeed,
                playerFreeMoveParameter.GroundedStickVelocity,
                Time.deltaTime);

            playerFreeMoveState.UpdateVelocity(currentVelocity, playerFreeMoveParameter.MoveThreshold);
            playerFreeMoveState.UpdateFacingDirection(currentVelocity, playerFreeMoveParameter.MoveThreshold);
        }

        public void Warp(Vector3 worldPosition, Quaternion worldRotation)
        {
            if (playerFreeMoveMotor == null)
            {
                return;
            }

            playerFreeMoveMotor.Warp(transform, worldPosition, worldRotation);
            playerFreeMoveState.ResetVelocity();
            playerFreeMoveState.ForceFacingDirection(worldRotation * Vector3.forward);

            if (enableDebugLog)
            {
                UnityEngine.Debug.Log($"[PlayerFreeMover] Warp: {worldPosition}");
            }
        }

        private bool CanProcessMovement()
        {
            if (characterController != null && inputReader != null && playerFreeMoveParameter != null)
            {
                return true;
            }

            if (!hasLoggedMissingSetup)
            {
                hasLoggedMissingSetup = true;
                UnityEngine.Debug.LogWarning("[PlayerFreeMover] CharacterController / GameInputReader / PlayerFreeMoveParameter の参照が不足しているため移動処理を開始できません。", this);
            }

            return false;
        }

        private void ApplyMovementParameter()
        {
            if (playerFreeMoveMotor == null)
            {
                if (characterController == null)
                {
                    return;
                }

                playerFreeMoveMotor = new PlayerFreeMoveMotor(characterController);
            }

            playerFreeMoveMotor.ApplyParameter(playerFreeMoveParameter);
        }

        private bool IsAnyMovementBlocked()
        {
            if (movementBlockers == null || movementBlockers.Length == 0)
            {
                return false;
            }

            foreach (var movementBlocker in movementBlockers)
            {
                if (movementBlocker == null)
                {
                    continue;
                }

                var isBlocked = movementBlocker.IsMovementBlocked;
                if (enableDebugLog)
                {
                    UnityEngine.Debug.Log(
                        $"[PlayerFreeMover] Blocker={movementBlocker.GetType().Name}, IsBlocked={isBlocked}");
                }

                if (isBlocked)
                {
                    if (enableDebugLog)
                    {
                        UnityEngine.Debug.Log(
                            $"[PlayerFreeMover] Movement blocked by {movementBlocker.GetType().Name}");
                    }
                    return true;
                }
            }

            return false;
        }
    }
}


