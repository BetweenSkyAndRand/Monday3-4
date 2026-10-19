namespace Game.Enemy
{
    public sealed class EnemyVirusResolver : IEnemyVirusResolver
    {
        private readonly EnemyVirusEntityRepository enemyVirusEntityRepository;
        private readonly EnemyVirusViewRepository enemyVirusViewRepository;

        public EnemyVirusResolver(
            EnemyVirusEntityRepository enemyVirusEntityRepository,
            EnemyVirusViewRepository enemyVirusViewRepository)
        {
            this.enemyVirusEntityRepository = enemyVirusEntityRepository;
            this.enemyVirusViewRepository = enemyVirusViewRepository;
        }

        public bool TryResolveSpawnContext(
            string uid,
            out EnemyVirusSpawnContext spawnContext,
            out EnemyVirusResolveErrorType errorType)
        {
            spawnContext = default;
            errorType = EnemyVirusResolveErrorType.None;

            if (!TryNormalizeUid(uid, out var normalizedUid))
            {
                errorType = EnemyVirusResolveErrorType.InvalidUid;
                return false;
            }

            if (!enemyVirusEntityRepository.TryGetByUid(normalizedUid, out var enemyEntity))
            {
                errorType = EnemyVirusResolveErrorType.EntityNotFound;
                return false;
            }

            if (!enemyVirusViewRepository.TryGetByUid(normalizedUid, out var enemyViewData))
            {
                errorType = EnemyVirusResolveErrorType.ViewDataNotFound;
                return false;
            }

            spawnContext = new EnemyVirusSpawnContext(normalizedUid, enemyEntity, enemyViewData);
            return true;
        }

        private static bool TryNormalizeUid(string uid, out string normalizedUid)
        {
            normalizedUid = uid?.Trim();
            return !string.IsNullOrEmpty(normalizedUid);
        }
    }
}

