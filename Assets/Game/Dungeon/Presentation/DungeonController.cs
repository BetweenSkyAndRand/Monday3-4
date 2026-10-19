using Game.BuildMode;
using Game.Player.Script;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// ダンジョン全体を管理するコントローラー。
    /// ビルドモードの状態監視・入力イベントの購読管理のみを直接担い、
    /// プレビュー表示は DungeonPreviewPresenter へ、確定配置は DungeonRoomInstaller へ委譲する。
    ///
    /// 責務:
    ///   - パラメーター・参照の初期化と検証
    ///   - ビルドモード状態の監視（Update）
    ///   - 入力イベント（OnBuildPlace）の購読/解除
    ///   - DungeonPreviewPresenter / DungeonRoomInstaller のコーディネーション
    /// </summary>
    public class DungeonController : MonoBehaviour
    {
        [Header("パラメーター ScriptableObject")]
        [SerializeField] private DungeonCellParameter cellParameter;
        [SerializeField] private DungeonRoomParameter roomParameter;

        [Header("プレファブ")]
        [SerializeField] private GameObject previewRoomPrefab;  // DungeonPreviewRoomView アタッチ済み
        [SerializeField] private GameObject placedRoomPrefab;   // DungeonPlacedRoomView アタッチ済み

        [Header("カメラ・入力")]
        [SerializeField] private Camera mainCamera;
        [SerializeField] private LayerMask groundMask;

        /// <summary>
        /// マウス座標・クリックイベントを受け取るための GameInputReader 参照。
        /// DungeonController が Mouse.current に直接依存しないようにするため使用する。
        /// </summary>
        [FormerlySerializedAs("inputReader")] [FormerlySerializedAs("buildInputReader")]
        [SerializeField] private GameInputReader gameInputReader;

        [Header("動的生成オブジェクトの親設定")]
        [Tooltip("生成される部屋の親Transform。未設定の場合は「===== LEVEL ===== > Dungeon」を自動検索します。")]
        [SerializeField] private Transform dungeonRoomsParent;

        /// <summary>外部クラスが読み取り専用で部屋データにアクセスするためのプロパティ。</summary>
        public IReadOnlyDungeonRoomRepository RoomRepository => roomInstaller?.RoomRepository;

        /// <summary>
        /// 拠点部屋（中央部屋）以外の部屋が確定配置されたときに発行されるイベント。
        /// SearchController がこれを購読し、新ダンジョンが生成されたことを検知する。
        /// </summary>
        public event System.Action OnRoomPlacedBeyondCentral;

        // 累計ダンジョン生成回数（部屋追加1回につき1回）。崩壊してもリセットしない。
        private readonly DungeonGenerationCountRepository generationCountRepository = new DungeonGenerationCountRepository();

        /// <summary>これまでにダンジョンを生成（部屋を追加）した累計回数。敵強化の基準値。</summary>
        public int GenerationCount => generationCountRepository.Count;

        /// <summary>
        /// 指定した部屋グリッド座標の確定済み View を取得する。
        /// ダンジョン崩壊システムなど、外部から特定の部屋の View を操作したい場合に使用する。
        /// </summary>
        public bool TryGetPlacedRoomView(Vector2Int roomGridPos, out DungeonPlacedRoomView view)
        {
            return roomInstaller.TryGetPlacedView(roomGridPos, out view);
        }

        /// <summary>
        /// 指定した部屋グリッド座標の部屋を完全に除去する（View の破棄 + リポジトリからの削除）。
        /// ダンジョン崩壊システムから、部屋の消滅処理として呼ばれる。
        /// </summary>
        public bool RemoveRoom(Vector2Int roomGridPos)
        {
            var removed = roomInstaller.RemoveRoom(roomGridPos);
            if (removed)
            {
                OnRoomRemoved?.Invoke(roomGridPos);
                OnPlacedRoomCountChanged?.Invoke(PlacedRoomCount);
            }

            return removed;
        }

        /// <summary>部屋が崩壊などで除去されたときに、その部屋のグリッド座標つきで発行される。</summary>
        public event System.Action<Vector2Int> OnRoomRemoved;

        /// <summary>配置できる部屋数の上限（中央の拠点部屋を除く）。</summary>
        public int MaxPlacedRoomCount => roomParameter != null ? roomParameter.MaxPlacedRoomCount : int.MaxValue;

        /// <summary>プレビュー位置の配置判定結果（配置不可の理由つき）。</summary>
        public DungeonPlacementResult PreviewPlacementResult =>
            previewPresenter != null ? previewPresenter.PlacementResult : DungeonPlacementResult.NoNeighbor;

        /// <summary>
        /// 配置済みの部屋数。拠点部屋（中央部屋）は含めない。
        /// 崩壊で部屋が消えた場合も減る。
        /// </summary>
        public int PlacedRoomCount
        {
            get
            {
                var count = 0;
                foreach (var room in roomInstaller.RoomRepository.GetAll())
                {
                    if (room.RoomGridPosition != Vector2Int.zero)
                    {
                        count++;
                    }
                }

                return count;
            }
        }

        /// <summary>配置済み部屋数が変化したときに、新しい部屋数つきで発行される。</summary>
        public event System.Action<int> OnPlacedRoomCountChanged;

        /// <summary>部屋パラメーター（外部の座標計算用に読み取り専用で公開）。</summary>
        public DungeonRoomParameter RoomParameter => roomParameter;

        /// <summary>セルパラメーター（外部の座標計算用に読み取り専用で公開）。</summary>
        public DungeonCellParameter CellParameter => cellParameter;

        // サブコンポーネント（純粋C#クラス）
        private DungeonRoomInstaller roomInstaller;
        private DungeonPreviewPresenter previewPresenter;

        // UseCases・Repository（Awake で生成し Start で previewPresenter に渡す）
        private DungeonPlacedRoomRepository innerRepository;
        private DungeonRoomPlacementUseCase placementUseCase;

        /// <summary>
        /// build input への購読状態。
        /// OnEnable と Start の両方から購読処理を呼ぶため、二重購読を防ぐ目的で保持する。
        /// </summary>
        private bool isSubscribedToBuildInput;
        private bool isBuildCancelRequested;

        // ──────────────────────────────────────────

        private void Awake()
        {
            // Awake でパラメーター null チェックを行い問題を早期検出する
            ValidateParameters();

            innerRepository  = new DungeonPlacedRoomRepository();
            placementUseCase = new DungeonRoomPlacementUseCase(
                innerRepository,
                roomParameter != null ? roomParameter.MaxPlacedRoomCount : int.MaxValue);
            var connectionUseCase = new DungeonRoomConnectionUseCase(innerRepository, roomParameter);

            // roomInstaller は Start でグループを決定してから開始させる
            // そのため、ここでは親を null で初期化し、Start で dungeonRoomsParent が確定したら更新する
            roomInstaller = new DungeonRoomInstaller(
                innerRepository,
                connectionUseCase,
                roomParameter,
                cellParameter,
                placedRoomPrefab,
                null  // 親は Start で設定
            );
        }

        private void Start()
        {
            if (mainCamera == null)
            {
                mainCamera = Camera.main;
            }

            if (gameInputReader == null)
            {
                gameInputReader = FindFirstObjectByType<GameInputReader>();
            }

            // dungeonRoomsParent が未設定の場合は自動検索する
            if (dungeonRoomsParent == null)
            {
                dungeonRoomsParent = FindDungeonParent();
                if (dungeonRoomsParent == null)
                {
                    UnityEngine.Debug.LogWarning("[DungeonController] dungeonRoomsParent が見つかりませんでした。生成される部屋はシーンルートに配置されます。");
                }
            }

            // roomInstaller に親 Transform を設定する
            roomInstaller.SetRoomsParent(dungeonRoomsParent);

            // previewPresenter は mainCamera / gameInputReader が確定した Start で生成する
            previewPresenter = new DungeonPreviewPresenter(
                placementUseCase,
                roomParameter,
                cellParameter,
                mainCamera,
                groundMask,
                gameInputReader,
                previewRoomPrefab
            );

            SubscribeBuildInputIfReady();

            if (roomInstaller.TryInstallCentralRoom())
            {
                // ミニマップなどが拠点部屋を表示できるよう、設置完了を通知する
                OnPlacedRoomCountChanged?.Invoke(PlacedRoomCount);
            }
        }

        private void Update()
        {
            // キャンセル入力は Esc のポーズ入力と同じフレームで処理されるため、
            // ポーズ側が「ビルドモード中」と判定し終えてから次フレームでビルドモードを終了する
            if (isBuildCancelRequested)
            {
                isBuildCancelRequested = false;
                if (BuildModeRepository.GetBuildMode() == BuildModeType.Building)
                {
                    BuildModeRepository.SetBuildMode(BuildModeType.None);
                }
            }

            if (BuildModeRepository.GetBuildMode() != BuildModeType.Building)
            {
                previewPresenter?.SetVisible(false);
                // ビルドモード終了時に位置キャッシュをリセットする。
                // これにより次回ビルドモード開始時に同じ座標でも必ず canPlace を再評価する。
                // （PlaceRoom() 直後に同位置でビルドモードを再開した場合の二重配置を防ぐ）
                previewPresenter?.ResetGridPos();
                return;
            }

            previewPresenter?.SetVisible(true);
            previewPresenter?.Refresh();
        }

        private void OnEnable()
        {
            SubscribeBuildInputIfReady();
        }

        private void OnDisable()
        {
            UnsubscribeBuildInputIfReady();
        }

        // ──────────────────────────────────────────
        // private
        // ──────────────────────────────────────────

        /// <summary>Inspector で注入されるパラメーター SO を検証する。</summary>
        private void ValidateParameters()
        {
            if (cellParameter == null)
            {
                UnityEngine.Debug.LogWarning("[DungeonController] DungeonCellParameter が未設定です。");
            }

            if (roomParameter == null)
            {
                UnityEngine.Debug.LogWarning("[DungeonController] DungeonRoomParameter が未設定です。");
            }
        }

        /// <summary>
        /// canPlace が true のとき RoomInstaller に確定配置を委譲し、ビルドモードを終了する。
        /// </summary>
        private void PlaceRoom()
        {
            // ビルドモード以外でのクリックによる誤配置を防ぐ
            if (BuildModeRepository.GetBuildMode() != BuildModeType.Building)
            {
                return;
            }

            if (previewPresenter == null || !previewPresenter.CanPlace || placementUseCase.IsRoomLimitReached())
            {
                return;
            }

            var success = roomInstaller.TryInstallRoom(previewPresenter.CurrentGridPos);
            if (!success)
            {
                return;
            }

            // 拠点部屋以外の部屋が確定配置されたことを通知する
            generationCountRepository.Increment();
            OnRoomPlacedBeyondCentral?.Invoke();
            OnPlacedRoomCountChanged?.Invoke(PlacedRoomCount);

            // 配置完了後にビルドモードを終了
            BuildModeRepository.SetBuildMode(BuildModeType.None);
            UnityEngine.Debug.Log("[DungeonController] 部屋を配置しました。ビルドモードを終了します。");
        }

        /// <summary>
        /// build input の購読に必要な参照が揃っている場合のみ購読する。
        /// OnEnable は Start より先に呼ばれるため、Start 側でも同じ処理を再実行する。
        /// </summary>
        private void SubscribeBuildInputIfReady()
        {
            if (isSubscribedToBuildInput)
            {
                return;
            }

            if (gameInputReader == null)
            {
                return;
            }

            gameInputReader.OnBuildPlace += PlaceRoom;
            gameInputReader.OnPause += HandleBuildCancelInput;
            isSubscribedToBuildInput = true;
        }

        /// <summary>
        /// 部屋数が上限に達しているときに限り、Esc でビルドモードをキャンセルできるようにする。
        /// </summary>
        private void HandleBuildCancelInput()
        {
            if (BuildModeRepository.GetBuildMode() != BuildModeType.Building)
            {
                return;
            }

            if (placementUseCase == null || !placementUseCase.IsRoomLimitReached())
            {
                return;
            }

            isBuildCancelRequested = true;
        }

        /// <summary>
        /// build input の購読を解除する。未購読時は何もしない。
        /// </summary>
        private void UnsubscribeBuildInputIfReady()
        {
            if (!isSubscribedToBuildInput)
            {
                return;
            }

            if (gameInputReader == null)
            {
                return;
            }

            gameInputReader.OnBuildPlace -= PlaceRoom;
            gameInputReader.OnPause -= HandleBuildCancelInput;
            isSubscribedToBuildInput = false;
        }

        /// <summary>
        /// ダンジョン生成オブジェクトの親 Transform を探す。
        /// 優先順位: "===== LEVEL ===== > Dungeon" > "Dungeon" > null
        /// </summary>
        private Transform FindDungeonParent()
        {
            // 1. "===== LEVEL =====" を探す
            var levelRoot = GameObject.Find("===== LEVEL =====");
            if (levelRoot != null)
            {
                var dungeonGroup = levelRoot.transform.Find("Dungeon");
                if (dungeonGroup != null)
                {
                    return dungeonGroup;
                }
            }

            // 2. フォールバック: "Dungeon" を直接探す
            var dungeonFallback = GameObject.Find("Dungeon");
            if (dungeonFallback != null)
            {
                return dungeonFallback.transform;
            }

            return null;
        }
    }
}
