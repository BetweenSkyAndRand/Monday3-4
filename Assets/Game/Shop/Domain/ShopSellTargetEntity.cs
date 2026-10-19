using Game.Inventory;
using Game.Item;
using UnityEngine;

namespace Game.Shop
{
    /// <summary>インベントリの1スロット分の、売却判定済みスナップショット。</summary>
    public readonly struct ShopSellTargetEntity
    {
        public int SlotIndex { get; }
        public string InstanceId { get; }
        public string ItemId { get; }
        public string DisplayName { get; }
        public Sprite Icon { get; }
        public ItemRarity Rarity { get; }
        public InventoryItemType ItemType { get; }
        public uint Quantity { get; }
        public uint UnitSellPrice { get; }
        public bool IsSellableItem { get; }
        public bool IsEquippedWeapon { get; }

        public bool CanSell => IsSellableItem && !IsEquippedWeapon;

        public ShopSellTargetEntity(
            int slotIndex,
            string instanceId,
            string itemId,
            string displayName,
            Sprite icon,
            ItemRarity rarity,
            InventoryItemType itemType,
            uint quantity,
            uint unitSellPrice,
            bool isSellableItem,
            bool isEquippedWeapon)
        {
            SlotIndex = slotIndex;
            InstanceId = instanceId;
            ItemId = itemId;
            DisplayName = displayName;
            Icon = icon;
            Rarity = rarity;
            ItemType = itemType;
            Quantity = quantity;
            UnitSellPrice = unitSellPrice;
            IsSellableItem = isSellableItem;
            IsEquippedWeapon = isEquippedWeapon;
        }
    }
}
