using Game.Item;
using Game.Weapon;

namespace Game.Inventory
{
    public readonly struct InventoryItemEntity
    {
        public string InstanceId { get; }
        public string ItemId { get; }
        public string DisplayName { get; }
        public string IconId { get; }
        public InventoryItemType ItemType { get; }
        public uint Quantity { get; }
        public WeaponData WeaponData { get; }
        public ItemRarity Rarity { get; }

        /// <summary>レア度で決まる武器の攻撃力。負の値は「未指定」で、その場合は WeaponData.DefaultAttackPower を使う。</summary>
        public int AttackPower { get; }

        /// <summary>実際に使う攻撃力。武器以外は 0。</summary>
        public int EffectiveAttackPower
        {
            get
            {
                if (ItemType != InventoryItemType.Weapon)
                {
                    return 0;
                }

                if (AttackPower >= 0)
                {
                    return AttackPower;
                }

                return WeaponData != null ? WeaponData.DefaultAttackPower : 0;
            }
        }

        public InventoryItemEntity(
            string instanceId,
            string itemId,
            string displayName,
            string iconId,
            InventoryItemType itemType,
            uint quantity,
            WeaponData weaponData,
            ItemRarity rarity = ItemRarity.N,
            int attackPower = -1)
        {
            InstanceId = instanceId;
            ItemId = itemId;
            DisplayName = displayName;
            IconId = iconId;
            ItemType = itemType;
            Quantity = quantity;
            WeaponData = weaponData;
            Rarity = rarity;
            AttackPower = attackPower;
        }

        public InventoryItemEntity WithQuantity(uint quantity)
        {
            return new InventoryItemEntity(InstanceId, ItemId, DisplayName, IconId, ItemType, quantity, WeaponData, Rarity, AttackPower);
        }
    }
}