using Game.Combat;
using Game.Enemy.UseCase;
using Game.Player.Script;
using UnityEngine;

namespace Game.Enemy.View
{
    /// <summary>
    /// 敵のAI行動を担当するコンポーネント。
    /// プレイヤーと同じ GridMover を使い、グリッド単位で移動する。
    /// 状態遷移は EnemyVirusActionComponent に委譲し、このクラスは移動と攻撃実行に集中する。
    /// IKnockbackHandlerを実装し、プレイヤーから吹き飛ばされた時の処理を担当する。
    /// グリッド補間移動は EnemyGridMover クラスに委譲する。
    /// </summary>
    public class EnemyVirusAIComponent : MonoBehaviour, Game.Grid.IKnockbackHandler
    {
        [Header("セル間の補間移動速度（PlayerGridMover の moveSpeed と同様）")]
        [SerializeField] private float moveSpeed = 4f;

        [Header("グリッドのセルサイズ（PlayerGridMover の cellSize と同値にすること）")]
        [SerializeField] private float cellSize = 1f;

        [Header("索敵設定")]
        [SerializeField] private LayerMask obstacleLayerMask;
        [SerializeField] private float lastSeenArrivalDistance = 0.5f;

        [Header("デバッグ")]
        [SerializeField] private bool enableDebugLog;

        private EnemyVirusActionComponent actionComponent;
        private Transform playerTransform;
        private GridMover gridMover;
        private EnemyGridMover enemyGridMover; // グリッド補間移動を担当
        private float viewRange;
        private float closeDetectionRange;
        private float loseSightRange;
        private float viewAngle;
        private float attackRange;
        private uint attackPower;
        private Vector3 lastSeenPlayerPosition;
        private bool hasLastSeenPlayerPosition;
        private bool isMovingToLastSeenPosition;
        private Collider enemyCollider;
        private Collider playerCollider;

        // 現在の移動方向（回転用）
        private Vector2Int currentMoveDirection;

        // Wandering用の徘徊タイマー
        private float wanderingTimer;
        private const float WanderingInterval = 1.5f; // 1.5秒ごとにランダム移動

        private const string PlayerTag = "Player";
        private const string WallLayerName = "wall";
        private const float DefaultViewRange = 4f;
        private const float DefaultCloseDetectionRange = 2f;
        private const float DefaultLoseSightRange = 6f;
        private const float DefaultViewAngle = 90f;

        /// <summary>
        /// 現在の移動方向を取得する。
        /// EnemyModelRotatorから参照される。
        /// </summary>
        public Vector2Int CurrentMoveDirection => currentMoveDirection;

        /// <summary>
        /// 現在移動中かどうかを取得する。
        /// </summary>
        public bool IsMoving => enemyGridMover != null && enemyGridMover.IsMoving;

        private void LogDebug(string message)
        {
            if (!enableDebugLog)
            {
                return;
            }

            UnityEngine.Debug.Log(message, this);
        }

        private void LogDebugWarning(string message)
        {
            if (!enableDebugLog)
            {
                return;
            }

            UnityEngine.Debug.LogWarning(message, this);
        }

        /// <summary>
        /// スポーン時に EnemyVirusSpawner から呼び出される初期化処理。
        /// エンティティから視野・攻撃範囲・攻撃力を取得してキャッシュする。
        /// </summary>
        public void Initialize(EnemyVirusEntity enemyEntity, uint attackPowerBonus = 0)
        {
            viewRange = enemyEntity.ViewRangeData?.ViewRange ?? DefaultViewRange;
            closeDetectionRange = enemyEntity.ViewRangeData?.CloseDetectionRange ?? DefaultCloseDetectionRange;
            loseSightRange = enemyEntity.ViewRangeData?.LoseSightRange ?? DefaultLoseSightRange;
            viewAngle = enemyEntity.ViewRangeData?.ViewAngle ?? DefaultViewAngle;
            attackRange = enemyEntity.WeaponEntity.AttackRangeData?.AttackRange ?? 1.5f;
            // 攻撃ダメージ = 敵本体の攻撃力（強化加算込み） + 敵の武器攻撃力
            attackPower = enemyEntity.BaseAttackPower + attackPowerBonus + enemyEntity.WeaponEntity.AttackPower;

            LogDebug($"[EnemyVirusAIComponent] Initialize完了 - viewRange: {viewRange}, closeDetectionRange: {closeDetectionRange}, loseSightRange: {loseSightRange}, viewAngle: {viewAngle}, attackRange: {attackRange}, attackPower: {attackPower}");
        }

