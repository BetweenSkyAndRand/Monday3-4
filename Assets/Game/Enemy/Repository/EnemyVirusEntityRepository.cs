using System.Collections.Generic;

namespace Game.Enemy
{
    public sealed class EnemyVirusEntityRepository
    {
        private Dictionary<string, EnemyVirusEntity> enemyVirusEntityDict = new();

        public void Cache(EnemyVirusMasterDataList masterDataList)
        {
            enemyVirusEntityDict = EnemyVirusEntityFactory.CreateDictionary(masterDataList);
        }

        public IReadOnlyDictionary<string, EnemyVirusEntity> GetAll()
        {
            return enemyVirusEntityDict;
        }

        public bool TryGetByUid(string uid, out EnemyVirusEntity enemyEntity)
        {
            enemyEntity = default;
            if (string.IsNullOrWhiteSpace(uid))
            {
                return false;
            }

            var normalizedUid = uid.Trim();
            return enemyVirusEntityDict.TryGetValue(normalizedUid, out enemyEntity);
        }
    }
}

