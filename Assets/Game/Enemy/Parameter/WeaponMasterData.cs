using UnityEngine;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Game/Enemy/WeaponMasterData", fileName = "WeaponMasterData")]
    public sealed class WeaponMasterData : ScriptableObject
    {
        [SerializeField] private uint attackPower;
        [SerializeField] private float attackCoolTime = 1f;
        [SerializeField] private AttackRangeData attackRangeData;

        public uint AttackPower => attackPower;
        /// <summary>攻撃のクールタイム（秒）。0 以下は 0 として扱う。</summary>
        public float AttackCoolTime => attackCoolTime;
        public AttackRangeData AttackRangeData => attackRangeData;
    }
}

