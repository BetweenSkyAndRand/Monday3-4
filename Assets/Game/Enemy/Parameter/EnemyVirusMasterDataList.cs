using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Game/Enemy/EnemyVirusMasterDataList", fileName = "EnemyVirusMasterDataList")]
    public sealed class EnemyVirusMasterDataList : ScriptableObject
    {
        [SerializeField] private List<EnemyVirusMasterData> enemyVirusMasterDataList = new();

        public IReadOnlyList<EnemyVirusMasterData> EnemyVirusMasterDataListValue => enemyVirusMasterDataList;
    }
}

