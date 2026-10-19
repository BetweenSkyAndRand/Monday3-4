using UnityEngine;

namespace Game.Drop
{
    /// <summary>抽選用の乱数。テストで固定値を返せるようにするための窓口。</summary>
    public interface IDropRandom
    {
        /// <summary>minInclusive 以上 maxExclusive 未満の実数。</summary>
        float Range(float minInclusive, float maxExclusive);

        /// <summary>minInclusive 以上 maxExclusive 未満の整数。</summary>
        int RangeInt(int minInclusive, int maxExclusive);
    }

    public sealed class UnityDropRandom : IDropRandom
    {
        public float Range(float minInclusive, float maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }

        public int RangeInt(int minInclusive, int maxExclusive)
        {
            return Random.Range(minInclusive, maxExclusive);
        }
    }
}