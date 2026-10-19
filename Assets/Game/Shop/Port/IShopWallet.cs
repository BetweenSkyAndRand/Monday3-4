namespace Game.Shop
{
    /// <summary>ショップが所持金を扱うための窓口。所持金の上限処理は実装側（お金システム）に委ねる。</summary>
    public interface IShopWallet
    {
        uint CurrentMoney { get; }

        void AddMoney(uint amount);

        bool TrySpendMoney(uint amount);
    }
}
