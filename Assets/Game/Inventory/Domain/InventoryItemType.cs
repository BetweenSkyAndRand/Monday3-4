namespace Game.Inventory
{
    /// <summary>
    /// インベントリ側のアイテム分類。None は空きスロットの表示用。
    /// Game.Item.ItemCategory（マスターデータ側の分類）と値の対応は InventoryItemTypeConverter で行う。
    /// </summary>
    public enum InventoryItemType
    {
        None = 0,
        Weapon = 1,
        Recovery = 2,
        SellMaterial = 3,
        Other = 4
    }
}