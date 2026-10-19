using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Game/Enemy/EnemyVirusViewDataList", fileName = "EnemyVirusViewDataList")]
    public sealed class EnemyVirusViewDataList : ScriptableObject
    {
        [SerializeField] private List<EnemyVirusViewData> viewDataList = new();

        public IReadOnlyList<EnemyVirusViewData> ViewDataList => viewDataList;
    }
}

