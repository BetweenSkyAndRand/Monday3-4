using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shop
{
    /// <summary>
    /// 商品行（左カラム）と所持品スロット（下部）の両方に使う、1マス分のView。
    /// 使わない参照（価格テキストなど）は Inspector で空のままにしてよい。
    /// 見た目の変更はこのスクリプトが付いたプレハブ側で行う。
    /// </summary>
    public class ShopEntryView : MonoBehaviour
    {
        [SerializeField] private Button entryButton;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image iconImage;
        [Tooltip("アイコンの上に重ねるレア度の枠。")]
        [SerializeField] private Image rarityFrameImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI priceText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [Tooltip("装備中の武器に表示するマーカー。")]
        [SerializeField] private GameObject equippedMarker;

        private int entryIndex;
        private Action<int> onClick;

        public void Initialize(int index, Action<int> onClickAction)
        {
            entryIndex = index;
            onClick = onClickAction;

            if (entryButton == null)
            {
                return;
            }

            entryButton.onClick.RemoveAllListeners();
            entryButton.onClick.AddListener(() => onClick?.Invoke(entryIndex));
        }

        public void SetContent(
            Sprite icon,
            Sprite rarityFrame,
            string displayName,
            string priceLabel,
            string quantityLabel,
            bool isEquipped)
        {
            SetImage(iconImage, icon);
            SetImage(rarityFrameImage, rarityFrame);
            SetText(nameText, displayName);
            SetText(priceText, priceLabel);
            SetText(quantityText, quantityLabel);

            if (equippedMarker != null)
            {
                equippedMarker.SetActive(isEquipped);
            }

            if (entryButton != null)
            {
                entryButton.interactable = true;
            }
        }

        public void SetEmpty()
        {
            SetContent(null, null, string.Empty, string.Empty, string.Empty, false);

            // 空スロットは選択できない
            if (entryButton != null)
            {
                entryButton.interactable = false;
            }
        }

        public void SetColor(Color color)
        {
            if (backgroundImage != null)
            {
                backgroundImage.color = color;
            }
        }

        private static void SetImage(Image targetImage, Sprite sprite)
        {
            if (targetImage == null)
            {
                return;
            }

            targetImage.sprite = sprite;
            targetImage.enabled = sprite != null;
            // 生成直後の sprite 変更が描画に反映されない場合に備えて再描画を促す
            targetImage.SetAllDirty();
        }

        private static void SetText(TextMeshProUGUI targetText, string text)
        {
            if (targetText != null)
            {
                targetText.text = text;
            }
        }
    }
}
