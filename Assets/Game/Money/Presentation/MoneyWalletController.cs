using System;
using Game.Money.Domain;
using Game.Money.Repository;
using Game.Money.UseCase;
using UnityEngine;

namespace Game.Money.Presentation
{
    /// <summary>
    /// お金システムの Presentation 層。
    /// 外部からの入出金要求を受け取り、UseCase へ委譲する。
    /// </summary>
    public class MoneyWalletController : MonoBehaviour
    {
        [Header("初期所持金")]
        [SerializeField] private uint initialMoney;

        private IMoneyBank moneyBank;
        private MoneyTransactionUseCase moneyTransactionUseCase;

        public uint CurrentMoney => moneyBank != null ? moneyBank.CurrentMoney : 0;

        public event Action<uint> MoneyChanged;

        private void Awake()
        {
            moneyBank = new RuntimeMoneyBankRepository(initialMoney);
            moneyTransactionUseCase = new MoneyTransactionUseCase(moneyBank);
        }

        public void AddMoney(uint amount)
        {
            if (moneyTransactionUseCase == null)
            {
                return;
            }

            moneyTransactionUseCase.Add(amount);
            MoneyChanged?.Invoke(CurrentMoney);
        }

        public bool TrySpendMoney(uint amount)
        {
            if (moneyTransactionUseCase == null)
            {
                return false;
            }

            var isSpent = moneyTransactionUseCase.TrySpend(amount);
            if (isSpent)
            {
                MoneyChanged?.Invoke(CurrentMoney);
            }

            return isSpent;
        }
    }
}

