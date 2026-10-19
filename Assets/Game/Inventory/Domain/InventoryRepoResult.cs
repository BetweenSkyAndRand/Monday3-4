namespace Game.Inventory
{
    public readonly struct InventoryRepoResult
    {
        public bool IsSuccess { get; }
        public InventoryRepoError Error { get; }

        public InventoryRepoResult(bool isSuccess, InventoryRepoError error)
        {
            IsSuccess = isSuccess;
            Error = error;
        }

        public static InventoryRepoResult Success()
        {
            return new InventoryRepoResult(true, InventoryRepoError.None);
        }

        public static InventoryRepoResult Failed(InventoryRepoError error)
        {
            return new InventoryRepoResult(false, error);
        }
    }
}

