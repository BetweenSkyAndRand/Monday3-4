namespace Game.Item
{
    /// <summary>
    /// アイテムの分類。Game.CellInfo.ItemType（セル上の配置物）とは別の概念。
    /// </summary>
    public enum ItemCategory
    {
        Weapon = 0,
        Recovery = 1,
        SellMaterial = 2,
        Other = 3
    }
}