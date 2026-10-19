using UnityEngine;

namespace Game.Revival.Parameter
{
    [CreateAssetMenu(fileName = "RevivalTreatmentParameter", menuName = "Game/Revival/Treatment Parameter")]
    public class RevivalTreatmentParameter : ScriptableObject
    {
        [Tooltip("治療費の基準額。治療費 = (基準額 × 死亡回数) × 倍率")]
        [SerializeField] private uint baseTreatmentCost = 50;

        [Tooltip("治療費にかける倍率。治療費 = (基準額 × 死亡回数) × 倍率")]
        [SerializeField] private uint treatmentCostMultiplier = 2;

        public uint BaseTreatmentCost => baseTreatmentCost;
        public uint TreatmentCostMultiplier => treatmentCostMultiplier;
    }
}
