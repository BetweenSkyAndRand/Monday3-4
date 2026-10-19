namespace Game.Search
{
    /// <summary>
    /// 残敵の管理を担うインターフェース。
    /// 残敵カウントの取得と全残敵の消去のみを責務とする。
    /// 将来の DungeonEnemyManager などが実装する。
    /// </summary>
    public interface IEnemyCleanupHandler
    {
        int GetRemainingEnemyCount();
        void DestroyAllRemainingEnemies();
    }
}