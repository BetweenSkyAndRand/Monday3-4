// ReSharper disable once CheckNamespace
namespace Game.GameClear
{
    /// <summary>
    /// ゲームクリア条件達成時にゲームクリアを発火させるためのインターフェース。
    /// クリア条件は未定（納品システムは廃止済み）。現在は呼び出し元がない。
    /// クリア条件を決めたら TriggerGameClear() を呼ぶ。
    /// GameClearHandler がこれを実装する。
    /// </summary>
    public interface IGameClearTrigger
    {
        /// <summary>ゲームクリアを発火し、ゲームクリアシーンへ遷移する。</summary>
        void TriggerGameClear();
    }
}


