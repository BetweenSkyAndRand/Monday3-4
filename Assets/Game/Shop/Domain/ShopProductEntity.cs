using Game.Item;
using Game.Item.Parameter;
using UnityEngine;

namespace Game.Shop
{
    /// <summary>購入一覧に表示する商品1件分のスナップショット。</summary>
    public readonly struct ShopProductEntity
    {
        public ItemParameter ItemParameter { get; }
        public ItemRarity Rarity { get; }
        public uint UnitPrice { get; }

        public string ItemId => ItemParameter.ItemId;
        public string DisplayName => ItemParameter.ItemName;
        public Sprite Icon => ItemParameter.Icon;
        public bool IsWeapon => ItemParameter.Category == ItemCategory.Weapon;

        public ShopProductEntity(ItemParameter itemParameter, ItemRarity rarity, uint unitPrice)
        {
            ItemParameter = itemParameter;
            Rarity = rarity;
            UnitPrice = unitPrice;
        }
    }
}
