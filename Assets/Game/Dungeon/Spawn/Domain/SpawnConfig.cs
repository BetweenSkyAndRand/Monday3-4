using System;

namespace Game.Dungeon.Spawn
{
    /// <summary>
    /// room_spawn.json の1スポーンルールに対応するデータクラス。
    /// </summary>
    [Serializable]
    public class SpawnRule
    {
        /// <summary>スポーンするオブジェクトの種別。"Enemy" / "TreasureBox" / "Misc" のいずれか。</summary>
        public string type;

        /// <summary>
        /// 確定スポーン数を超えた追加スポーン 1 体ごとの確率（万分率, 10000 = 100%）。
        /// 例: 2000 なら 1 体追加するたびに 20% の抽選を行う。
        /// </summary>
        public int ratePer10000;

        /// <summary>
        /// 確定スポーン数。確率抽選なしで必ず配置される数。
        /// 0 の場合は確定スポーンなし（ratePer10000 の確率のみで判定される）。
        /// </summary>
        public int minCount;

        /// <summary>スポーン総数の上限。minCount を超えた分だけ ratePer10000 で追加抽選する。</summary>
        public int maxCount;
    }

    /// <summary>
    /// room_spawn.json のルートオブジェクト。
    /// miscPrefabIds は未選定のため空配列で運用し、後から種類を追加できる可変設計にしている。
    /// </summary>
    [Serializable]
    public class SpawnConfigData
    {
        public SpawnRule[] spawnRules;

        /// <summary>
        /// 雑貨オブジェクトのプレハブ ID リスト。
        /// 未選定のため空配列で開始し、追加時にここへ ID を加えるだけで対応できる。
        /// </summary>
        public string[] miscPrefabIds;
    }
}
