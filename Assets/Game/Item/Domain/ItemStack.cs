namespace Game.Item
{
    /// <summary>
    /// ドロップ・投棄・死亡時ドロップの受け渡しに使う共通の値（アイテムID・個数・レア度）。
    /// アイテムの識別は「アイテムID + レア度」で行う。
    /// </summary>
    public readonly struct ItemStack
    {
        public string ItemId { get; }
        public uint Quantity { get; }
        public ItemRarity Rarity { get; }

        public bool IsValid => !string.IsNullOrEmpty(ItemId) && Quantity > 0;

        public ItemStack(string itemId, uint quantity, ItemRarity rarity = ItemRarity.N)
        {
            ItemId = itemId;
            Quantity = quantity;
            Rarity = rarity;
        }
    }
}