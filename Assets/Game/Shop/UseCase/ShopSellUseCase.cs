using Game.Inventory;

namespace Game.Shop
{
    /// <summary>
    /// 売却処理。画面で選んだ対象を実行時に現在のインベントリと再照合してから、
    /// アイテムを取り除き、所持金を加算する。
    /// </summary>
    public class ShopSellUseCase
    {
        private readonly IInventory inventory;
        private readonly IShopWallet wallet;
        private readonly ShopSellTargetUseCase sellTargetUseCase;

        public ShopSellUseCase(IInventory inventory, IShopWallet wallet, ShopSellTargetUseCase sellTargetUseCase)
        {
            this.inventory = inventory;
            this.wallet = wallet;
            this.sellTargetUseCase = sellTargetUseCase;
        }

        /// <summary>状態を変更せずに、選択した対象を指定数量だけ売却できるか検証する。</summary>
        public ShopTransactionResult Validate(ShopSellTargetEntity selectedTarget, uint quantity)
        {
            var currentTarget = sellTargetUseCase.Build(inventory.GetCurrentInventory(), selectedTarget.SlotIndex);
            if (!currentTarget.HasValue || !IsSameTarget(selectedTarget, currentTarget.Value))
            {
                return ShopTransactionResult.Failed(ShopTransactionError.TargetChanged);
            }

            var target = currentTarget.Value;
            if (!target.IsSellableItem)
            {
                return ShopTransactionResult.Failed(ShopTransactionError.NotSellable);
            }

            if (target.IsEquippedWeapon)
            {
                return ShopTransactionResult.Failed(ShopTransactionError.EquippedWeapon);
            }

            if (quantity == 0 || quantity > target.Quantity)
            {
                return ShopTransactionResult.Failed(ShopTransactionError.InvalidQuantity);
            }

            // 所持金の上限処理は所持金側（お金システム）に任せ、ここでは uint の範囲に収めるだけにする
            var totalPrice = (ulong)target.UnitSellPrice * quantity;
            var clampedTotalPrice = totalPrice > uint.MaxValue ? uint.MaxValue : (uint)totalPrice;
            return ShopTransactionResult.Succeeded(clampedTotalPrice);
        }

        public ShopTransactionResult Execute(ShopSellTargetEntity selectedTarget, uint quantity)
        {
            var validation = Validate(selectedTarget, quantity);
            if (!validation.IsSuccess)
            {
                return validation;
            }

            var removeResult = inventory.TryRemoveItemAt(selectedTarget.SlotIndex, quantity);
            if (!removeResult.IsSuccess)
            {
                return ShopTransactionResult.Failed(ShopTransactionError.Failed);
            }

            wallet.AddMoney(validation.TotalPrice);
            return validation;
        }

        private static bool IsSameTarget(ShopSellTargetEntity selectedTarget, ShopSellTargetEntity currentTarget)
        {
            return selectedTarget.SlotIndex == currentTarget.SlotIndex
                && selectedTarget.InstanceId == currentTarget.InstanceId
                && selectedTarget.ItemId == currentTarget.ItemId
                && selectedTarget.Rarity == currentTarget.Rarity;
        }
    }
}
