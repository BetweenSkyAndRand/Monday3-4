namespace Game.Inventory.UseCase
{
    /// <summary>
    /// Minecraft スタイルの「ホットバースロット選択」ユースケース。
    /// スロットの選択 = 装備（武器の場合）を意味し、装備専用スロットは存在しない。
    /// 選択中スロットに武器があれば、それが装備中の武器として扱われる。
    /// </summary>
    public class InventoryEquipUseCase
    {
        private readonly IInventoryRepository repository;

        public InventoryEquipUseCase(IInventoryRepository repository)
        {
            this.repository = repository;
        }

        /// <summary>
        /// 指定スロットを選択状態にする。武器・パッチ・空スロット問わず選択可能。
        /// 範囲外のインデックスを渡した場合は失敗を返す。
        /// </summary>
        public InventoryUseCaseResult SelectSlot(int itemSlotIndex)
        {
            var entity = repository.GetCurrent();
            if (itemSlotIndex < 0 || itemSlotIndex >= entity.ItemSlots.Count)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.InvalidSlotIndex, "inventory.error.slot.invalid");
            }

            var setResult = repository.TrySetActiveSlot(itemSlotIndex);
            if (!setResult.IsSuccess)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.RepositoryFailed, "inventory.error.repository");
            }

            repository.TryIncrementVersion();
            return InventoryUseCaseResult.Success();
        }

        /// <summary>
        /// 選択状態を解除する（未選択状態 = ActiveSlotIndex = -1）。
        /// </summary>
        public InventoryUseCaseResult DeselectSlot()
        {
            var setResult = repository.TrySetActiveSlot(-1);
            if (!setResult.IsSuccess)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.RepositoryFailed, "inventory.error.repository");
            }

            repository.TryIncrementVersion();
            return InventoryUseCaseResult.Success();
        }
    }
}

