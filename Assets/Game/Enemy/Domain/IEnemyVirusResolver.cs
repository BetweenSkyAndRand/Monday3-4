namespace Game.Enemy
{
    public interface IEnemyVirusResolver
    {
        bool TryResolveSpawnContext(
            string uid,
            out EnemyVirusSpawnContext spawnContext,
            out EnemyVirusResolveErrorType errorType);
    }
}

