namespace Game.Item
{
    /// <summary>
    /// アイテムのレア度。数値がそのまま売却額の指数になる（N=0, R=1, SR=2, UR=3）。
    /// </summary>
    public enum ItemRarity
    {
        N = 0,
        R = 1,
        SR = 2,
        UR = 3
    }
}