        private void Awake()
        {
            // Awake 時点では actionComponent が未追加の場合がある（プレハブに事前アタッチされていない場合など）。
            // Start() でも再取得するため、ここで null でも問題ない。
            actionComponent = GetComponent<EnemyVirusActionComponent>();
            enemyCollider = GetComponent<Collider>();
            if (enemyCollider == null)
            {
                enemyCollider = GetComponentInChildren<Collider>();
            }

            if (obstacleLayerMask.value == 0)
            {
                obstacleLayerMask = LayerMask.GetMask(WallLayerName);
            }
        }

        private void Start()
        {
            // Awake 時に取得できなかった場合（プレハブ事前アタッチ + InitializeLogic で動的追加のケース）
            // Start() はすべての AddComponent が完了した後に呼ばれるため、ここで確実に取得できる。
            if (actionComponent == null)
            {
                actionComponent = GetComponent<EnemyVirusActionComponent>();
            }

            if (actionComponent == null)
            {
                UnityEngine.Debug.LogWarning("[EnemyVirusAIComponent] EnemyVirusActionComponent が見つかりません。", this);
            }

            // プレイヤーを Player タグで検索してキャッシュする
            var playerObject = GameObject.FindGameObjectWithTag(PlayerTag);
            if (playerObject == null)
            {
                UnityEngine.Debug.LogWarning("[EnemyVirusAIComponent] Player タグを持つオブジェクトが見つかりません。", this);
            }
            else
            {
                playerTransform = playerObject.transform;
                playerCollider = playerObject.GetComponent<Collider>();
                if (playerCollider == null)
                {
                    playerCollider = playerObject.GetComponentInChildren<Collider>();
                }
            }

            // 現在のワールド座標からグリッド座標を計算して GridMover を初期化する
            // （プレイヤーと同じ cellSize を使うことでグリッドが一致する）
            var initialGridX = Mathf.RoundToInt(transform.position.x / cellSize);
            var initialGridZ = Mathf.RoundToInt(transform.position.z / cellSize);
            var initialGrid = new Vector2Int(initialGridX, initialGridZ);

            // GridMover に cellSize・初期グリッド座標・プレイヤー自身・移動戦略を渡して生成する
            var movementStrategy = new Game.Grid.EnemyMovementStrategy();
            gridMover = new GridMover(cellSize, initialGrid, gameObject, movementStrategy, enableDebugLog);

            // EnemyGridMover を初期化（グリッド補間移動を委譲）
            enemyGridMover = new EnemyGridMover(transform, moveSpeed, enableDebugLog);

            LogDebug($"[EnemyVirusAIComponent] GridMover初期化 - Tag: {gameObject.tag}, 位置: {transform.position}, Y座標: {transform.position.y}, グリッド: {initialGrid}");
        }

        private void OnDestroy()
        {
            // グリッド占有を解放
            Game.Grid.GridOccupationManager.ReleaseByOccupant(gameObject);
        }

        private void Update()
        {
            if (actionComponent == null || !actionComponent.IsInitialized)
            {
                if (enableDebugLog && Time.frameCount % 300 == 0) // 5秒ごと
                {
                    UnityEngine.Debug.LogWarning($"[EnemyVirusAIComponent] 初期化未完了 - actionComponent: {actionComponent != null}, IsInitialized: {actionComponent?.IsInitialized}", this);
                }
                return;
            }

            if (actionComponent.CurrentState == EnemyVirusStateTransitionType.Dead)
            {
                return;
            }

            // 状態を定期的にログ出力（2秒ごと）
            if (enableDebugLog && Time.frameCount % 120 == 0)
            {
                var playerDist = playerTransform != null ? Vector3.Distance(transform.position, playerTransform.position) : -1;
                UnityEngine.Debug.Log($"[EnemyVirusAIComponent] 状態: {actionComponent.CurrentState}, IsMoving: {IsMoving}, プレイヤー距離: {playerDist:F2}", this);
            }

            switch (actionComponent.CurrentState)
            {
                case EnemyVirusStateTransitionType.Wandering:
                    UpdateWandering();
                    break;
                case EnemyVirusStateTransitionType.Detected:
                    UpdateDetected();
                    break;
                case EnemyVirusStateTransitionType.Chasing:
                    UpdateChasing();
                    break;
                case EnemyVirusStateTransitionType.Attacking:
                    UpdateAttacking();
                    break;
            }
        }

