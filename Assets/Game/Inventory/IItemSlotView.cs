using System;

namespace Game.Inventory
{
    public interface IItemSlotView
    {
        int SlotIndex { get; }

        void Initialize(int slotIndex);
        void SetButtonListener(Action<int> onClick);
        void ApplyViewModel(InventoryViewModel viewModel);
    }
}