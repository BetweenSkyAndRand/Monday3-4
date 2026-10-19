using Game.Money.Domain;

namespace Game.Money.Repository
{
    /// <summary>
    /// ランタイム上で所持金を保持する Repository。
    /// 保存責務だけを持つ。
    /// </summary>
    public class RuntimeMoneyBankRepository : IMoneyBank
    {
        public uint CurrentMoney { get; private set; }

        public RuntimeMoneyBankRepository(uint initialMoney)
        {
            CurrentMoney = initialMoney;
        }

        public void SetMoney(uint amount)
        {
            CurrentMoney = amount;
        }
    }
}

