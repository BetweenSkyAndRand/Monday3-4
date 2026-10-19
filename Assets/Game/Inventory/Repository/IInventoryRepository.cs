namespace Game.Inventory
{
    public interface IInventoryRepository
    {
        InventoryEntity GetCurrent();

        /// <summary>指定スロットにアイテムを書き込む。null で空にできる。</summary>
        InventoryRepoResult TrySetItemSlot(int itemSlotIndex, InventoryItemEntity? item);

        /// <summary>指定スロットを空にする。空のスロットを指定した場合は NoItem を返す。</summary>
        InventoryRepoResult TryClearItemSlot(int itemSlotIndex);

        /// <summary>
        /// 選択中スロットのインデックスを更新する。-1 を渡すと未選択状態にできる。
        /// 範囲外の値を渡した場合は InvalidSlotIndex を返す。
        /// </summary>
        InventoryRepoResult TrySetActiveSlot(int itemSlotIndex);

        InventoryRepoResult TryIncrementVersion();
    }
}

