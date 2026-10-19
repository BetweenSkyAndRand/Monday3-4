namespace Game.Enemy
{
    public readonly struct WeaponEntity
    {
        public WeaponEntity(uint attackPower, float attackCoolTime, AttackRangeData attackRangeData)
        {
            AttackPower = attackPower;
            AttackCoolTime = attackCoolTime;
            AttackRangeData = attackRangeData;
        }

        public uint AttackPower { get; }
        /// <summary>攻撃クールタイム（秒）。WeaponMasterData から設定される。</summary>
        public float AttackCoolTime { get; }
        public AttackRangeData AttackRangeData { get; }
    }
}

