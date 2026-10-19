namespace Game.Enemy
{
    public readonly struct EnemyVirusSpawnContext
    {
        public EnemyVirusSpawnContext(string uid, EnemyVirusEntity enemyEntity, EnemyVirusViewData enemyViewData)
        {
            Uid = uid;
            EnemyEntity = enemyEntity;
            EnemyViewData = enemyViewData;
        }

        public string Uid { get; }
        public EnemyVirusEntity EnemyEntity { get; }
        public EnemyVirusViewData EnemyViewData { get; }
    }
}