        /// <summary>
        /// 徘徊状態：プレイヤーが視野に入ったら Detected へ遷移する。
        /// プレイヤーが視野外の場合は、一定時間ごとにランダムな方向に移動する。
        /// </summary>
        private void UpdateWandering()
        {
            // プレイヤー検知（視野範囲内に入ったら発見状態へ）
            if (TryDetectPlayer())
            {
                LogDebug("[EnemyVirusAIComponent] プレイヤー発見 → Detected へ遷移");
                actionComponent.ForceTransitState(EnemyVirusStateTransitionType.Detected);
                return;
            }

            // 移動中は補間を続ける
            if (IsMoving)
            {
                enemyGridMover.UpdateMove();
                return;
            }

            // 一定時間ごとにランダムな方向に移動
            wanderingTimer += Time.deltaTime;
            if (wanderingTimer >= WanderingInterval)
            {
                wanderingTimer = 0f;
                TryMoveRandomDirection();
            }
        }

        /// <summary>
        /// ランダムな方向に1グリッド移動を試みる。
        /// </summary>
        private void TryMoveRandomDirection()
        {
            if (gridMover == null)
            {
                return;
            }

            // ランダムな方向を選択（上下左右のいずれか）
            var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
            var randomDirection = directions[Random.Range(0, directions.Length)];

            if (!gridMover.TryGetNextPos(randomDirection, transform.position.y, out var nextWorldPos))
            {
                // 移動できない場合は別の方向を試す
                foreach (var direction in directions)
                {
                    if (direction == randomDirection)
                    {
                        continue;
                    }

                    if (gridMover.TryGetNextPos(direction, transform.position.y, out nextWorldPos))
                    {
                        gridMover.ConfirmMove(gridMover.GetNextGrid(direction));
                        enemyGridMover.StartMove(nextWorldPos);
                        currentMoveDirection = direction;
                        LogDebug($"[EnemyVirusAIComponent] 徘徊移動開始（代替方向） - 方向: {direction}");
                        return;
                    }
                }

                LogDebug("[EnemyVirusAIComponent] 徘徊移動 - すべての方向が障害物");
                return;
            }

            gridMover.ConfirmMove(gridMover.GetNextGrid(randomDirection));
            enemyGridMover.StartMove(nextWorldPos);
            currentMoveDirection = randomDirection;
            LogDebug($"[EnemyVirusAIComponent] 徘徊移動開始 - 方向: {randomDirection}");
        }

        /// <summary>
        /// 発見状態：即座に Chasing へ遷移する。
        /// </summary>
        private void UpdateDetected()
        {
            actionComponent.ForceTransitState(EnemyVirusStateTransitionType.Chasing);
        }

        /// <summary>
        /// 追跡状態：GridMover を使ってグリッド単位でプレイヤー、または最後に見た位置へ近づく。
        /// RayCast が途切れた場合は最後に見た位置を目指し、再発見できなければ Wandering へ戻る。
        /// </summary>
        private void UpdateChasing()
        {
            if (playerTransform == null)
            {
                UnityEngine.Debug.LogWarning("[EnemyVirusAIComponent] UpdateChasing - playerTransformがnull", this);
                return;
            }

            if (CanContinueTrackingPlayer())
            {
                UpdateLastSeenPlayerPosition();
                isMovingToLastSeenPosition = false;

                if (IsPlayerInAttackRange())
                {
                    LogDebug("[EnemyVirusAIComponent] 攻撃範囲内 → Attacking へ遷移");
                    actionComponent.ForceTransitState(EnemyVirusStateTransitionType.Attacking);
                    return;
                }

                // 目標グリッドへの補間移動中は MoveTowards で近づき続ける
                if (IsMoving)
                {
                    enemyGridMover.UpdateMove();
                    return;
                }

                // 新しい移動方向を計算して GridMover で次セルを確定する
                TryMoveOneGridTowardTarget(playerTransform.position, "プレイヤー");
                return;
            }

            UpdateMoveToLastSeenPosition();
        }


