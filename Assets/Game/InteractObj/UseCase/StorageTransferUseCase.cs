using Game.Inventory;

namespace Game.InteractObj.UseCase
{
    /// <summary>
    /// ストレージへの預け入れ・引き出しロジックを担当する UseCase。
    /// StorageInventoryController から分離され、ビジネスロジックのみを担当する。
    /// </summary>
    public class StorageTransferUseCase
    {
        /// <summary>
        /// プレイヤーのアクティブスロットにあるアイテムを全部ストレージの指定スロットへ預ける。
        /// ストレージ側のスロットが使用中の場合は失敗する。
        /// </summary>
        public bool TryDeposit(
            IInventoryRepository storageRepository,
            IInventory playerInventory,
            int playerSlotIndex,
            int storageSlotIndex,
            InventoryItemEntity itemEntity)
        {
            var storageEntity = storageRepository.GetCurrent();
            if (storageSlotIndex >= storageEntity.ItemSlots.Count)
            {
                return false;
            }

            // ストレージの対象スロットが使用中ならキャンセル
            if (storageEntity.ItemSlots[storageSlotIndex].Item.HasValue)
            {
                UnityEngine.Debug.Log("[StorageTransferUseCase] ストレージのそのスロットは使用中です。");
                return false;
            }

            // ストレージスロットにアイテムをセット
            var setResult = storageRepository.TrySetItemSlot(storageSlotIndex, itemEntity);
            if (!setResult.IsSuccess)
            {
                UnityEngine.Debug.LogWarning($"[StorageTransferUseCase] ストレージへの書き込みに失敗しました: {setResult.Error}");
                return false;
            }

            storageRepository.TryIncrementVersion();

            // プレイヤーのスロットを消費（失敗した場合はロールバック）
            var consumeSucceeded = TryConsumeFromPlayerSlot(playerInventory, playerSlotIndex, itemEntity);
            if (!consumeSucceeded)
            {
                storageRepository.TryClearItemSlot(storageSlotIndex);
                storageRepository.TryIncrementVersion();
                return false;
            }

            UnityEngine.Debug.Log($"[StorageTransferUseCase] [{itemEntity.DisplayName}] をストレージ スロット{storageSlotIndex}へ預けました。");
            return true;
        }

        /// <summary>
        /// ストレージの指定スロットにあるアイテムをプレイヤーのインベントリへ引き出す。
        /// プレイヤーのインベントリに空きがない場合は失敗する。
        /// </summary>
        public bool TryWithdraw(
            IInventoryRepository storageRepository,
            IInventory playerInventory,
            int storageSlotIndex,
            out InventoryItemEntity itemEntity)
        {
            itemEntity = default;

            var storageEntity = storageRepository.GetCurrent();
            if (storageSlotIndex >= storageEntity.ItemSlots.Count)
            {
                return false;
            }

            var storageSlot = storageEntity.ItemSlots[storageSlotIndex];
            if (!storageSlot.Item.HasValue)
            {
                UnityEngine.Debug.Log("[StorageTransferUseCase] そのストレージスロットは空です。");
                return false;
            }

            itemEntity = storageSlot.Item.Value;

            // プレイヤーのインベントリへ追加
            var addResult = TryAddToPlayerInventory(playerInventory, itemEntity);
            if (!addResult.IsSuccess)
            {
                UnityEngine.Debug.LogWarning($"[StorageTransferUseCase] 引き出し失敗（プレイヤーのインベントリが満杯の可能性）: {addResult.Error}");
                return false;
            }

            // ストレージスロットを空にする
            storageRepository.TryClearItemSlot(storageSlotIndex);
            storageRepository.TryIncrementVersion();

            UnityEngine.Debug.Log($"[StorageTransferUseCase] [{itemEntity.DisplayName}] をプレイヤーへ引き出しました。");
            return true;
        }

        /// <summary>プレイヤーのスロットからアイテムを消費する。</summary>
        private static bool TryConsumeFromPlayerSlot(IInventory playerInventory, int playerSlotIndex, InventoryItemEntity itemEntity)
        {

            if (itemEntity.ItemType == InventoryItemType.Weapon)
            {
                var result = playerInventory.TryConsumeWeapon(playerSlotIndex);
                if (!result.IsSuccess)
                {
                    UnityEngine.Debug.LogWarning($"[StorageTransferUseCase] プレイヤーの武器消費に失敗（ロールバック）: {result.Error}");
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>プレイヤーのインベントリにアイテムを追加する。</summary>
        private static InventoryUseCaseResult TryAddToPlayerInventory(IInventory playerInventory, InventoryItemEntity itemEntity)
        {

            if (itemEntity.ItemType == InventoryItemType.Weapon && itemEntity.WeaponData != null)
            {
                return playerInventory.TryAddItem(InventoryItemSpecFactory.FromWeaponEntity(itemEntity), 1);
            }

            return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "storage.error.unknown_item_type");
        }
    }
}


