using Game.Revival.Domain;

namespace Game.Revival.Repository
{
    /// <summary>
    /// ランタイム上で死亡回数を保持する Repository。
    /// </summary>
    public class InMemoryDeathCountRepository : IDeathCountRepository
    {
        public int CurrentCount { get; private set; }

        public int Increment()
        {
            CurrentCount += 1;
            return CurrentCount;
        }

        public void Reset()
        {
            CurrentCount = 0;
        }
    }
}

