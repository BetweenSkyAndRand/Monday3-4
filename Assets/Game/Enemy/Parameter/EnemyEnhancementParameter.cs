using System;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// 「何回目のダンジョン生成で、どれだけ強化するか」の1エントリ。
    /// </summary>
    [Serializable]
    public struct EnemyEnhancementMilestone
    {
        [Tooltip("累計ダンジョン生成回数がこの値以上になると適用される。")]
        [Min(1)] public int generationCount;
        [Tooltip("加算するHP。")]
        public uint hpBonus;
        [Tooltip("加算する敵本体の攻撃力。")]
        public uint attackPowerBonus;
        [Tooltip("ドロップ率の加算ボーナス。0.5 で +50%（倍率 1.5）。閾値を満たしたマイルストーンの合計に 1 を足したものが倍率になる。")]
        [Min(0f)] public float dropRateBonus;
    }

    /// <summary>
    /// 敵強化のマイルストーン一覧。閾値・加算値は Inspector から自由に変更できる。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Enemy/EnemyEnhancementParameter", fileName = "EnemyEnhancementParameter")]
    public sealed class EnemyEnhancementParameter : ScriptableObject
    {
        [SerializeField] private EnemyEnhancementMilestone[] milestones = Array.Empty<EnemyEnhancementMilestone>();

        public IReadOnlyList<EnemyEnhancementMilestone> Milestones => milestones;
    }
}

