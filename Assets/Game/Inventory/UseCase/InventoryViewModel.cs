using Game.Item;
using UnityEngine;

namespace Game.Inventory
{
    public readonly struct InventoryViewModel
    {
        public int SlotIndex { get; }
        public bool IsEquippedSlot { get; }
        public bool IsEmpty { get; }
        public InventoryItemType ItemType { get; }
        public string ItemId { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public uint Quantity { get; }
        public bool IsSelected { get; }

        /// <summary>武器の攻撃力（レア度で決まる有効値）。武器以外は 0。</summary>
        public int BaseAttackPower { get; }

        /// <summary>アイコン枠の表示に使うレア度。</summary>
        public ItemRarity Rarity { get; }

        public InventoryViewModel(
            int slotIndex,
            bool isEquippedSlot,
            bool isEmpty,
            InventoryItemType itemType,
            string itemId,
            string displayName,
            Sprite icon,
            uint quantity,
            bool isSelected,
            int baseAttackPower = 0,
            ItemRarity rarity = ItemRarity.N)
        {
            SlotIndex = slotIndex;
            IsEquippedSlot = isEquippedSlot;
            IsEmpty = isEmpty;
            ItemType = itemType;
            ItemId = itemId;
            DisplayName = displayName;
            Icon = icon;
            Quantity = quantity;
            IsSelected = isSelected;
            BaseAttackPower = baseAttackPower;
            Rarity = rarity;
        }
    }
}