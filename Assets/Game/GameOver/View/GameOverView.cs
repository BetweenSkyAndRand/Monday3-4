using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Game.Inventory;

// ReSharper disable once CheckNamespace
namespace Game.GameOver
{
    /// <summary>
    /// ゲームオーバーシーン（GameOverScene）の UI を管理するクラス。
    /// ・タイトルに戻るボタンの購読と遷移処理
    /// ・最終の持ち物表示（プレイヤーインベントリ＋拠点ストレージ）
    ///   → インベントリは松尾担当の実装完了後に接続する（現在は TODO プレースホルダー）
    ///   → インベントリが取得できない場合は inventoryText を非表示にする。
    /// </summary>
    public class GameOverView : MonoBehaviour
    {
        [Header("シーン遷移")]
        [SerializeField] private string titleSceneName = "TitleScene";

        [Header("ボタン")]
        [SerializeField] private Button returnToTitleButton;

        [Header("持ち物テキスト（未接続時は自動非表示）")]
        [SerializeField] private TMPro.TextMeshProUGUI inventoryText;

        [Header("フェード設定")]
        [SerializeField] private float fadeInDuration = 0.5f;

        // TODO: 松尾さんのインベントリ・ストレージ実装後に下記フィールドを接続する
        // [SerializeField] private Text storageText;

        private void Awake()
        {
            if (returnToTitleButton != null)
            {
                returnToTitleButton.onClick.AddListener(ReturnToTitle);
            }
            else
            {
                UnityEngine.Debug.LogWarning("[GameOverView] returnToTitleButton が未設定です。Inspector でアサインしてください。");
            }
        }

        private void OnDestroy()
        {
            if (returnToTitleButton != null)
            {
                returnToTitleButton.onClick.RemoveListener(ReturnToTitle);
            }
        }

        private void Start()
        {
            // ScreenEffectManager のフェードアウトで黒くなった画面をフェードインで解除する
            var screenEffectManager = Effect.ScreenEffectManager.Instance;
            if (screenEffectManager != null)
            {
                screenEffectManager.FadeIn(fadeInDuration);
            }

            DisplayFinalInventory();
        }

        /// <summary>
        /// タイトルシーンへ遷移する。
        /// </summary>
        private void ReturnToTitle()
        {
            SceneManager.LoadScene(titleSceneName);
        }

        /// <summary>
        /// 最終の持ち物を UI に表示する。
        /// インベントリが取得できない場合（未実装 or 空）は inventoryText を非表示にする。
        /// 松尾担当のインベントリ実装後に接続する。
        /// </summary>
        private void DisplayFinalInventory()
        {
            // TODO: IInventory / IStorageRepository から情報を取得して表示する
            // 現時点ではインベントリが未実装のため、取得できなかったとみなして非表示にする。
            var inventoryInfo = TryGetInventoryInfo();
            if (string.IsNullOrEmpty(inventoryInfo))
            {
                // 持ち物情報が取れなかった場合は inventoryText ごと非表示にする
                if (inventoryText != null)
                {
                    inventoryText.gameObject.SetActive(false);
                }

                UnityEngine.Debug.Log("[GameOverView] 持ち物情報が取得できなかったため inventoryText を非表示にしました。");
                return;
            }

            if (inventoryText != null)
            {
                inventoryText.text = inventoryInfo;
                inventoryText.gameObject.SetActive(true);
            }
        }

        /// <summary>
        /// インベントリ情報文字列を取得する。
        /// 未実装または空の場合は null / 空文字を返す。
        /// 松尾担当のインベントリ実装後にここで取得ロジックを実装する。
        /// </summary>
        private static string TryGetInventoryInfo()
        {
            if (!InventoryRuntime.TryGetSnapshot(out var entity))
            {
                return null;
            }

            var summaryUseCase = new FinalInventorySummaryUseCase();
            var summary = summaryUseCase.Execute(entity);
            return string.IsNullOrWhiteSpace(summary) ? null : summary;
        }
    }
}
