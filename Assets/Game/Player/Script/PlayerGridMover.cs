using Game.Player;
using Game.Utility;
using UnityEngine;
namespace Game.Player.Script
{
    /// <summary>
    /// セル単位でプレイヤーを移動させるクラス。
    /// 入力は GameInputReader から受け取り、移動方向の解釈と補間移動を担当する。
    /// グリッド座標計算・障害物判定は GridMover（純粋C#クラス）に委譲する。
    /// 移動禁止判定は IMovementBlocker[] に委譲し、ビルドモードや攻撃状態を直接知らない設計にする。
    /// </summary>
    public class RogueMover : MonoBehaviour, IPlayerMover
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private GameInputReader inputReader;
        [SerializeField] private bool enableDebugLog = false;
        [SerializeField] private MonoBehaviour[] movementBlockerObjects;
        private GridMover gridMover;
        private bool isMoving;
        private Vector3 targetPos;
        private Vector3 startPos;
        private Vector2Int startGridPos;
        private Vector2Int inputDir;
        private IMovementBlocker[] movementBlockers;
        private Vector3 facingDirection = Vector3.forward;
        private Vector3 currentVelocity;
        private float moveStartTime;
        private const float MoveTimeout = 1.0f;
        public Vector2Int CurrentMoveDirection => inputDir;
        public Vector3 FacingDirection => facingDirection;
        public Vector3 CurrentVelocity => currentVelocity;
        public bool IsMoving => isMoving;
        public void Warp(Vector3 worldPosition, Quaternion worldRotation)
        {
            isMoving = false;
            inputDir = Vector2Int.zero;
            currentVelocity = Vector3.zero;
            UpdateFacingDirection(worldRotation * Vector3.forward);
            Game.Grid.GridOccupationManager.ReleaseByOccupant(gameObject);
            transform.SetPositionAndRotation(worldPosition, worldRotation);
            targetPos = worldPosition;
            var newGridPos = new Vector2Int(
                Mathf.RoundToInt(worldPosition.x / cellSize),
                Mathf.RoundToInt(worldPosition.z / cellSize)
            );
            if (gridMover != null)
            {
                gridMover.ForceSetGridPosition(newGridPos);
            }
            Game.Grid.GridOccupationManager.Occupy(newGridPos, gameObject);
            if (enableDebugLog)
            {
                UnityEngine.Debug.Log($"[RogueMover] Warp: {worldPosition} (グリッド: {newGridPos})");
            }
        }
        private void Start()
        {
            if (inputReader == null)
            {
                inputReader = FindFirstObjectByType<GameInputReader>();
            }
            movementBlockers = MonoBehaviourCaster.CastArray<IMovementBlocker>(movementBlockerObjects, nameof(RogueMover));
            var initialGrid = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / cellSize),
                Mathf.RoundToInt(transform.position.z / cellSize)
            );
            gridMover = new GridMover(
                cellSize,
                initialGrid,
                gameObject,
                new Game.Grid.PlayerMovementStrategy(),
                enableDebugLog);
            targetPos = transform.position;
        }
        private void OnDestroy()
        {
            Game.Grid.GridOccupationManager.ReleaseByOccupant(gameObject);
        }
        private void Update()
        {
            if (IsAnyMovementBlocked())
            {
                inputDir = Vector2Int.zero;
                currentVelocity = Vector3.zero;
                if (isMoving)
                {
                    CancelCurrentMove();
                }
                return;
            }
            ReadInput();
            if (!isMoving)
            {
                TryMove();
            }
            MoveProcess();
        }
        private bool IsAnyMovementBlocked()
        {
            if (movementBlockers == null || movementBlockers.Length == 0)
            {
                if (enableDebugLog)
                {
                    UnityEngine.Debug.LogWarning("[RogueMover] movementBlockers が空です（移動ブロッカーが設定されていません）");
                }
                return false;
            }
            foreach (var blocker in movementBlockers)
            {
                if (blocker == null)
                {
                    if (enableDebugLog)
                    {
                        UnityEngine.Debug.LogWarning("[RogueMover] movementBlockers に null が含まれています");
                    }
                    continue;
                }
                if (blocker.IsMovementBlocked)
                {
                    return true;
                }
            }
            return false;
        }
        private void ReadInput()
        {
            inputDir = Vector2Int.zero;
            if (inputReader == null)
            {
                return;
            }
            var raw = inputReader.MoveInput;
            var x = 0;
            var y = 0;
            if (raw.x > 0.5f)
            {
                x = 1;
            }
            else if (raw.x < -0.5f)
            {
                x = -1;
            }
            if (raw.y > 0.5f)
            {
                y = 1;
            }
            else if (raw.y < -0.5f)
            {
                y = -1;
            }
            inputDir = new Vector2Int(x, y);
            if (inputDir != Vector2Int.zero)
            {
                UpdateFacingDirection(new Vector3(inputDir.x, 0f, inputDir.y));
            }
        }
        private void TryMove()
        {
            if (inputDir == Vector2Int.zero)
            {
                return;
            }
            if (!gridMover.TryGetNextPos(inputDir, transform.position.y, out var worldPos))
            {
                return;
            }
            startPos = transform.position;
            startGridPos = gridMover.CurrentGridPos;
            var nextGrid = gridMover.GetNextGrid(inputDir);
            gridMover.ConfirmMove(nextGrid);
            targetPos = worldPos;
            isMoving = true;
            moveStartTime = Time.time;
        }
        private void CancelCurrentMove()
        {
            transform.position = startPos;
            gridMover.ForceSetGridPosition(startGridPos);
            Game.Grid.GridOccupationManager.ReleaseByOccupant(gameObject);
            Game.Grid.GridOccupationManager.Occupy(startGridPos, gameObject);
            isMoving = false;
            currentVelocity = Vector3.zero;
        }
        private void MoveProcess()
        {
            if (!isMoving)
            {
                currentVelocity = Vector3.zero;
                return;
            }
            var previousPosition = transform.position;
            if (Time.time - moveStartTime > MoveTimeout)
            {
                if (enableDebugLog)
                {
                    UnityEngine.Debug.LogWarning($"[RogueMover] 移動タイムアウト - 現在位置: {transform.position}, 目標位置: {targetPos}, 元の位置: {startPos}");
                }
                CancelCurrentMove();
                return;
            }
            transform.position = Vector3.MoveTowards(
                transform.position,
                targetPos,
                moveSpeed * Time.deltaTime
            );
            if (Vector3.Distance(transform.position, targetPos) < 0.01f)
            {
                transform.position = targetPos;
                isMoving = false;
            }
            currentVelocity = Time.deltaTime > 0f
                ? (transform.position - previousPosition) / Time.deltaTime
                : Vector3.zero;
            if (currentVelocity.sqrMagnitude > 0.0001f)
            {
                UpdateFacingDirection(currentVelocity);
            }
        }
        private void UpdateFacingDirection(Vector3 direction)
        {
            var planarDirection = new Vector3(direction.x, 0f, direction.z);
            if (planarDirection.sqrMagnitude <= 0f)
            {
                return;
            }
            facingDirection = planarDirection.normalized;
        }
    }
}
