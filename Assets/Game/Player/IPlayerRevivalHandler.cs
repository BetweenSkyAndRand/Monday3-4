// ReSharper disable once CheckNamespace
namespace Game.Player
{
    /// <summary>
    /// 即死イベントから復活フローを開始する窓口インターフェース。
    /// </summary>
    public interface IPlayerRevivalHandler
    {
        void HandleDeath();
    }
}

