using TMPro;
using System;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory
{
    /// <summary>
    /// インベントリの1スロットのViewクラス
    /// </summary>
    public class ItemSlotView : MonoBehaviour, IItemSlotView
    {
        [SerializeField] private Image itemIcon;
        [SerializeField] private TextMeshProUGUI itemQuantityText;
        [SerializeField] private GameObject selectedHighlight;
        [SerializeField] private GameObject equippedMarker;
        [SerializeField] private Button slotButton;

        [Header("レア度の枠（アイコンの子として自動生成される）")]
        [SerializeField] private Game.Item.Parameter.ItemRarityParameter rarityParameter;

        [Header("武器攻撃力表示（左上・武器スロット専用）")]
        [Tooltip("武器の攻撃力を表示するテキスト。武器スロット左上に配置する。未アサインでも動作する。")]
        [SerializeField] private TextMeshProUGUI weaponAttackText;

        public int SlotIndex { get; private set; }

        private Action<int> onClick;

        public void Initialize(int slotIndex)
        {
            SlotIndex = slotIndex;
        }

        public void SetButtonListener(Action<int> onClickAction)
        {
            onClick = onClickAction;

            if (slotButton == null)
            {
                return;
            }

            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(() =>
            {
                if (onClick != null)
                {
                    onClick.Invoke(SlotIndex);
                }
            });
        }

        public void ApplyViewModel(InventoryViewModel viewModel)
        {
            if (itemIcon != null)
            {
                itemIcon.sprite = viewModel.Icon;
                itemIcon.enabled = viewModel.Icon != null;
                // SetActive(true) と同フレームの sprite 変更が描画に反映されないケースに対応するため、
                // Image を明示的にダーティ化して CanvasRenderer に再アップロードさせる。
                itemIcon.SetAllDirty();
            }

            UpdateRarityFrame(viewModel);

            if (itemQuantityText != null)
            {
                itemQuantityText.text = BuildQuantityLabel(viewModel);
            }

            if (selectedHighlight != null)
            {
                selectedHighlight.SetActive(viewModel.IsSelected);
            }

            if (equippedMarker != null)
            {
                equippedMarker.SetActive(viewModel.IsEquippedSlot && !viewModel.IsEmpty);
            }

            // 武器攻撃力テキストを更新する
            // 武器スロットのみ表示する
            UpdateWeaponAttackText(viewModel);
        }

        /// <summary>
        /// 武器攻撃力テキストを更新する。武器以外・空スロットは非表示。
        /// </summary>
        private void UpdateWeaponAttackText(InventoryViewModel viewModel)
        {
            if (weaponAttackText == null)
            {
                return;
            }

            if (viewModel.IsEmpty || viewModel.ItemType != InventoryItemType.Weapon)
            {
                weaponAttackText.gameObject.SetActive(false);
                return;
            }

            weaponAttackText.gameObject.SetActive(true);
            weaponAttackText.text = $"{viewModel.BaseAttackPower}";
        }

        private void UpdateRarityFrame(InventoryViewModel viewModel)
        {
            // 空スロットとアイコンなしのときは枠も出さない
            var hasIcon = !viewModel.IsEmpty && viewModel.Icon != null;
            var frameSprite = hasIcon && rarityParameter != null
                ? rarityParameter.GetFrameSprite(viewModel.Rarity)
                : null;

            Game.Item.RarityFrameOverlay.Apply(itemIcon, frameSprite);
        }

        private string BuildQuantityLabel(InventoryViewModel viewModel)
        {
            if (viewModel.IsEmpty)
            {
                return string.Empty;
            }

            if (viewModel.ItemType == InventoryItemType.Weapon)
            {
                return string.Empty;
            }

            return viewModel.Quantity > 0 ? $"x{viewModel.Quantity}" : string.Empty;
        }
    }
}
