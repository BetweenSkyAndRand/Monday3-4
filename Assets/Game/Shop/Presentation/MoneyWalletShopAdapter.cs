using Game.Money.Presentation;

namespace Game.Shop
{
    /// <summary>MoneyWalletController をショップ用の窓口（IShopWallet）に合わせるアダプター。</summary>
    public class MoneyWalletShopAdapter : IShopWallet
    {
        private readonly MoneyWalletController moneyWalletController;

        public MoneyWalletShopAdapter(MoneyWalletController moneyWalletController)
        {
            this.moneyWalletController = moneyWalletController;
        }

        public uint CurrentMoney => moneyWalletController.CurrentMoney;

        public void AddMoney(uint amount)
        {
            moneyWalletController.AddMoney(amount);
        }

        public bool TrySpendMoney(uint amount)
        {
            return moneyWalletController.TrySpendMoney(amount);
        }
    }
}
