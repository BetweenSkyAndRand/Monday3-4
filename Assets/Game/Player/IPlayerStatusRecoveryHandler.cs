// ReSharper disable once CheckNamespace
namespace Game.Player.Presentation
{
    /// <summary>
    /// リスポーン成功後にプレイヤーのHP状態を復旧するためのインターフェース。
    /// </summary>
    public interface IPlayerStatusRecoveryHandler
    {
        /// <summary>
        /// HPと死亡済みフラグを復旧し、再度プレイ可能な状態へ戻す。
        /// </summary>
        void RestoreToFullHealth();
    }
}


