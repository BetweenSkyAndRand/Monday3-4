using System.Collections.Generic;
using Game.Inventory;
using Game.Item;
using Game.Weapon;

namespace Game.GroundItem
{
    public readonly struct DeathDropEntry
    {
        public DeathDropEntry(int slotIndex, ItemStack itemStack)
        {
            SlotIndex = slotIndex;
            ItemStack = itemStack;
        }

        public int SlotIndex { get; }
        public ItemStack ItemStack { get; }
    }

    /// <summary>死亡時に地面へ落とす対象（初期武器を含むすべてのアイテム）を求める。</summary>
    public class PlayerDeathDropUseCase
    {
        public List<DeathDropEntry> Execute(InventoryEntity inventoryEntity)
        {
            var deathDropEntries = new List<DeathDropEntry>();
            foreach (var slot in inventoryEntity.ItemSlots)
            {
                if (!slot.Item.HasValue)
                {
                    continue;
                }

                var item = slot.Item.Value;
                deathDropEntries.Add(new DeathDropEntry(slot.SlotIndex, new ItemStack(item.ItemId, item.Quantity, item.Rarity)));
            }

            return deathDropEntries;
        }
    }
}