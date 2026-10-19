using System;
using Game.Inventory.Parameter;
using Game.Weapon;

namespace Game.Inventory.UseCase
{
    public class InventoryMutationUseCase
    {
        private readonly IInventoryRepository repository;
        private readonly InventoryDisplayParameter parameter;

        public InventoryMutationUseCase(IInventoryRepository repository, InventoryDisplayParameter parameter)
        {
            this.repository = repository;
            this.parameter = parameter;
        }

        public InventoryUseCaseResult TryAddWeapon(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "inventory.error.weapon.null");
            }

            var weaponItemId = weaponData.ItemId;
            var weaponSpec = new InventoryItemSpec(
                weaponItemId,
                weaponData.WeaponName,
                InventoryItemType.Weapon,
                1,
                weaponData,
                Game.Item.ItemRarity.N,
                weaponData.DefaultAttackPower);

            return TryAddItem(weaponSpec, 1);
        }

        /// <summary>
        /// 任意カテゴリのアイテムを追加する。同一 ItemId のスタックを上限まで埋め、残りを空きスロットへ置く。
        /// 全量を格納できない場合は何も変更せず InventoryFull を返す。
        /// </summary>
        public InventoryUseCaseResult TryAddItem(InventoryItemSpec itemSpec, uint quantity)
        {
            if (quantity == 0 || itemSpec.MaxStackSize <= 0 || itemSpec.ItemType == InventoryItemType.None)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.InvalidQuantity, "inventory.error.quantity.invalid");
            }

            // 武器は 1 スロット 1 個固定
            if (itemSpec.ItemType == InventoryItemType.Weapon && quantity != 1)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.InvalidQuantity, "inventory.error.quantity.invalid");
            }

            var entity = repository.GetCurrent();
            var maxStackSize = itemSpec.ItemType == InventoryItemType.Weapon ? 1u : (uint)itemSpec.MaxStackSize;
            var writePlans = BuildWritePlans(entity, itemSpec, quantity, maxStackSize);
            if (writePlans == null)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.InventoryFull, "inventory.error.full");
            }

            // 全量が入ることを確認してから書き込む（途中失敗で中途半端な状態を残さない）
            foreach (var writePlan in writePlans)
            {
                var writeResult = repository.TrySetItemSlot(writePlan.SlotIndex, writePlan.Item);
                if (!writeResult.IsSuccess)
                {
                    return InventoryUseCaseResult.Failed(InventoryUseCaseError.RepositoryFailed, "inventory.error.repository");
                }
            }

            repository.TryIncrementVersion();
            return InventoryUseCaseResult.Success();
        }

        public uint GetAddableQuantity(InventoryItemSpec itemSpec)
        {
            if (itemSpec.MaxStackSize <= 0 || itemSpec.ItemType == InventoryItemType.None)
            {
                return 0;
            }

            var maxStackSize = itemSpec.ItemType == InventoryItemType.Weapon ? 1u : (uint)itemSpec.MaxStackSize;
            var entity = repository.GetCurrent();
            ulong addableQuantity = 0;
            foreach (var slot in entity.ItemSlots)
            {
                if (!slot.Item.HasValue)
                {
                    addableQuantity += maxStackSize;
                    continue;
                }

                var existingItem = slot.Item.Value;
                if (existingItem.ItemId != itemSpec.ItemId || existingItem.Rarity != itemSpec.Rarity)
                {
                    continue;
                }

                addableQuantity += maxStackSize > existingItem.Quantity ? maxStackSize - existingItem.Quantity : 0;
            }

            return addableQuantity > uint.MaxValue ? uint.MaxValue : (uint)addableQuantity;
        }

        private readonly struct SlotWritePlan
        {
            public int SlotIndex { get; }
            public InventoryItemEntity Item { get; }

            public SlotWritePlan(int slotIndex, InventoryItemEntity item)
            {
                SlotIndex = slotIndex;
                Item = item;
            }
        }

        private static System.Collections.Generic.List<SlotWritePlan> BuildWritePlans(
            InventoryEntity entity,
            InventoryItemSpec itemSpec,
            uint quantity,
            uint maxStackSize)
        {
            var writePlans = new System.Collections.Generic.List<SlotWritePlan>();
            var remainingQuantity = quantity;

            // 既存スタックを先に埋める（武器は maxStackSize=1 のため対象外になる）
            for (var i = 0; i < entity.ItemSlots.Count && remainingQuantity > 0; i++)
            {
                var existingItem = entity.ItemSlots[i].Item;
                if (!existingItem.HasValue || existingItem.Value.ItemId != itemSpec.ItemId || existingItem.Value.Rarity != itemSpec.Rarity)
                {
                    continue;
                }

                var freeSpace = maxStackSize > existingItem.Value.Quantity ? maxStackSize - existingItem.Value.Quantity : 0;
                if (freeSpace == 0)
                {
                    continue;
                }

                var addedQuantity = Math.Min(freeSpace, remainingQuantity);
                writePlans.Add(new SlotWritePlan(i, existingItem.Value.WithQuantity(existingItem.Value.Quantity + addedQuantity)));
                remainingQuantity -= addedQuantity;
            }

            for (var i = 0; i < entity.ItemSlots.Count && remainingQuantity > 0; i++)
            {
                if (entity.ItemSlots[i].Item.HasValue)
                {
                    continue;
                }

                var placedQuantity = Math.Min(maxStackSize, remainingQuantity);
                var newItem = new InventoryItemEntity(
                    Guid.NewGuid().ToString("N"),
                    itemSpec.ItemId,
                    itemSpec.DisplayName,
                    itemSpec.ItemId,
                    itemSpec.ItemType,
                    placedQuantity,
                    itemSpec.WeaponData,
                    itemSpec.Rarity,
                    itemSpec.AttackPower);
                writePlans.Add(new SlotWritePlan(i, newItem));
                remainingQuantity -= placedQuantity;
            }

            return remainingQuantity == 0 ? writePlans : null;
        }

        public InventoryUseCaseResult TryConsumeWeapon(int itemSlotIndex)
        {
            var entity = repository.GetCurrent();
            if (itemSlotIndex < 0 || itemSlotIndex >= entity.ItemSlots.Count)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.InvalidSlotIndex, "inventory.error.slot.invalid");
            }

            var item = entity.ItemSlots[itemSlotIndex].Item;
            if (!item.HasValue)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "inventory.error.slot.empty");
            }

            if (item.Value.ItemType != InventoryItemType.Weapon)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.NotEquipable, "inventory.error.weapon.required");
            }

            return TryRemoveItemAt(itemSlotIndex);
        }

        public InventoryUseCaseResult TryRemoveItemAt(int itemSlotIndex)
        {
            var entity = repository.GetCurrent();
            if (itemSlotIndex < 0 || itemSlotIndex >= entity.ItemSlots.Count)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.InvalidSlotIndex, "inventory.error.slot.invalid");
            }

            if (!entity.ItemSlots[itemSlotIndex].Item.HasValue)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "inventory.error.slot.empty");
            }

            return TryRemoveItemAt(itemSlotIndex, entity.ItemSlots[itemSlotIndex].Item.Value.Quantity);
        }

        public InventoryUseCaseResult TryRemoveItemAt(int itemSlotIndex, uint quantity)
        {
            if (quantity == 0)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.InvalidQuantity, "inventory.error.quantity.invalid");
            }

            var entity = repository.GetCurrent();
            if (itemSlotIndex < 0 || itemSlotIndex >= entity.ItemSlots.Count)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.InvalidSlotIndex, "inventory.error.slot.invalid");
            }

            var item = entity.ItemSlots[itemSlotIndex].Item;
            if (!item.HasValue)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "inventory.error.slot.empty");
            }

            if (quantity > item.Value.Quantity)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.InsufficientQuantity, "inventory.error.quantity.insufficient");
            }

            InventoryRepoResult mutationResult;
            if (quantity == item.Value.Quantity)
            {
                mutationResult = repository.TryClearItemSlot(itemSlotIndex);
            }
            else
            {
                mutationResult = repository.TrySetItemSlot(itemSlotIndex, item.Value.WithQuantity(item.Value.Quantity - quantity));
            }

            if (!mutationResult.IsSuccess)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.RepositoryFailed, "inventory.error.repository");
            }

            repository.TryIncrementVersion();
            return InventoryUseCaseResult.Success();
        }
    }
}