using System;
using System.Collections.Generic;
using Game.Inventory.Parameter;
using UnityEngine;

namespace Game.Inventory.UseCase
{
    /// <summary>
    /// Minecraft スタイルのホットバー用 ViewModel を構築する。
    /// 装備専用スロットは出力せず、N個のアイテムスロットを順番に出力する。
    /// 選択中スロット（entity.ActiveSlotIndex）がハイライト表示される。
    /// </summary>
    public class InventoryBuildViewModelUseCase
    {
        public IReadOnlyList<InventoryViewModel> Execute(
            InventoryEntity entity,
            int selectedViewSlotIndex,
            InventoryDisplayParameter parameter,
            Func<string, Sprite> itemIconResolver = null)
        {
            var viewModels = new List<InventoryViewModel>();

            foreach (var slot in entity.ItemSlots)
            {
                var viewSlotIndex = slot.SlotIndex;
                viewModels.Add(BuildSlot(
                    viewSlotIndex,
                    slot.Item,
                    selectedViewSlotIndex == viewSlotIndex,
                    parameter,
                    itemIconResolver));
            }

            return viewModels;
        }

        private InventoryViewModel BuildSlot(
            int viewSlotIndex,
            InventoryItemEntity? item,
            bool isSelected,
            InventoryDisplayParameter parameter,
            Func<string, Sprite> itemIconResolver)
        {
            if (!item.HasValue)
            {
                return new InventoryViewModel(
                    viewSlotIndex,
                    false,
                    true,
                    InventoryItemType.None,
                    string.Empty,
                    "Empty",
                    parameter.EmptySlotSprite,
                    0,
                    isSelected);
            }

            var value = item.Value;
            var isEquippedSlot = isSelected && value.ItemType == InventoryItemType.Weapon;

            // アイコンの取得元は ItemParameter に統一する。
            // ItemParameter にアイコンが無い旧データのときだけ、従来の取得元へフォールバックする。
            var resolvedIcon = itemIconResolver?.Invoke(value.ItemId);
            if (resolvedIcon == null && value.ItemType == InventoryItemType.Weapon)
            {
                resolvedIcon = value.WeaponData?.InventoryIcon;
            }

            if (resolvedIcon == null)
            {
                resolvedIcon = parameter.ResolveIcon(value.ItemId);
            }

            var attackPower = value.EffectiveAttackPower;

            return new InventoryViewModel(
                viewSlotIndex,
                isEquippedSlot,
                false,
                value.ItemType,
                value.ItemId,
                value.DisplayName,
                resolvedIcon,
                value.Quantity,
                isSelected,
                attackPower,
                value.Rarity);
        }
    }
}

