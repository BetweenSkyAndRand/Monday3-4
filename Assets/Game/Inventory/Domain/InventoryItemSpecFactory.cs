using Game.Item;
using Game.Item.Parameter;

namespace Game.Inventory
{
    public static class InventoryItemSpecFactory
    {
        /// <summary>ItemParameter とレア度から追加用の仕様を作る。攻撃力はレア度の行の値を使う。</summary>
        public static InventoryItemSpec FromItemParameter(ItemParameter itemParameter, ItemRarity rarity)
        {
            var itemType = InventoryItemTypeConverter.FromCategory(itemParameter.Category);
            var attackPower = itemType == InventoryItemType.Weapon ? itemParameter.GetAttackPower(rarity) : -1;

            return new InventoryItemSpec(
                itemParameter.ItemId,
                itemParameter.ItemName,
                itemType,
                itemParameter.MaxStackSize,
                itemParameter.WeaponData,
                rarity,
                attackPower);
        }

        /// <summary>
        /// 既存の武器エンティティから、レア度と攻撃力を保ったまま追加する仕様を作る。
        /// ストレージや宝箱からプレイヤーへ戻すときに使う（武器は常にスタック数 1）。
        /// </summary>
        public static InventoryItemSpec FromWeaponEntity(InventoryItemEntity weaponEntity)
        {
            return new InventoryItemSpec(
                weaponEntity.ItemId,
                weaponEntity.DisplayName,
                InventoryItemType.Weapon,
                1,
                weaponEntity.WeaponData,
                weaponEntity.Rarity,
                weaponEntity.AttackPower);
        }
    }
}