        /// <summary>
        /// 指定した位置の方向に 1 グリッド分移動を試みる。
        /// GridMover の障害物判定を通過した場合のみ移動を確定する。
        /// 直進できない場合は、斜め方向（X方向とZ方向の両方）を試す。
        /// </summary>
        private void TryMoveOneGridTowardTarget(Vector3 targetWorldPosition, string targetLabel)
        {
            if (gridMover == null)
            {
                UnityEngine.Debug.LogWarning("[EnemyVirusAIComponent] TryMoveOneGridTowardTarget - gridMoverがnull", this);
                return;
            }

            var direction = GetGridDirectionToTarget(targetWorldPosition);
            if (direction == Vector2Int.zero)
            {
                if (enableDebugLog && Time.frameCount % 60 == 0)
                {
                    UnityEngine.Debug.Log($"[EnemyVirusAIComponent] TryMoveOneGridTowardTarget - 移動方向がzero（{targetLabel}に十分近い）", this);
                }
                return;
            }

            // まず優先方向を試す
            if (gridMover.TryGetNextPos(direction, transform.position.y, out var nextWorldPos))
            {
                gridMover.ConfirmMove(gridMover.GetNextGrid(direction));
                enemyGridMover.StartMove(nextWorldPos);
                currentMoveDirection = direction;
                LogDebug($"[EnemyVirusAIComponent] {targetLabel}への移動開始 - 方向: {direction}, 目標座標: {nextWorldPos}");
                return;
            }

            // 優先方向が障害物の場合、代替方向を試す
            if (enableDebugLog && Time.frameCount % 60 == 0)
            {
                LogDebugWarning($"[EnemyVirusAIComponent] 優先方向が障害物 - 方向: {direction}, 代替ルートを探索");
            }

            // プレイヤーへの差分を取得
            var diff = targetWorldPosition - transform.position;

            // X方向とZ方向の両方を試す（優先方向でない方）
            Vector2Int[] alternativeDirections;

            if (direction.x != 0)
            {
                // X方向が優先だった場合、Z方向を試す
                alternativeDirections = new[]
                {
                    diff.z > 0 ? Vector2Int.up : Vector2Int.down,
                    diff.z > 0 ? Vector2Int.down : Vector2Int.up,
                    direction.x > 0 ? Vector2Int.left : Vector2Int.right
                };
            }
            else
            {
                // Z方向が優先だった場合、X方向を試す
                alternativeDirections = new[]
                {
                    diff.x > 0 ? Vector2Int.right : Vector2Int.left,
                    diff.x > 0 ? Vector2Int.left : Vector2Int.right,
                    direction.y > 0 ? Vector2Int.down : Vector2Int.up
                };
            }

            // 代替方向を順番に試す
            foreach (var altDir in alternativeDirections)
            {
                if (gridMover.TryGetNextPos(altDir, transform.position.y, out nextWorldPos))
                {
                    gridMover.ConfirmMove(gridMover.GetNextGrid(altDir));
                    enemyGridMover.StartMove(nextWorldPos);
                    currentMoveDirection = altDir;
                    LogDebug($"[EnemyVirusAIComponent] {targetLabel}への代替ルート移動開始 - 方向: {altDir}, 目標座標: {nextWorldPos}");
                    return;
                }
            }

            // すべての方向が障害物の場合
            if (enableDebugLog && Time.frameCount % 120 == 0)
            {
                LogDebugWarning("[EnemyVirusAIComponent] すべての方向が障害物 - 移動できません");
            }
        }

