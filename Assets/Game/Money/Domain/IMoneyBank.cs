namespace Game.Money.Domain
{
    /// <summary>
    /// 所持金の保存先を抽象化するインターフェース。
    /// </summary>
    public interface IMoneyBank
    {
        uint CurrentMoney { get; }

        void SetMoney(uint amount);
    }
}

