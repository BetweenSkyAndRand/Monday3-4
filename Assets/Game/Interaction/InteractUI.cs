using Game.Item;
using Game.Item.Parameter;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

// ReSharper disable once CheckNamespace
namespace Game.Interaction
{
    /// <summary>
    /// インタラクト促進 UI の表示・非表示を管理するクラス。
    /// テキスト整形はこのクラスの責務とする（"[E] " プレフィックス付与）。
    /// PlayerInteract から Show/Hide を呼ばれる。
    /// Singleton として機能するが、シーン遷移時に古い参照が残らないよう
    /// OnDestroy で Instance をクリアする。
    /// </summary>
    public class InteractUI : MonoBehaviour
    {
        public static InteractUI Instance { get; private set; }

        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI text;

        [Header("アイテムカード（未設定でも文字だけで動作する）")]
        [Tooltip("アイコンを表示する Image。レア度の枠は子として自動生成される。")]
        [SerializeField] private Image iconImage;
        [Tooltip("数量を表示するテキスト。未設定のときは文字の末尾に数量を足す。")]
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private ItemRarityParameter rarityParameter;

        private void Awake()
        {
            Instance = this;

            if (root == null)
            {
                root = gameObject;
            }

            Hide();
        }

        private void OnDestroy()
        {
            // シーン遷移時に古い参照が残らないようにクリアする
            if (Instance == this)
            {
                Instance = null;
            }
        }

        public void Show(string message)
        {
            Show(message, null);
        }

        /// <summary>
        /// メッセージと、あればアイテムカード（アイコン・枠・数量）を表示する。
        /// 数量は 2 個以上のときだけ表示する。
        /// </summary>
        public void Show(string message, ItemIconDisplayData? displayData)
        {
            root.SetActive(true);

            var showQuantity = displayData.HasValue && displayData.Value.Quantity > 1;
            var quantityLabel = showQuantity ? $"x{displayData.Value.Quantity}" : string.Empty;

            // 数量用のテキストがない構成では、文字の末尾に足して情報を落とさない
            var appendQuantityToMessage = showQuantity && quantityText == null;
            text.text = appendQuantityToMessage ? $"[E] {message} {quantityLabel}" : "[E] " + message;

            if (quantityText != null)
            {
                quantityText.gameObject.SetActive(showQuantity);
                quantityText.text = quantityLabel;
            }

            ApplyIcon(displayData);
        }

        private void ApplyIcon(ItemIconDisplayData? displayData)
        {
            if (iconImage == null)
            {
                return;
            }

            var hasIcon = displayData.HasValue && displayData.Value.Icon != null;
            iconImage.gameObject.SetActive(hasIcon);
            if (!hasIcon)
            {
                return;
            }

            iconImage.sprite = displayData.Value.Icon;

            // プレースホルダー用に透明で置かれた Image でもアイコンが見えるよう、不透明の白に戻す
            iconImage.color = Color.white;
            iconImage.preserveAspect = true;

            // アイコンなしのときは枠も出さないため、枠の更新はアイコンありのときだけ行う
            var frameSprite = rarityParameter != null ? rarityParameter.GetFrameSprite(displayData.Value.Rarity) : null;
            RarityFrameOverlay.Apply(iconImage, frameSprite);
        }

        public void Hide()
        {
            root.SetActive(false);
        }
    }
}