using System.Collections.Generic;

namespace Game.Enemy
{
    public sealed class EnemyVirusViewRepository
    {
        private Dictionary<string, EnemyVirusViewData> enemyVirusViewDataDict = new();

        public void Cache(EnemyVirusViewDataList viewDataList)
        {
            enemyVirusViewDataDict = EnemyVirusViewFactory.CreateDictionary(viewDataList);
        }

        public IReadOnlyDictionary<string, EnemyVirusViewData> GetAll()
        {
            return enemyVirusViewDataDict;
        }

        public bool TryGetByUid(string uid, out EnemyVirusViewData enemyViewData)
        {
            enemyViewData = null;
            if (string.IsNullOrWhiteSpace(uid))
            {
                return false;
            }

            var normalizedUid = uid.Trim();
            return enemyVirusViewDataDict.TryGetValue(normalizedUid, out enemyViewData);
        }
    }
}

