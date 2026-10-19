namespace Game.Enemy
{
    public readonly struct EnemyVirusEntity
    {
        public EnemyVirusEntity(
            string uid,
            EnemyVirusType enemyVirusType,
            uint hp,
            uint baseAttackPower,
            ViewRangeData viewRangeData,
            WeaponEntity weaponEntity,
            EnemyVirusActionPatternEntity actionPatternEntity)
        {
            Uid = uid;
            EnemyVirusType = enemyVirusType;
            Hp = hp;
            BaseAttackPower = baseAttackPower;
            ViewRangeData = viewRangeData;
            WeaponEntity = weaponEntity;
            ActionPatternEntity = actionPatternEntity;
        }

        public string Uid { get; }
        public EnemyVirusType EnemyVirusType { get; }
        public uint Hp { get; }
        /// <summary>敵本体の攻撃力。攻撃時のダメージは 本体攻撃力 + 武器攻撃力。</summary>
        public uint BaseAttackPower { get; }
        public ViewRangeData ViewRangeData { get; }
        public WeaponEntity WeaponEntity { get; }
        public EnemyVirusActionPatternEntity ActionPatternEntity { get; }
    }
}

