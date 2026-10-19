using Game.Item;
using Game.Weapon;

namespace Game.Inventory
{
    /// <summary>
    /// インベントリへ追加するアイテムの仕様。ItemParameter（ScriptableObject）から変換して使う。
    /// UseCase を ScriptableObject に依存させず、単体テストできるようにするための値。
    /// </summary>
    public readonly struct InventoryItemSpec
    {
        public string ItemId { get; }
        public string DisplayName { get; }
        public InventoryItemType ItemType { get; }
        public int MaxStackSize { get; }
        public WeaponData WeaponData { get; }
        public ItemRarity Rarity { get; }

        /// <summary>レア度で決まる武器の攻撃力。負の値は「未指定（WeaponData.DefaultAttackPower を使う）」。</summary>
        public int AttackPower { get; }

        public InventoryItemSpec(
            string itemId,
            string displayName,
            InventoryItemType itemType,
            int maxStackSize,
            WeaponData weaponData,
            ItemRarity rarity = ItemRarity.N,
            int attackPower = -1)
        {
            ItemId = itemId;
            DisplayName = displayName;
            ItemType = itemType;
            MaxStackSize = maxStackSize;
            WeaponData = weaponData;
            Rarity = rarity;
            AttackPower = attackPower;
        }
    }
}