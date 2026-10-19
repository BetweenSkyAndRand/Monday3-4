using System;
using UnityEngine;

namespace Game.Enemy
{
    [Serializable]
    public sealed class EnemyVirusMasterData
    {
        [SerializeField] private string uid;
        [SerializeField] private EnemyVirusParamMasterData virusParamMasterData;
        [SerializeField] private EnemyVirusActionPatternMasterData virusActionPatternMasterData;

        public string Uid => uid;
        public EnemyVirusParamMasterData VirusParamMasterData => virusParamMasterData;
        public EnemyVirusActionPatternMasterData VirusActionPatternMasterData => virusActionPatternMasterData;
    }
}

