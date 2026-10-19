using UnityEngine;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Game/Enemy/EnemyVirusParamMasterData", fileName = "EnemyVirusParamMasterData")]
    public sealed class EnemyVirusParamMasterData : ScriptableObject
    {
        [SerializeField] private EnemyVirusType enemyVirusType;
        [SerializeField] private uint hp;
        [Tooltip("敵本体の基礎攻撃力。攻撃時は 武器攻撃力 に合算される。ダンジョン生成回数による強化もここへ加算される。")]
        [SerializeField] private uint baseAttackPower;
        [SerializeField] private ViewRangeData viewRangeData;
        [SerializeField] private WeaponMasterData weaponMasterData;

        public EnemyVirusType EnemyVirusType => enemyVirusType;
        public uint Hp => hp;
        public uint BaseAttackPower => baseAttackPower;
        public ViewRangeData ViewRangeData => viewRangeData;
        public WeaponMasterData WeaponMasterData => weaponMasterData;
    }
}
