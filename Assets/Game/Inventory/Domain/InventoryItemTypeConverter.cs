using Game.Item;

namespace Game.Inventory
{
    public static class InventoryItemTypeConverter
    {
        public static InventoryItemType FromCategory(ItemCategory category)
        {
            switch (category)
            {
                case ItemCategory.Weapon:
                    return InventoryItemType.Weapon;
                case ItemCategory.Recovery:
                    return InventoryItemType.Recovery;
                case ItemCategory.SellMaterial:
                    return InventoryItemType.SellMaterial;
                default:
                    return InventoryItemType.Other;
            }
        }
    }
}