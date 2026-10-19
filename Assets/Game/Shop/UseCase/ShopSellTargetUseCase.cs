using System.Collections.Generic;
using Game.Inventory;
using Game.Item;
using Game.Item.Parameter;
using Game.Item.UseCase;

namespace Game.Shop
{
    /// <summary>現在のインベントリから、スロット番号順の売却対象スナップショットを作る。</summary>
    public class ShopSellTargetUseCase
    {
        private readonly ItemDatabase itemDatabase;
        private readonly ItemSellPriceCalculatorUseCase sellPriceCalculator;

        public ShopSellTargetUseCase(ItemDatabase itemDatabase, ItemSellPriceCalculatorUseCase sellPriceCalculator)
        {
            this.itemDatabase = itemDatabase;
            this.sellPriceCalculator = sellPriceCalculator;
        }

        /// <summary>戻り値のインデックスはスロット番号と一致する。空スロットは null。</summary>
        public List<ShopSellTargetEntity?> BuildAll(InventoryEntity inventoryEntity)
        {
            var targets = new List<ShopSellTargetEntity?>();
            foreach (var slot in inventoryEntity.ItemSlots)
            {
                targets.Add(BuildFromSlot(slot, inventoryEntity.ActiveSlotIndex));
            }

            return targets;
        }

        public ShopSellTargetEntity? Build(InventoryEntity inventoryEntity, int slotIndex)
        {
            if (inventoryEntity.ItemSlots == null || slotIndex < 0 || slotIndex >= inventoryEntity.ItemSlots.Count)
            {
                return null;
            }

            return BuildFromSlot(inventoryEntity.ItemSlots[slotIndex], inventoryEntity.ActiveSlotIndex);
        }

        private ShopSellTargetEntity? BuildFromSlot(InventorySlotEntity slot, int activeSlotIndex)
        {
            if (!slot.Item.HasValue)
            {
                return null;
            }

            var item = slot.Item.Value;
            var itemParameter = itemDatabase != null ? itemDatabase.GetItem(item.ItemId) : null;
            var isSellableItem = itemParameter != null && itemParameter.IsSellable;
            var unitSellPrice = itemParameter != null
                ? sellPriceCalculator.Calculate(itemParameter.BaseSellPrice, item.Rarity, itemParameter.IsSellable)
                : 0u;
            var isEquippedWeapon = item.ItemType == InventoryItemType.Weapon && slot.SlotIndex == activeSlotIndex;

            return new ShopSellTargetEntity(
                slot.SlotIndex,
                item.InstanceId,
                item.ItemId,
                item.DisplayName,
                itemParameter != null ? itemParameter.Icon : null,
                item.Rarity,
                item.ItemType,
                item.Quantity,
                unitSellPrice,
                isSellableItem,
                isEquippedWeapon);
        }
    }
}
