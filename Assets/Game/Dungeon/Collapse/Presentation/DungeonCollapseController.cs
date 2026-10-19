using Game.Enemy;
using Game.Player;
using Game.Search;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// ダンジョン崩壊システムを統括するコントローラー。
    /// 責務：
    ///   - 探索開始で崩壊キューを構築し、タイマー（待機→警告→崩壊）を管理する
    ///   - 各フェーズの切り替わりで View（発光・画面揺れ・SE・UI）を呼び出す
    ///   - 崩壊実行時にプレイヤー/敵の在室判定、Repository からの除去を DungeonController に委譲する
    /// 判定ロジック（部屋内判定）は DungeonRoomEntityFactory に、
    /// 配置済み部屋の削除は DungeonController → DungeonRoomInstaller → Repository に委譲し、
    /// このクラス自身はフェーズ管理とオーケストレーションのみを担う。
    /// </summary>
    public class DungeonCollapseController : MonoBehaviour, ISearchStartHandler
    {
        /// <summary>崩壊タイマーの内部フェーズ。</summary>
        private enum CollapsePhase
        {
            /// <summary>崩壊対象がない、または探索中でない。</summary>
            Idle,
            /// <summary>次の崩壊まで待機中（警告開始前）。</summary>
            WaitingForNext,
            /// <summary>警告演出中（崩壊直前）。</summary>
            Warning,
        }

        [Header("パラメーター")]
        [SerializeField] private DungeonCollapseParameter collapseParameter;

        [Header("参照")]
        [Tooltip("未設定の場合はシーンから自動検索する。")]
        [SerializeField] private DungeonController dungeonController;
        [Tooltip("未設定の場合はシーンから自動検索する。")]
        [SerializeField] private SearchController searchController;
        [Tooltip("未設定の場合は Player タグから自動検索する。")]
        [SerializeField] private Transform playerTransform;
        [Tooltip("警告演出・画面揺れの対象カメラ。未設定時は Camera.main を使用する。")]
        [SerializeField] private Camera targetCamera;

        private DungeonCollapseScheduleUseCase scheduleUseCase;
        private DungeonCollapseWarningDirectionUseCase warningDirectionUseCase;
        private DungeonCollapseQueueEntity collapseQueue;
        private CollapsePhase currentPhase = CollapsePhase.Idle;
        private float phaseTimer;
        private DungeonRoomWarningEffect activeWarningEffect;

        private void Awake()
        {
            scheduleUseCase = new DungeonCollapseScheduleUseCase();
            warningDirectionUseCase = new DungeonCollapseWarningDirectionUseCase();

            if (dungeonController == null)
            {
                dungeonController = FindFirstObjectByType<DungeonController>();
            }

            if (searchController == null)
            {
                searchController = FindFirstObjectByType<SearchController>();
            }

            if (playerTransform == null)
            {
                var playerObject = GameObject.FindGameObjectWithTag("Player");
                if (playerObject != null)
                {
                    playerTransform = playerObject.transform;
                }
            }

            if (searchController != null)
            {
                searchController.OnSearchEnded += HandleSearchEnded;
            }
            else
            {
                UnityEngine.Debug.LogWarning("[DungeonCollapseController] SearchController が見つかりません。探索終了時のリセットが機能しません。");
            }

            if (collapseParameter == null)
            {
                UnityEngine.Debug.LogWarning("[DungeonCollapseController] DungeonCollapseParameter が未設定です。崩壊システムは動作しません。");
            }

            if (dungeonController == null)
            {
                UnityEngine.Debug.LogWarning("[DungeonCollapseController] DungeonController が見つかりません。崩壊システムは動作しません。");
            }
        }

        private void OnDestroy()
        {
            if (searchController != null)
            {
                searchController.OnSearchEnded -= HandleSearchEnded;
            }
        }

        // ─── ISearchStartHandler ────────────────────────────────────────

        /// <summary>
        /// 探索開始時に SearchController から呼ばれる。
        /// 配置済み部屋から崩壊キューを構築し、最初の崩壊タイマーを開始する。
        /// </summary>
        public void OnSearchStart()
        {
            if (dungeonController == null || collapseParameter == null)
            {
                return;
            }

            collapseQueue = scheduleUseCase.Execute(dungeonController.RoomRepository);

            if (!collapseQueue.HasNext)
            {
                UnityEngine.Debug.Log("[DungeonCollapseController] 崩壊対象の部屋がありません。");
                currentPhase = CollapsePhase.Idle;
                return;
            }

            currentPhase = CollapsePhase.WaitingForNext;
            phaseTimer = collapseParameter.FirstDelaySeconds;
            UnityEngine.Debug.Log($"[DungeonCollapseController] 崩壊キューを構築しました（{collapseQueue.PendingRoomPositions.Length}部屋）。最初の崩壊まで {phaseTimer:F1} 秒。");
        }

        /// <summary>
        /// 探索終了時に呼ばれる。仕様どおり、キューとタイマーを完全リセットする。
        /// 崩壊による即死 → PlayerRespawnController → SearchController.RequestSearchEnd() という経路で、
        /// ExecuteCollapse() の実行中に再入呼び出しされることがある。
        /// そのためフェーズだけでなくキュー自体も空にし、AdvanceToNextRoom() が崩壊を再開できないようにする。
        /// </summary>
        private void HandleSearchEnded()
        {
            currentPhase = CollapsePhase.Idle;
            collapseQueue = DungeonCollapseQueueEntity.Create(System.Array.Empty<Vector2Int>());
            phaseTimer = 0f;
            StopActiveWarningEffects();

            // 探索終了時は、段階崩壊で残っている部屋を含めてダンジョンを全崩壊させる
            CollapseAllRemainingRooms();
            UnityEngine.Debug.Log("[DungeonCollapseController] 探索終了に伴い崩壊システムをリセットし、ダンジョンを全崩壊させました。");
        }

        /// <summary>
        /// 配置済みの部屋（中央拠点を除く）をすべて崩壊させる。
        /// 探索終了は「初期部屋にいるとき」か「プレイヤー死亡時」にしか起きない設計のため、
        /// プレイヤーの即死判定は行わない（ここで Kill すると探索終了が再入する恐れもある）。
        /// 敵は SearchController が先に全削除済みだが、念のため部屋内の敵も消す。
        /// </summary>
        private void CollapseAllRemainingRooms()
        {
            if (dungeonController == null || collapseParameter == null)
            {
                return;
            }

            var remainingTargets = scheduleUseCase.Execute(dungeonController.RoomRepository);
            var roomParameter = dungeonController.RoomParameter;
            var cellParameter = dungeonController.CellParameter;

            foreach (var targetRoomGridPos in remainingTargets.PendingRoomPositions)
            {
                Vector3 roomWorldCenter;
                if (dungeonController.TryGetPlacedRoomView(targetRoomGridPos, out var placedView) && placedView != null)
                {
                    roomWorldCenter = placedView.transform.position;
                }
                else
                {
                    roomWorldCenter = DungeonRoomEntityFactory.GetRoomWorldCenter(targetRoomGridPos, roomParameter, cellParameter);
                }

                DestroyEnemiesInsideRoom(targetRoomGridPos, roomParameter, cellParameter);
                RemoveGroundItemsInsideRoom(targetRoomGridPos, roomParameter, cellParameter);
                PlayCollapsePresentation(roomWorldCenter);
                dungeonController.RemoveRoom(targetRoomGridPos);
            }
        }

        private void Update()
        {
            if (currentPhase == CollapsePhase.Idle)
            {
                return;
            }

            if (!collapseQueue.HasNext)
            {
                currentPhase = CollapsePhase.Idle;
                return;
            }

            phaseTimer -= Time.deltaTime;

            switch (currentPhase)
            {
                case CollapsePhase.WaitingForNext:
                    if (phaseTimer <= collapseParameter.WarningDurationSeconds)
                    {
                        StartWarningPhase();
                    }
                    break;

                case CollapsePhase.Warning:
                    UpdateWarningPresentation();
                    if (phaseTimer <= 0f)
                    {
                        ExecuteCollapse();
                    }
                    break;
            }
        }

        /// <summary>
        /// 警告フェーズを開始する。対象部屋の発光・画面揺れ・警告音・警告UI（円環グロー）をすべて再生する。
        /// </summary>
        private void StartWarningPhase()
        {
            currentPhase = CollapsePhase.Warning;
            var targetRoomGridPos = collapseQueue.Current;

            // 対象部屋のみ発光させる
            if (dungeonController.TryGetPlacedRoomView(targetRoomGridPos, out var placedView) && placedView != null)
            {
                activeWarningEffect = placedView.GetComponent<DungeonRoomWarningEffect>();
                if (activeWarningEffect == null)
                {
                    activeWarningEffect = placedView.gameObject.AddComponent<DungeonRoomWarningEffect>();
                }

                activeWarningEffect.StartWarning(
                    collapseParameter.WarningGlowColor,
                    collapseParameter.WarningDurationSeconds,
                    collapseParameter.WarningPulseSpeed);
            }

            // 画面揺れ
            var shakeManager = Effect.ScreenShakeManager.Instance;
            if (shakeManager != null)
            {
                shakeManager.Shake(collapseParameter.WarningDurationSeconds, collapseParameter.ScreenShakeMagnitude, targetCamera);
            }

            // 警告SE
            if (collapseParameter.WarningSe != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    audioManager.PlaySe(collapseParameter.WarningSe);
                }
            }

            // 警告UI（画面周囲の円環グロー）
            var warningUiView = DungeonCollapseWarningUIView.Instance;
            if (warningUiView != null)
            {
                warningUiView.ShowWarning(collapseParameter);
            }

            // 開始フレームから正しい方向・色で表示されるよう、初回の更新をここで行う
            UpdateWarningPresentation();

            UnityEngine.Debug.Log($"[DungeonCollapseController] 部屋 {targetRoomGridPos} の崩壊警告を開始しました。");
        }

        /// <summary>
        /// 崩壊対象部屋の方向と距離を再計算し、警告UIへ反映する。
        /// プレイヤーが移動するため警告フェーズ中は毎フレーム呼ばれる。
        /// </summary>
        private void UpdateWarningPresentation()
        {
            var warningUiView = DungeonCollapseWarningUIView.Instance;
            if (warningUiView == null || playerTransform == null || !collapseQueue.HasNext)
            {
                return;
            }

            var targetRoomGridPos = collapseQueue.Current;
            var roomParameter = dungeonController.RoomParameter;
            var cellParameter = dungeonController.CellParameter;

            var roomWorldCenter = DungeonRoomEntityFactory.GetRoomWorldCenter(targetRoomGridPos, roomParameter, cellParameter);
            var isPlayerInside = DungeonRoomEntityFactory.IsPositionInRoom(playerTransform.position, targetRoomGridPos, roomParameter, cellParameter);

            var warningCamera = targetCamera != null ? targetCamera : Camera.main;
            var cameraRight = warningCamera != null ? warningCamera.transform.right : Vector3.right;

            var warning = warningDirectionUseCase.Execute(
                playerTransform.position,
                roomWorldCenter,
                cameraRight,
                isPlayerInside,
                collapseParameter.MaxWarningDistance);

            warningUiView.UpdateWarning(warning);
        }

        /// <summary>
        /// 崩壊を実行する。プレイヤー・敵の在室判定、Repository からの除去、次のタイマー設定までを行う。
        /// </summary>
        private void ExecuteCollapse()
        {
            var targetRoomGridPos = collapseQueue.Current;
            var roomParameter = dungeonController.RoomParameter;
            var cellParameter = dungeonController.CellParameter;

            StopActiveWarningEffects();

            Vector3 roomWorldCenter;
            if (dungeonController.TryGetPlacedRoomView(targetRoomGridPos, out var placedView) && placedView != null)
            {
                roomWorldCenter = placedView.transform.position;
            }
            else
            {
                roomWorldCenter = DungeonRoomEntityFactory.GetRoomWorldCenter(targetRoomGridPos, roomParameter, cellParameter);
            }

            KillPlayerIfInsideRoom(targetRoomGridPos, roomParameter, cellParameter);
            DestroyEnemiesInsideRoom(targetRoomGridPos, roomParameter, cellParameter);
            RemoveGroundItemsInsideRoom(targetRoomGridPos, roomParameter, cellParameter);
            PlayCollapsePresentation(roomWorldCenter);

            // 部屋を完全消滅させる（View破棄 + Repositoryからの除去）。
            // Remove 済みのため、ビルドフェーズに戻れば IsCellOccupied が false となり再配置可能になる。
            dungeonController.RemoveRoom(targetRoomGridPos);
            UnityEngine.Debug.Log($"[DungeonCollapseController] 部屋 {targetRoomGridPos} が崩壊しました。");

            AdvanceToNextRoom();
        }

        /// <summary>プレイヤーが対象部屋の中にいる場合、即死イベントを発火する。</summary>
        private void KillPlayerIfInsideRoom(Vector2Int targetRoomGridPos, DungeonRoomParameter roomParameter, DungeonCellParameter cellParameter)
        {
            if (playerTransform == null)
            {
                return;
            }

            if (!DungeonRoomEntityFactory.IsPositionInRoom(playerTransform.position, targetRoomGridPos, roomParameter, cellParameter))
            {
                return;
            }

            var lethalHandler = playerTransform.GetComponent<IPlayerLethalDamageHandler>()
                ?? playerTransform.GetComponentInParent<IPlayerLethalDamageHandler>()
                ?? playerTransform.GetComponentInChildren<IPlayerLethalDamageHandler>();

            if (lethalHandler != null)
            {
                UnityEngine.Debug.Log("[DungeonCollapseController] プレイヤーが崩壊部屋内にいたため即死させます。");
                lethalHandler.Kill();
            }
            else
            {
                UnityEngine.Debug.LogWarning("[DungeonCollapseController] IPlayerLethalDamageHandler が見つかりません。プレイヤーの即死処理をスキップします。");
            }
        }

        /// <summary>対象部屋の中にいる敵をすべて消滅させる（プレイヤー同様に消滅させる仕様）。</summary>
        private void DestroyEnemiesInsideRoom(Vector2Int targetRoomGridPos, DungeonRoomParameter roomParameter, DungeonCellParameter cellParameter)
        {
            var allEnemyHealthComponents = FindObjectsByType<EnemyVirusHealthComponent>(FindObjectsSortMode.None);
            foreach (var enemyHealth in allEnemyHealthComponents)
            {
                if (enemyHealth == null)
                {
                    continue;
                }

                if (!DungeonRoomEntityFactory.IsPositionInRoom(enemyHealth.transform.position, targetRoomGridPos, roomParameter, cellParameter))
                {
                    continue;
                }

                // uint.MaxValue のダメージで確実に撃破する。
                // 既存の OnDeath → EnemyVirusDeathComponent の消滅演出パイプラインにそのまま乗せる。
                // 崩壊で消える敵はドロップしない（地面アイテムも崩壊で消えるため）
                if (enemyHealth.TryGetComponent<EnemyDropComponent>(out var enemyDropComponent))
                {
                    enemyDropComponent.SuppressDrop();
                }

                enemyHealth.TakeDamage(uint.MaxValue);
            }
        }

        /// <summary>対象部屋の中にある地面アイテムをすべて削除する。</summary>
        private void RemoveGroundItemsInsideRoom(Vector2Int targetRoomGridPos, DungeonRoomParameter roomParameter, DungeonCellParameter cellParameter)
        {
            if (!Game.GroundItem.GroundItemRuntime.TryGetController(out var groundItemSpawnController))
            {
                return;
            }

            groundItemSpawnController.RemoveInBounds(
                worldPosition => DungeonRoomEntityFactory.IsPositionInRoom(worldPosition, targetRoomGridPos, roomParameter, cellParameter));
        }

        /// <summary>崩壊時のVFX・SEを再生する。</summary>
        private void PlayCollapsePresentation(Vector3 roomWorldCenter)
        {
            if (collapseParameter.CollapseVfxPrefab != null)
            {
                var vfxManager = Effect.VFXManager.Instance;
                if (vfxManager != null)
                {
                    vfxManager.PlayEffect(collapseParameter.CollapseVfxPrefab, roomWorldCenter, Quaternion.identity);
                }
            }

            if (collapseParameter.CollapseSe != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    audioManager.PlaySe(collapseParameter.CollapseSe);
                }
            }
        }

        /// <summary>
        /// キューを次に進め、探索が継続している場合のみ次の崩壊タイマーを開始する。
        /// ExecuteCollapse() 中のプレイヤー即死で探索終了（HandleSearchEnded）が走った場合は
        /// currentPhase が Idle になっているため、ここで崩壊を再開させない。
        /// </summary>
        private void AdvanceToNextRoom()
        {
            // 探索終了によるリセット済みなら、キューを進めず Idle のまま維持する
            if (currentPhase == CollapsePhase.Idle)
            {
                var resetWarningUiView = DungeonCollapseWarningUIView.Instance;
                if (resetWarningUiView != null)
                {
                    resetWarningUiView.HideWarning();
                }

                return;
            }

            collapseQueue = collapseQueue.Advance();

            if (collapseQueue.HasNext)
            {
                currentPhase = CollapsePhase.WaitingForNext;
                phaseTimer = collapseParameter.IntervalSeconds;
            }
            else
            {
                currentPhase = CollapsePhase.Idle;
                UnityEngine.Debug.Log("[DungeonCollapseController] 崩壊キューが空になりました。");
            }

            var warningUiView = DungeonCollapseWarningUIView.Instance;
            if (warningUiView != null)
            {
                warningUiView.HideWarning();
            }
        }

        /// <summary>発光演出・UIを停止する（崩壊実行時・探索終了リセット時に呼ぶ）。</summary>
        private void StopActiveWarningEffects()
        {
            if (activeWarningEffect != null)
            {
                activeWarningEffect.StopWarning();
                activeWarningEffect = null;
            }

            var warningUiView = DungeonCollapseWarningUIView.Instance;
            if (warningUiView != null)
            {
                warningUiView.HideWarning();
            }
        }
    }
}

