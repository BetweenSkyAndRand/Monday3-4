namespace Game.Shop
{
    public enum ShopTransactionError
    {
        None = 0,
        InvalidProduct = 1,
        InvalidQuantity = 2,
        InsufficientMoney = 3,
        InventoryFull = 4,
        TargetChanged = 5,
        NotSellable = 6,
        EquippedWeapon = 7,
        RollbackFailed = 8,
        Failed = 9
    }
}
