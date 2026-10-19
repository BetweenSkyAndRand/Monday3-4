namespace Game.Inventory
{
    public readonly struct InventorySlotEntity
    {
        public int SlotIndex { get; }
        public InventoryItemEntity? Item { get; }

        public InventorySlotEntity(int slotIndex, InventoryItemEntity? item)
        {
            SlotIndex = slotIndex;
            Item = item;
        }
    }
}

