namespace Game.Inventory.UseCase
{
    public class InventoryToggleUseCase
    {
        public bool Execute(bool isOpen)
        {
            return !isOpen;
        }
    }
}