        /// <summary>
        /// 指定した位置への方向を XZ 平面上の 4 方向グリッドベクトルに変換する。
        /// X と Z の差が小さく 1 セル未満の場合はゼロを返す（細かすぎる移動を抑制）。
        /// </summary>
        private Vector2Int GetGridDirectionToTarget(Vector3 targetWorldPosition)
        {
            var diff = targetWorldPosition - transform.position;

            // 1 セル以内の近接距離では新たな移動を行わない
            if (diff.sqrMagnitude < cellSize * cellSize * 0.25f)
            {
                // if (Time.frameCount % 60 == 0)
                // {
                //     UnityEngine.Debug.Log($"[EnemyVirusAIComponent] プレイヤーに十分近い (距離: {diff.magnitude:F2}) - 移動スキップ", this);
                // }
                return Vector2Int.zero;
            }

            // X と Z の距離が大きい方向に 1 セル移動する
            Vector2Int direction;
            if (Mathf.Abs(diff.x) >= Mathf.Abs(diff.z))
            {
                direction = diff.x > 0 ? Vector2Int.right : Vector2Int.left;
            }
            else
            {
                direction = diff.z > 0 ? Vector2Int.up : Vector2Int.down;
            }

            if (enableDebugLog && Time.frameCount % 30 == 0)
            {
                UnityEngine.Debug.Log($"[EnemyVirusAIComponent] 移動方向決定 - 敵位置: {transform.position}, 目標位置: {targetWorldPosition}, diff: ({diff.x:F2}, {diff.z:F2}), 方向: {direction}", this);
            }

            return direction;
        }

        /// <summary>
        /// 攻撃状態：クールタイムが経過していれば IDamageable にダメージを与える。
        /// プレイヤーが攻撃圏外へ出たら Chasing へ戻る。
        /// </summary>
        private void UpdateAttacking()
        {
            if (playerTransform == null)
            {
                return;
            }

            if (!CanContinueTrackingPlayer())
            {
                actionComponent.ForceTransitState(EnemyVirusStateTransitionType.Chasing);
                return;
            }

            if (!IsPlayerInAttackRange())
            {
                actionComponent.ForceTransitState(EnemyVirusStateTransitionType.Chasing);
                return;
            }

            if (!actionComponent.CanAttackNow())
            {
                return;
            }

            if (playerTransform.TryGetComponent<IDamageable>(out var damageable))
            {
                damageable.TakeDamage(attackPower);
            }

            actionComponent.NotifyAttackExecuted();
        }

        private void UpdateMoveToLastSeenPosition()
        {
            if (TryDetectPlayer())
            {
                LogDebug("[EnemyVirusAIComponent] プレイヤーを再発見したため追跡を継続");
                isMovingToLastSeenPosition = false;
                return;
            }

            if (!hasLastSeenPlayerPosition)
            {
                LogDebugWarning("[EnemyVirusAIComponent] 最後に見た位置が未設定のため Wandering へ戻します");
                isMovingToLastSeenPosition = false;
                actionComponent.ForceTransitState(EnemyVirusStateTransitionType.Wandering);
                return;
            }

            isMovingToLastSeenPosition = true;

            if (HasReachedLastSeenPosition())
            {
                LogDebug("[EnemyVirusAIComponent] 最後に見た位置に到達したが再発見できなかったため Wandering へ遷移");
                isMovingToLastSeenPosition = false;
                actionComponent.ForceTransitState(EnemyVirusStateTransitionType.Wandering);
                return;
            }

            if (IsMoving)
            {
                enemyGridMover.UpdateMove();
                return;
            }

            TryMoveOneGridTowardTarget(lastSeenPlayerPosition, "最後に見た位置");
        }

        private bool TryDetectPlayer()
        {
            if (playerTransform == null)
            {
                return false;
            }

            var distance = Vector3.Distance(transform.position, playerTransform.position);
            var isInCloseDetectionRange = distance <= closeDetectionRange;
            var isInNormalViewRange = distance <= viewRange;

            if (!isInCloseDetectionRange && !isInNormalViewRange)
            {
                return false;
            }

            if (!isInCloseDetectionRange && !IsPlayerWithinViewAngle())
            {
                return false;
            }

            if (!HasLineOfSightToPlayer())
            {
                return false;
            }

            UpdateLastSeenPlayerPosition();

            if (enableDebugLog && Time.frameCount % 30 == 0)
            {
                UnityEngine.Debug.Log($"[EnemyVirusAIComponent] プレイヤー検知成功 - 距離: {distance:F2}, 近接特例: {isInCloseDetectionRange}", this);
            }

            return true;
        }

