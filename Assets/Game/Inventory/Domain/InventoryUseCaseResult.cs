namespace Game.Inventory
{
    public readonly struct InventoryUseCaseResult
    {
        public bool IsSuccess { get; }
        public InventoryUseCaseError Error { get; }
        public string MessageKey { get; }

        public InventoryUseCaseResult(bool isSuccess, InventoryUseCaseError error, string messageKey)
        {
            IsSuccess = isSuccess;
            Error = error;
            MessageKey = messageKey;
        }

        public static InventoryUseCaseResult Success()
        {
            return new InventoryUseCaseResult(true, InventoryUseCaseError.None, string.Empty);
        }

        public static InventoryUseCaseResult Failed(InventoryUseCaseError error, string messageKey)
        {
            return new InventoryUseCaseResult(false, error, messageKey);
        }
    }
}

