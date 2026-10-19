using Game.Dungeon;
using Game.Utility;
using UnityEngine;
// ReSharper disable once CheckNamespace
// プロジェクト規約でフォルダ階層とネームスペース名を使用している (例: Game.Search, Game.Dungeon)
namespace Game.Search
{
    /// <summary>
    /// 探索の開始・終了イベントをトリガーし、登録済みハンドラーへ通知する。
    /// 責務：探索フェーズの遷移と各インターフェースへの通知のみ。
    /// 探索開始ガード：配置済み部屋が最初の１つのみ（Count <= 1）の場合は
    /// SearchEnd 時にトリガーを復活させない。
    /// </summary>
    public class SearchController : MonoBehaviour
    {
        [Header("ダンジョン参照（探索開始ガード用）")]
        [SerializeField] private DungeonController dungeonController;
        [Header("探索開始ハンドラー（ISearchStartHandler 実装）")]
        [SerializeField] private MonoBehaviour[] searchStartHandlerObjects;
        [Header("スポーン命令ハンドラー（ISpawnInstructionHandler 実装）")]
        [SerializeField] private MonoBehaviour[] spawnHandlerObjects;
        [Header("残敵クリーンアップ（IEnemyCleanupHandler 実装）")]
        [SerializeField] private MonoBehaviour enemyCleanupHandlerObject;
        [Header("探索開始トリガー（SearchEnd 時にリセットされる）")]
        [SerializeField] private SearchStartTriggerView[] searchStartTriggerViews;
        private SearchPhaseRepository searchPhaseRepository;
        private ISearchStartHandler[] searchStartHandlers;
        private ISpawnInstructionHandler[] spawnHandlers;
        private IEnemyCleanupHandler enemyCleanupHandler;

        /// <summary>
        /// 探索終了後に新しいダンジョンが生成されたかどうか。
        /// true のときだけ探索開始トリガーをリセットできる。
        /// </summary>
        private bool isDungeonBuiltForNextSearch;
        /// <summary>
        /// 探索フェーズが InProgress（探索中）かどうかを返す。
        /// SearchEndInteractableView がインタラクトUI表示時の判定に使用する。
        /// </summary>
        public bool IsSearchInProgress => searchPhaseRepository != null
            && searchPhaseRepository.GetPhase() == SearchPhase.InProgress;

        /// <summary>
        /// 探索が終了したときに発行されるイベント。
        /// ダンジョン崩壊システムなど、探索終了に合わせて状態をリセットしたいクラスが購読する。
        /// </summary>
        public event System.Action OnSearchEnded;
        private void Awake()
        {
            searchPhaseRepository = new SearchPhaseRepository();
            // MonoBehaviour[] を各インターフェース型にキャストして保持する。
            // Unity が interface SerializeField をサポートしないため、明示的にキャストを切り分け実行。
            // キャストロジックを MonoBehaviourCaster に委譲し、SearchController の責務は探索フェーズ管理に絞る。
            searchStartHandlers = MonoBehaviourCaster.CastArray<ISearchStartHandler>(searchStartHandlerObjects, nameof(SearchController));
            spawnHandlers       = MonoBehaviourCaster.CastArray<ISpawnInstructionHandler>(spawnHandlerObjects, nameof(SearchController));
            enemyCleanupHandler    = MonoBehaviourCaster.CastSingle<IEnemyCleanupHandler>(enemyCleanupHandlerObject, nameof(SearchController));
            if (searchStartTriggerViews == null || searchStartTriggerViews.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[SearchController] searchStartTriggerViews が設定されていません。SearchEnd 時にトリガーが復活しません。");
            }

            // DungeonController の部屋配置イベントを購読する。
            // ダンジョンが生成されたタイミングで探索開始可能状態にする。
            if (dungeonController != null)
            {
                dungeonController.OnRoomPlacedBeyondCentral += OnDungeonRoomPlaced;
            }
            else
            {
                UnityEngine.Debug.LogWarning("[SearchController] dungeonController が未設定です。ダンジョン生成の検知ができません。");
            }
        }

