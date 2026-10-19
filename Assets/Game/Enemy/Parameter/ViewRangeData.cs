using System;
using UnityEngine;

namespace Game.Enemy
{
    [Serializable]
    public sealed class ViewRangeData
    {
        [SerializeField] private float viewRange = 4f;
        [SerializeField] private float closeDetectionRange = 2f;
        [SerializeField] private float loseSightRange = 6f;
        [SerializeField] private float viewAngle = 90f;

        /// <summary>通常索敵でプレイヤーを検知できる距離（ワールド単位）。</summary>
        public float ViewRange => viewRange;

        /// <summary>視野角を無視してプレイヤーを検知できる近接距離（ワールド単位）。</summary>
        public float CloseDetectionRange => closeDetectionRange;

        /// <summary>追跡中にプレイヤーを見失う距離（ワールド単位）。</summary>
        public float LoseSightRange => loseSightRange;

        /// <summary>通常索敵に使用する視野角（度）。</summary>
        public float ViewAngle => viewAngle;
    }
}
