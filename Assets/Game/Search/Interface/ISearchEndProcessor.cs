namespace Game.Search
{
    /// <summary>
    /// 探索終了処理の契約を定義するインターフェース。
    /// 残敵管理（GetCount / Destroy）と画面遷移（TransitionToEndScreen）を定義する。
    /// 将来的に実装クラスが別れる場合は IEnemyCleanupHandler / ISceneTransitionHandler への分割を検討する。
    /// </summary>
    /// <obsolete>
    /// IEnemyCleanupHandler / ISceneTransitionHandler を使用してください。
    /// </obsolete>
    public interface ISearchEndProcessor : IEnemyCleanupHandler, ISceneTransitionHandler
    {
    }
}