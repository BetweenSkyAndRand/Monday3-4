using System;
using Game.Inventory;

namespace Game.Shop
{
    /// <summary>
    /// 購入処理。事前検証 → 所持金を支払う → アイテムを追加、の順に確定し、
    /// アイテム追加に失敗した場合は支払った金額を戻す（ロールバック）。
    /// </summary>
    public class ShopPurchaseUseCase
    {
        private readonly IInventory inventory;
        private readonly IShopWallet wallet;

        public ShopPurchaseUseCase(IInventory inventory, IShopWallet wallet)
        {
            this.inventory = inventory;
            this.wallet = wallet;
        }

        /// <summary>所持金・インベントリの空き・武器の個数制限から、今回購入できる最大数量を返す。</summary>
        public uint GetMaxQuantity(ShopProductEntity product, uint maxPurchaseQuantityPerTransaction)
        {
            if (product.UnitPrice == 0)
            {
                return 0;
            }

            var affordableQuantity = wallet.CurrentMoney / product.UnitPrice;
            var addableQuantity = inventory.GetAddableQuantity(CreateSpec(product));
            var maxQuantity = Math.Min(Math.Min(affordableQuantity, addableQuantity), maxPurchaseQuantityPerTransaction);

            return product.IsWeapon ? Math.Min(maxQuantity, 1u) : maxQuantity;
        }

        /// <summary>状態を変更せずに、指定数量を購入できるか検証する。</summary>
        public ShopTransactionResult Validate(ShopProductEntity product, uint quantity)
        {
            if (product.ItemParameter == null || product.UnitPrice == 0)
            {
                return ShopTransactionResult.Failed(ShopTransactionError.InvalidProduct);
            }

            if (quantity == 0 || (product.IsWeapon && quantity != 1))
            {
                return ShopTransactionResult.Failed(ShopTransactionError.InvalidQuantity);
            }

            var totalPrice = (ulong)product.UnitPrice * quantity;
            if (totalPrice > wallet.CurrentMoney)
            {
                return ShopTransactionResult.Failed(ShopTransactionError.InsufficientMoney);
            }

            if (inventory.GetAddableQuantity(CreateSpec(product)) < quantity)
            {
                return ShopTransactionResult.Failed(ShopTransactionError.InventoryFull);
            }

            return ShopTransactionResult.Succeeded((uint)totalPrice);
        }

        public ShopTransactionResult Execute(ShopProductEntity product, uint quantity)
        {
            var validation = Validate(product, quantity);
            if (!validation.IsSuccess)
            {
                return validation;
            }

            var moneyBeforePurchase = wallet.CurrentMoney;
            if (!wallet.TrySpendMoney(validation.TotalPrice))
            {
                return ShopTransactionResult.Failed(ShopTransactionError.InsufficientMoney);
            }

            var addResult = inventory.TryAddItem(CreateSpec(product), quantity);
            if (addResult.IsSuccess)
            {
                return validation;
            }

            wallet.AddMoney(validation.TotalPrice);
            if (wallet.CurrentMoney != moneyBeforePurchase)
            {
                return ShopTransactionResult.Failed(ShopTransactionError.RollbackFailed);
            }

            return ShopTransactionResult.Failed(ShopTransactionError.InventoryFull);
        }

        private static InventoryItemSpec CreateSpec(ShopProductEntity product)
        {
            return InventoryItemSpecFactory.FromItemParameter(product.ItemParameter, product.Rarity);
        }
    }
}
