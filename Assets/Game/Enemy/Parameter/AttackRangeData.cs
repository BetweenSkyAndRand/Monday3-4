using System;
using UnityEngine;

namespace Game.Enemy
{
    [Serializable]
    public sealed class AttackRangeData
    {
        [SerializeField] private float attackRange = 1.5f;

        /// <summary>敵が攻撃を当てられる距離（ワールド単位）。</summary>
        public float AttackRange => attackRange;
    }
}
