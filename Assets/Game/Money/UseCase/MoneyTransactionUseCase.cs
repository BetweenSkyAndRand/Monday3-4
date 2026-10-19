using Game.Money.Domain;

namespace Game.Money.UseCase
{
    /// <summary>
    /// 所持金の加減算ルールを扱うユースケース。
    /// </summary>
    public class MoneyTransactionUseCase
    {
        private readonly IMoneyBank moneyBank;

        public MoneyTransactionUseCase(IMoneyBank moneyBank)
        {
            this.moneyBank = moneyBank;
        }

        public void Add(uint amount)
        {
            var result = (ulong)moneyBank.CurrentMoney + amount;
            if (result > uint.MaxValue)
            {
                moneyBank.SetMoney(uint.MaxValue);
                return;
            }

            moneyBank.SetMoney((uint)result);
        }

        public bool TrySpend(uint amount)
        {
            if (amount > moneyBank.CurrentMoney)
            {
                return false;
            }

            moneyBank.SetMoney(moneyBank.CurrentMoney - amount);
            return true;
        }
    }
}

