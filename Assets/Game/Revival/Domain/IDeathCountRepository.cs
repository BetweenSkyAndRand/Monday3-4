namespace Game.Revival.Domain
{
    /// <summary>
    /// 死亡回数の保存先を抽象化するインターフェース。
    /// </summary>
    public interface IDeathCountRepository
    {
        int CurrentCount { get; }

        int Increment();

        void Reset();
    }
}

