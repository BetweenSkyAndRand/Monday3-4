using Game.Interaction;
using UnityEngine;

// ReSharper disable once CheckNamespace
// プロジェクト規約でフォルダ階層より短い名前空間を使用している (例: Game.Search)
namespace Game.Search
{
    /// <summary>
    /// 探索終了インタラクトビュー。
    /// 責務：インタラクトのトリガー送信と UI 表示ラベルの提供のみ。
    /// フェーズ管理は SearchController に委譲する。
    /// </summary>
    public class SearchEndInteractableView : MonoBehaviour, IInteractableObject
    {
        [SerializeField] private SearchController searchController;
        [SerializeField] private float cellSize = 1f; // セルサイズ（プレイヤーと同じ値を設定）

        [Tooltip("探索終了の確認ダイアログ。未設定なら確認なしで即終了する。")]
        [SerializeField] private SearchEndConfirmDialogView confirmDialog;

        [Header("UI表示設定")]
        [Tooltip("インタラクトテキスト設定 ScriptableObject")]
        [SerializeField] private InteractTextSettings textSettings;

        /// <summary>
        /// インタラクト UI に表示するラベル（フォールバック用）。
        /// </summary>
        private const string InteractLabel = "SearchEnd";

        private void Start()
        {
            // このオブジェクトがいるグリッド座標を占有する
            var gridPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / cellSize),
                Mathf.RoundToInt(transform.position.z / cellSize)
            );
            Game.Grid.GridOccupationManager.Occupy(gridPos, gameObject);
        }

        private void OnDestroy()
        {
            // グリッド占有を解放
            Game.Grid.GridOccupationManager.ReleaseByOccupant(gameObject);
        }

        /// <summary>
        /// インタラクト UI に表示するラベルを返す。
        /// 探索中でなければ空文字を返して UI を非表示にする（インタラクト無効扱い）。
        /// </summary>
        public string GetInteractText()
        {
            // 探索中でない場合はインタラクト不可
            if (searchController == null || !searchController.IsSearchInProgress)
            {
                return string.Empty;
            }

            // textSettings が未設定の場合はフォールバック
            if (textSettings == null)
            {
                return InteractLabel;
            }

            return textSettings.searchEndText;
        }

        /// <summary>
        /// プレイヤーにインタラクトされたら、探索終了を要求する。
        /// フェーズ（探索中か否か）のチェックは SearchController が行う。
        /// </summary>
        public void Interact()
        {
            if (searchController == null)
            {
                UnityEngine.Debug.LogError("[SearchEndInteractableView] searchController が設定されていません。");
                return;
            }

            if (confirmDialog == null)
            {
                searchController.RequestSearchEnd();
                return;
            }

            // ダイアログ表示中の再インタラクトは無視する
            if (SearchEndConfirmDialogView.IsOpen)
            {
                return;
            }

            confirmDialog.Open(searchController.RequestSearchEnd);
        }
    }
}

