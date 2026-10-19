using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Game/Enemy/EnemyVirusActionPatternMasterData", fileName = "EnemyVirusActionPatternMasterData")]
    public sealed class EnemyVirusActionPatternMasterData : ScriptableObject
    {
        [SerializeField] private EnemyVirusStateTransitionType initialState;
        [SerializeField] private float stateTransitionCoolTime = 1f;
        [SerializeField] private List<EnemyVirusStateTransitionType> actionPatterns = new();

        public EnemyVirusStateTransitionType InitialState => initialState;
        public float StateTransitionCoolTime => stateTransitionCoolTime;
        public IReadOnlyList<EnemyVirusStateTransitionType> ActionPatterns => actionPatterns;
    }
}