        private void Start()
        {
            // ゲーム開始時はダンジョン未生成のため、すべての探索開始トリガーを無効化する
            DisableAllTriggers();
        }

        private void OnDestroy()
        {
            if (dungeonController != null)
            {
                dungeonController.OnRoomPlacedBeyondCentral -= OnDungeonRoomPlaced;
            }
        }

        /// <summary>
        /// ダンジョンに新しい部屋が配置されたときに呼ばれる。
        /// 探索開始トリガーを有効化する。
        /// </summary>
        private void OnDungeonRoomPlaced()
        {
            if (isDungeonBuiltForNextSearch)
            {
                // 既にトリガーは有効化済みのため再実行しない
                return;
            }

            isDungeonBuiltForNextSearch = true;
            ResetAllTriggers();
        }

        /// <summary>
        /// すべての探索開始トリガーを有効化する。
        /// </summary>
        private void ResetAllTriggers()
        {
            if (searchStartTriggerViews == null || searchStartTriggerViews.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[SearchController] searchStartTriggerViews が空です。Inspector で割り当ててください。");
                return;
            }

            foreach (var triggerView in searchStartTriggerViews)
            {
                if (triggerView == null)
                {
                    UnityEngine.Debug.LogWarning("[SearchController] searchStartTriggerViews に null の要素があります。");
                    continue;
                }

                triggerView.ResetTrigger();
            }
        }

        /// <summary>
        /// すべての探索開始トリガーを無効化する。
        /// ダンジョン未生成のときに呼ばれる。
        /// </summary>
        private void DisableAllTriggers()
        {
            if (searchStartTriggerViews == null)
            {
                return;
            }

            foreach (var triggerView in searchStartTriggerViews)
            {
                if (triggerView == null)
                {
                    continue;
                }

                triggerView.DisableTrigger();
            }
        }
        /// <summary>
        /// 探索を開始する。多重呼び防止のためフェーズが NotStarted のときのみ処理する。
        /// </summary>
        public void RequestSearchStart()
        {
            if (searchPhaseRepository.GetPhase() != SearchPhase.NotStarted)
            {
                return;
            }
            searchPhaseRepository.SetPhase(SearchPhase.InProgress);
            foreach (var handler in searchStartHandlers)
            {
                handler.OnSearchStart();
            }
            // 何をどう出すかは即座に通知。出現場所などの詳細は実装クラスに委譲し、必要情報を取得する。
            foreach (var handler in spawnHandlers)
            {
                handler.OnSpawnInstruction();
            }
        }
        /// <summary>
        /// 探索を終了する。フェーズが InProgress のときのみ終了処理を実行する。
        /// </summary>
        public void RequestSearchEnd()
        {
            if (searchPhaseRepository.GetPhase() != SearchPhase.InProgress)
            {
                return;
            }
            ExecuteSearchEnd();
        }
        private void ExecuteSearchEnd()
        {
            searchPhaseRepository.SetPhase(SearchPhase.Ended);

            if (enemyCleanupHandler != null)
            {
                // 残敵をすべて削除
                enemyCleanupHandler.DestroyAllRemainingEnemies();
            }

            // ゲームオーバー判定は PlayerRevivalController（治療費の支払い可否）が管理する。

            // 終了後にフェーズをリセットし、次の探索に備える
            searchPhaseRepository.SetPhase(SearchPhase.NotStarted);
            // UnityEngine.Debug.Log("[SearchController] フェーズ → NotStartedにセット。");

            // 探索終了後はダンジョン未生成状態に戻す。
            // 新しいダンジョンが生成（OnDungeonRoomPlaced）されるまでトリガーを復活させない。
            isDungeonBuiltForNextSearch = false;
            DisableAllTriggers();

            // 崩壊システムなど、探索終了を購読するクラスへ通知する
            OnSearchEnded?.Invoke();
        }
    }
}
