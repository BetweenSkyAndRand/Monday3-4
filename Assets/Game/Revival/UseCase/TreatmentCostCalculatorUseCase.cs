namespace Game.Revival.UseCase
{
    /// <summary>
    /// 治療費の計算を行うユースケース。
    /// 治療費 = (基準額 * 死亡回数) * 倍率。
    /// </summary>
    public class TreatmentCostCalculatorUseCase
    {
        private readonly uint baseTreatmentCost;
        private readonly uint treatmentCostMultiplier;

        public TreatmentCostCalculatorUseCase(uint baseTreatmentCost, uint treatmentCostMultiplier)
        {
            this.baseTreatmentCost = baseTreatmentCost;
            this.treatmentCostMultiplier = treatmentCostMultiplier;
        }

        public uint Calculate(int deathCount)
        {
            if (deathCount <= 0)
            {
                return 0;
            }

            var rawCost = (ulong)baseTreatmentCost * (ulong)deathCount * treatmentCostMultiplier;
            if (rawCost > uint.MaxValue)
            {
                return uint.MaxValue;
            }

            return (uint)rawCost;
        }
    }
}
