namespace Game.Inventory
{
    /// <summary>
    /// Minecraftスタイルのホットバー型インベントリ。
    /// 装備専用スロットは持たず、N個のアイテムスロット + 現在選択中スロットのインデックスを保持する。
    /// 武器・パッチを任意のスロットに格納でき、選択中スロットの武器が「装備中」として扱われる。
    /// </summary>
    public class InventoryRepository : IInventoryRepository
    {
        private readonly InventoryItemEntity?[] itemSlots;
        private int activeSlotIndex;
        private uint version;

        public InventoryRepository(int itemSlotCount)
        {
            itemSlots = new InventoryItemEntity?[itemSlotCount];
            activeSlotIndex = -1;
            version = 0;
        }

        public InventoryEntity GetCurrent()
        {
            var snapshotSlots = new InventorySlotEntity[itemSlots.Length];
            for (var i = 0; i < itemSlots.Length; i++)
            {
                snapshotSlots[i] = new InventorySlotEntity(i, itemSlots[i]);
            }

            return new InventoryEntity(snapshotSlots, activeSlotIndex, version);
        }

        public InventoryRepoResult TrySetItemSlot(int itemSlotIndex, InventoryItemEntity? item)
        {
            if (!IsValidIndex(itemSlotIndex))
            {
                return InventoryRepoResult.Failed(InventoryRepoError.InvalidSlotIndex);
            }

            // 武器は1個までというルールは維持（Quantity=1のみ許可）
            if (item.HasValue && item.Value.ItemType == InventoryItemType.Weapon && item.Value.Quantity != 1)
            {
                return InventoryRepoResult.Failed(InventoryRepoError.OutOfRange);
            }

            itemSlots[itemSlotIndex] = item;
            return InventoryRepoResult.Success();
        }

        public InventoryRepoResult TryClearItemSlot(int itemSlotIndex)
        {
            if (!IsValidIndex(itemSlotIndex))
            {
                return InventoryRepoResult.Failed(InventoryRepoError.InvalidSlotIndex);
            }

            if (!itemSlots[itemSlotIndex].HasValue)
            {
                return InventoryRepoResult.Failed(InventoryRepoError.NoItem);
            }

            itemSlots[itemSlotIndex] = null;
            return InventoryRepoResult.Success();
        }

        public InventoryRepoResult TrySetActiveSlot(int itemSlotIndex)
        {
            // -1 は未選択状態として許可する
            if (itemSlotIndex != -1 && !IsValidIndex(itemSlotIndex))
            {
                return InventoryRepoResult.Failed(InventoryRepoError.InvalidSlotIndex);
            }

            activeSlotIndex = itemSlotIndex;
            return InventoryRepoResult.Success();
        }

        public InventoryRepoResult TryIncrementVersion()
        {
            version++;
            return InventoryRepoResult.Success();
        }

        private bool IsValidIndex(int itemSlotIndex)
        {
            return itemSlotIndex >= 0 && itemSlotIndex < itemSlots.Length;
        }
    }
}