        private bool CanContinueTrackingPlayer()
        {
            if (playerTransform == null)
            {
                return false;
            }

            var distance = Vector3.Distance(transform.position, playerTransform.position);
            if (distance > loseSightRange)
            {
                if (enableDebugLog && Time.frameCount % 60 == 0)
                {
                    UnityEngine.Debug.Log($"[EnemyVirusAIComponent] 見失い距離超過 - 距離: {distance:F2}, loseSightRange: {loseSightRange:F2}", this);
                }

                return false;
            }

            return HasLineOfSightToPlayer();
        }

        private bool IsPlayerWithinViewAngle()
        {
            if (playerTransform == null)
            {
                return false;
            }

            var toPlayer = playerTransform.position - transform.position;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.0001f)
            {
                return true;
            }

            var facingDirection = GetFacingDirection();
            var angle = Vector3.Angle(facingDirection, toPlayer.normalized);
            var isInViewAngle = angle <= viewAngle * 0.5f;

            if (enableDebugLog && Time.frameCount % 120 == 0)
            {
                UnityEngine.Debug.Log($"[EnemyVirusAIComponent] 視野角チェック - angle: {angle:F2}, viewAngle: {viewAngle:F2}, 範囲内: {isInViewAngle}", this);
            }

            return isInViewAngle;
        }

        private Vector3 GetFacingDirection()
        {
            if (currentMoveDirection != Vector2Int.zero)
            {
                return new Vector3(currentMoveDirection.x, 0f, currentMoveDirection.y).normalized;
            }

            var forward = transform.forward;
            forward.y = 0f;
            if (forward.sqrMagnitude >= 0.0001f)
            {
                return forward.normalized;
            }

            return Vector3.forward;
        }

        private bool HasLineOfSightToPlayer()
        {
            if (playerTransform == null)
            {
                return false;
            }

            var rayOrigin = GetEnemyCenterPosition();
            var rayTarget = GetPlayerCenterPosition();
            var rayVector = rayTarget - rayOrigin;
            var rayDistance = rayVector.magnitude;
            if (rayDistance <= 0.001f)
            {
                return true;
            }

            var hitWall = Physics.Raycast(
                rayOrigin,
                rayVector.normalized,
                out var hit,
                rayDistance,
                obstacleLayerMask,
                QueryTriggerInteraction.Ignore);

            if (enableDebugLog && Time.frameCount % 60 == 0)
            {
                var hitName = hitWall ? hit.collider.gameObject.name : "なし";
                UnityEngine.Debug.Log($"[EnemyVirusAIComponent] RayCastチェック - 距離: {rayDistance:F2}, 遮蔽: {hitWall}, ヒット: {hitName}", this);
            }

            return !hitWall;
        }

        private Vector3 GetEnemyCenterPosition()
        {
            if (enemyCollider != null)
            {
                return enemyCollider.bounds.center;
            }

            return transform.position;
        }

        private Vector3 GetPlayerCenterPosition()
        {
            if (playerCollider == null && playerTransform != null)
            {
                playerCollider = playerTransform.GetComponent<Collider>();
                if (playerCollider == null)
                {
                    playerCollider = playerTransform.GetComponentInChildren<Collider>();
                }
            }

            if (playerCollider != null)
            {
                return playerCollider.bounds.center;
            }

            return playerTransform != null ? playerTransform.position : Vector3.zero;
        }

        private void UpdateLastSeenPlayerPosition()
        {
            if (playerTransform == null)
            {
                return;
            }

            lastSeenPlayerPosition = playerTransform.position;
            hasLastSeenPlayerPosition = true;
        }

        private bool HasReachedLastSeenPosition()
        {
            if (!hasLastSeenPlayerPosition)
            {
                return false;
            }

            var enemyPositionXZ = new Vector2(transform.position.x, transform.position.z);
            var lastSeenPositionXZ = new Vector2(lastSeenPlayerPosition.x, lastSeenPlayerPosition.z);
            var distance = Vector2.Distance(enemyPositionXZ, lastSeenPositionXZ);

            return distance <= lastSeenArrivalDistance;
        }

