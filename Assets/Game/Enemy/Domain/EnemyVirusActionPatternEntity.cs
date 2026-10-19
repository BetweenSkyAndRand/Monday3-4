using System.Collections.Generic;

namespace Game.Enemy
{
    public readonly struct EnemyVirusActionPatternEntity
    {
        public EnemyVirusActionPatternEntity(
            EnemyVirusStateTransitionType initialState,
            float stateTransitionCoolTime,
            IReadOnlyList<EnemyVirusStateTransitionType> actionPatterns)
        {
            InitialState = initialState;
            StateTransitionCoolTime = stateTransitionCoolTime;
            ActionPatterns = actionPatterns;
        }

        public EnemyVirusStateTransitionType InitialState { get; }
        public float StateTransitionCoolTime { get; }
        public IReadOnlyList<EnemyVirusStateTransitionType> ActionPatterns { get; }
    }
}

