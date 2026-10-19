using System;
using UnityEngine;

namespace Game.Dungeon.Spawn
{
    /// <summary>Inspector から出現数・確率を設定できるスポーン種別。</summary>
    public enum ConfigurableSpawnType
    {
        TreasureBox,
        Misc
    }

    /// <summary>
    /// Inspector で設定する1スポーンルール。room_spawn.json の SpawnRule と同じ意味で、
    /// 同じ種別の行がある場合は JSON より優先される。
    /// </summary>
    [Serializable]
    public class SpawnRuleSetting
    {
        [Tooltip("スポーンする種別")]
        public ConfigurableSpawnType type;

        [Tooltip("1部屋あたりの確定スポーン数（確率抽選なし）")]
        [Min(0)]
        public int minCount;

        [Tooltip("1部屋あたりのスポーン数の上限。minCount を超えた分は ratePer10000 で 1 つずつ抽選する")]
        [Min(0)]
        public int maxCount = 1;

        [Tooltip("確定数を超えた追加 1 つごとの出現率（万分率。10000 = 100%）")]
        [Range(0, 10000)]
        public int ratePer10000;

        /// <summary>JSON ルールと同じ形式に変換する。minCount が maxCount を超えないよう補正する。</summary>
        public SpawnRule ToSpawnRule()
        {
            var safeMinCount = Mathf.Max(0, minCount);
            return new SpawnRule
            {
                type = type.ToString(),
                minCount = safeMinCount,
                maxCount = Mathf.Max(safeMinCount, maxCount),
                ratePer10000 = Mathf.Clamp(ratePer10000, 0, 10000)
            };
        }
    }
}
