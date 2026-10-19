namespace Game.Debug
{
    /// <summary>
    /// デバッグログのカテゴリ。
    /// カテゴリごとにログのON/OFFを制御できる。
    /// </summary>
    public enum LogCategory
    {
        /// <summary>プレイヤー関連（移動・攻撃・インベントリなど）</summary>
        Player,

        /// <summary>敵関連（AI・攻撃・ダメージなど）</summary>
        Enemy,

        /// <summary>インベントリ・アイテム関連</summary>
        Inventory,

        /// <summary>ダンジョン生成・部屋配置関連</summary>
        Dungeon,

        /// <summary>戦闘・ダメージ関連</summary>
        Combat,

        /// <summary>UI関連</summary>
        UI,

        /// <summary>オブジェクト・インタラクション関連</summary>
        Object,

        /// <summary>システム・マネージャー関連</summary>
        System,

        /// <summary>その他</summary>
        Other
    }
}

