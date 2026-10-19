// ReSharper disable once CheckNamespace
namespace Game.UI
{
    /// <summary>
    /// 死亡時など、外部イベントによって強制的に閉じるべきUIパネルの共通窓口。
    /// 各UIコントローラー（StorageInventoryController等）がこれを実装することで、
    /// 呼び出し側（PlayerRespawnController等）は個々のUIの詳細を知らずに一括で閉じられる。
    /// 将来UIが増えても、この呼び出し側の変更は不要になる設計上の緩衝材。
    /// </summary>
    public interface IForceCloseableUi
    {
        /// <summary>UIが開いていれば閉じる。閉じていれば何もしない。</summary>
        void CloseIfOpen();
    }
}

