using System;

namespace Game.Inventory
{
    public static class InventoryRuntime
    {
        private static IInventory activeInventory;
        private static InventoryEntity latestSnapshot;
        private static bool hasSnapshot;

        public static void SetActiveInventory(IInventory inventory)
        {
            activeInventory = inventory;
        }

        public static void ClearActiveInventory(IInventory inventory)
        {
            if (ReferenceEquals(activeInventory, inventory))
            {
                activeInventory = null;
            }
        }

        public static bool TryGetInventory(out IInventory inventory)
        {
            inventory = activeInventory;
            return inventory != null;
        }

        public static void UpdateSnapshot(InventoryEntity entity)
        {
            latestSnapshot = entity;
            hasSnapshot = true;
        }

        public static bool TryGetSnapshot(out InventoryEntity entity)
        {
            entity = latestSnapshot;
            return hasSnapshot;
        }
    }
}
