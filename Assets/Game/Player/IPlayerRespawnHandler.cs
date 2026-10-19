// ReSharper disable once CheckNamespace
namespace Game.Player
{
    /// <summary>
    /// 死亡後の後処理とリスポーン処理を分離して呼び出すためのインターフェース。
    /// </summary>
    public interface IPlayerRespawnHandler
    {
        /// <summary>
        /// 死亡直後に実行する準備処理（UIクローズ、探索終了など）を実行する。
        /// </summary>
        void RunDeathPreparation();

        /// <summary>
        /// 支払い成功後のリスポーン本処理を実行する。
        /// </summary>
        void Respawn();
    }
}

