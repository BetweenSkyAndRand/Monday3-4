using UnityEngine;

namespace Game.Enemy
{
    public interface IEnemyVirusSpawner
    {
        bool TrySpawn(
            EnemyVirusSpawnCommand spawnCommand,
            out GameObject spawnedEnemyObject,
            out EnemyVirusSpawnErrorType spawnErrorType,
            out EnemyVirusResolveErrorType resolveErrorType);
    }
}