        private bool IsPlayerInAttackRange()
        {
            if (playerTransform == null)
            {
                return false;
            }

            var distance = Vector3.Distance(transform.position, playerTransform.position);
            var inRange = distance <= attackRange;

            if (enableDebugLog && Time.frameCount % 120 == 0)
            {
                UnityEngine.Debug.Log($"[EnemyVirusAIComponent] 攻撃範囲チェック - 距離: {distance:F2}, 攻撃範囲: {attackRange:F2}, 範囲内: {inRange}", this);
            }

            return inRange;
        }

        /// <summary>
        /// プレイヤーから吹き飛ばされた時の処理（IKnockbackHandler実装）。
        /// プレイヤーから離れる方向に最大10グリッドまで移動する。
        /// </summary>
        /// <param name="fromPosition">プレイヤーの位置</param>
        /// <param name="cellSizeParam">グリッドのセルサイズ</param>
        public void OnKnockback(Vector3 fromPosition, float cellSizeParam)
        {
            if (gridMover == null)
            {
                UnityEngine.Debug.LogWarning("[EnemyVirusAIComponent] OnKnockback - gridMoverがnull", this);
                return;
            }

            // 現在のグリッド座標を取得
            var currentGrid = gridMover.CurrentGridPos;

            // プレイヤーのグリッド座標を計算
            var playerGrid = new Vector2Int(
                Mathf.RoundToInt(fromPosition.x / cellSize),
                Mathf.RoundToInt(fromPosition.z / cellSize)
            );

            // 吹き飛ばし方向を計算（プレイヤーから離れる方向）
            var knockbackDir = currentGrid - playerGrid;
            if (knockbackDir == Vector2Int.zero)
            {
                // 同じ位置にいる場合、ランダムな方向に吹き飛ばす
                var directions = new[] { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
                knockbackDir = directions[Random.Range(0, directions.Length)];
            }
            else
            {
                // ベクトルを正規化（上下左右のいずれかに）
                if (Mathf.Abs(knockbackDir.x) > Mathf.Abs(knockbackDir.y))
                {
                    knockbackDir = new Vector2Int(knockbackDir.x > 0 ? 1 : -1, 0);
                }
                else
                {
                    knockbackDir = new Vector2Int(0, knockbackDir.y > 0 ? 1 : -1);
                }
            }

            // ベクトル方向に最大10グリッドまで探索して、最遠の空きグリッドを見つける
            Vector2Int knockbackTarget = currentGrid;
            for (var distance = 1; distance <= 10; distance++)
            {
                var testGrid = currentGrid + knockbackDir * distance;

                // GridMoverを使って障害物チェック（1方向ずつ）
                if (!gridMover.TryGetNextPos(knockbackDir, transform.position.y, out _))
                {
                    // 障害物がある場合は前の位置で停止
                    break;
                }

                knockbackTarget = testGrid;
            }

            // 吹き飛ばし先に移動
            if (knockbackTarget != currentGrid)
            {
                // 占有状態を更新
                Game.Grid.GridOccupationManager.Release(currentGrid);
                Game.Grid.GridOccupationManager.Occupy(knockbackTarget, gameObject);

                // 実際に移動
                var knockbackWorldPos = new Vector3(
                    knockbackTarget.x * cellSize,
                    transform.position.y,
                    knockbackTarget.y * cellSize
                );

                transform.position = knockbackWorldPos;
                enemyGridMover.ResetTarget(); // 移動状態をリセット

                // GridMoverの内部グリッド位置を更新
                gridMover.ForceSetGridPosition(knockbackTarget);

                LogDebug($"[EnemyVirusAIComponent] 吹き飛ばし - {gameObject.name}: {currentGrid} → {knockbackTarget}, 方向: {knockbackDir}");
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawWireSphere(transform.position, viewRange);

            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, closeDetectionRange);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, loseSightRange);

            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);

            if (hasLastSeenPlayerPosition || isMovingToLastSeenPosition)
            {
                Gizmos.color = Color.green;
                Gizmos.DrawSphere(lastSeenPlayerPosition, 0.1f);
                Gizmos.DrawLine(transform.position, lastSeenPlayerPosition);
            }
        }
    }
}
