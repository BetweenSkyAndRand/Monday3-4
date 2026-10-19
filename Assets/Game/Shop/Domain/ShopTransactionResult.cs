namespace Game.Shop
{
    public readonly struct ShopTransactionResult
    {
        public bool IsSuccess { get; }
        public ShopTransactionError Error { get; }

        /// <summary>成功時の購入/売却の合計金額。</summary>
        public uint TotalPrice { get; }

        private ShopTransactionResult(bool isSuccess, ShopTransactionError error, uint totalPrice)
        {
            IsSuccess = isSuccess;
            Error = error;
            TotalPrice = totalPrice;
        }

        public static ShopTransactionResult Succeeded(uint totalPrice)
        {
            return new ShopTransactionResult(true, ShopTransactionError.None, totalPrice);
        }

        public static ShopTransactionResult Failed(ShopTransactionError error)
        {
            return new ShopTransactionResult(false, error, 0);
        }
    }
}
