using System.Text;

namespace Game.Inventory
{
    public class FinalInventorySummaryUseCase
    {
        public string Execute(InventoryEntity entity)
        {
            var builder = new StringBuilder();

            if (entity.EquippedWeaponSlot.HasValue)
            {
                builder.Append("Equip: ");
                builder.AppendLine(entity.EquippedWeaponSlot.Value.DisplayName);
            }

            foreach (var slot in entity.ItemSlots)
            {
                if (!slot.Item.HasValue)
                {
                    continue;
                }

                var item = slot.Item.Value;
                builder.Append("Slot ");
                builder.Append(slot.SlotIndex);
                builder.Append(": ");
                builder.Append(item.DisplayName);

                builder.AppendLine();
            }

            return builder.ToString().TrimEnd();
        }
    }
}

