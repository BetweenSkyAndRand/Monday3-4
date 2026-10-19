using Game.Interaction;
using Game.Search;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.BuildMode
{
    /// <summary>
    /// ビルドモード切り替えボタン。
    /// 責務: インタラクトのトリガー送信 と UI 表示ラベルの提供のみ。
    /// 状態管理は静的クラス BuildModeRepository に委譲する。
    /// 探索中（SearchPhase.InProgress）はビルドモードに入れないよう、
    /// SearchController.IsSearchInProgress を参照してインタラクトを無効化する。
    /// </summary>
    public class BuildModeToggleObject : MonoBehaviour, IInteractableObject
    {
        [SerializeField] private SearchController searchController;
        [SerializeField] private float cellSize = 1f; // セルサイズ（プレイヤーと同じ値を設定）

        [Header("UI表示設定")]
        [Tooltip("インタラクトテキスト設定 ScriptableObject")]
        [SerializeField] private InteractTextSettings textSettings;

        /// <summary>
        /// インタラクトUI に表示するラベル（フォールバック用）。
        /// </summary>
        private const string InteractLabel = "Build";

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
        /// インタラクトUI に表示するラベルを返す。
        /// 探索中またはビルドモード中は空文字を返して UI を非表示にする（インタラクト無効扱い）。
        /// </summary>
        public string GetInteractText()
        {
            // 探索中はビルドモードに入れない
            if (searchController != null && searchController.IsSearchInProgress)
            {
                return string.Empty;
            }

            // 既にビルドモード中の場合はトグルボタンを無効化
            if (BuildModeRepository.GetBuildMode() == BuildModeType.Building)
            {
                return string.Empty;
            }

            // textSettings が未設定の場合はフォールバック
            if (textSettings == null)
            {
                return InteractLabel;
            }

            return textSettings.buildModeToggleText;
        }

        /// <summary>プレイヤーにインタラクトされたら、ビルドモード状態を切り替える。</summary>
        public void Interact()
        {
            // 探索中はビルドモードに入れない（GetInteractText() が空文字を返すことで
            // PlayerInteract 側が currentInteractable を null にするため通常は呼ばれないが、
            // 念のため二重ガードを入れておく）
            if (searchController != null && searchController.IsSearchInProgress)
            {
                UnityEngine.Debug.LogWarning("[BuildModeToggleObject] 探索中はビルドモードに入れません。");
                return;
            }

            // 今の状態を取得
            var oldBuildMode = BuildModeRepository.GetBuildMode();

            // 既にビルドモード中の場合は何もしない（二重ガード）
            if (oldBuildMode == BuildModeType.Building)
            {
                UnityEngine.Debug.LogWarning("[BuildModeToggleObject] 既にビルドモード中です。ビルドモードを終了してから再度実行してください。");
                return;
            }

            // None から Building への切り替えのみを実行
            if (oldBuildMode != BuildModeType.None)
            {
                UnityEngine.Debug.LogWarning($"[BuildModeToggleObject] 予期しないビルドモード状態: {oldBuildMode}");
                return;
            }

            // 新しい状態を保存
            var currentBuildMode = BuildModeType.Building;
            BuildModeRepository.SetBuildMode(currentBuildMode);

            // UnityEngine.Debug.Log($"[BuildModeToggleObject] ビルドモード切り替え: {oldBuildMode} → {currentBuildMode}");
        }
    }
}

