namespace Game.Inventory
{
    public enum InventoryUseCaseError
    {
        None = 0,
        InventoryFull = 1,
        StackLimitReached = 2,
        DuplicateWeaponNotAllowed = 3,
        ItemNotFound = 4,
        InsufficientQuantity = 5,
        NotEquipable = 6,
        NothingEquipped = 7,
        NoEmptySlotForUnequip = 8,
        InvalidSlotIndex = 9,
        InvalidQuantity = 10,
        RepositoryFailed = 100
    }
}

