namespace Game.Enemy
{
    /// <summary>敵強化の計算結果（加算値）。</summary>
    public readonly struct EnemyEnhancementResult
    {
        public EnemyEnhancementResult(uint hpBonus, uint attackPowerBonus, float dropRateMultiplier = 1f)
        {
            DropRateMultiplier = dropRateMultiplier;
            HpBonus = hpBonus;
            AttackPowerBonus = attackPowerBonus;
        }

        public uint HpBonus { get; }
        public uint AttackPowerBonus { get; }
        /// <summary>ドロップ率の倍率（強化なしは 1）。</summary>
        public float DropRateMultiplier { get; }
    }

    /// <summary>
    /// 累計ダンジョン生成回数から敵の強化値を求める。
    /// 閾値以上のマイルストーンをすべて合算する（加算式）。
    /// </summary>
    public class EnemyEnhancementUseCase
    {
        public EnemyEnhancementResult Execute(int generationCount, EnemyEnhancementParameter parameter)
        {
            if (parameter == null || parameter.Milestones == null)
            {
                return new EnemyEnhancementResult(0, 0);
            }

            uint totalHpBonus = 0;
            uint totalAttackPowerBonus = 0;
            var totalDropRateBonus = 0f;
            foreach (var milestone in parameter.Milestones)
            {
                if (generationCount < milestone.generationCount)
                {
                    continue;
                }

                totalHpBonus += milestone.hpBonus;
                totalAttackPowerBonus += milestone.attackPowerBonus;
                totalDropRateBonus += milestone.dropRateBonus;
            }

            return new EnemyEnhancementResult(totalHpBonus, totalAttackPowerBonus, 1f + totalDropRateBonus);
        }
    }
}

