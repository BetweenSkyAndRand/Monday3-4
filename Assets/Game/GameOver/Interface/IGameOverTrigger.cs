// ReSharper disable once CheckNamespace
namespace Game.GameOver
{
    /// <summary>
    /// ゲームオーバーを発火させるためのインターフェース。
    /// リスポーン時に治療費を支払えないときに TriggerGameOver() を呼ぶ。
    /// GameOverHandler がこれを実装する。
    /// </summary>
    public interface IGameOverTrigger
    {
        /// <summary>ゲームオーバーを発火し、ゲームオーバーシーンへ遷移する。</summary>
        void TriggerGameOver();
    }
}


