// ReSharper disable once CheckNamespace
namespace Game.Player
{
    /// <summary>
    /// プレイヤーに即死（致死）イベントを伝えるための窓口インターフェース。
    /// ダンジョン崩壊など、通常のダメージ計算（HP等）を経由しない即死イベントから使用する。
    ///
    /// 「即死時に何をするか」（現状: ゲームオーバー遷移／将来: リスポーン処理への変更可能性）は
    /// 実装クラス（PlayerDamageHandler）側の責務とし、呼び出し側は具体的な遷移先を知らなくてよい設計にする。
    /// これにより、リスポーン機能などの変更があっても、
    /// 呼び出し側（崩壊システム等）のコードは変更不要になる。
    /// </summary>
    public interface IPlayerLethalDamageHandler
    {
        /// <summary>プレイヤーを即座に死亡させる。</summary>
        void Kill();
    }
}

