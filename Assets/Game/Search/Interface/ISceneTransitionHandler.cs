namespace Game.Search
{
    /// <summary>
    /// 探索終了後の画面遷移を担うインターフェース。
    /// シーン切り替えや結果画面の表示のみを責務とする。
    /// </summary>
    public interface ISceneTransitionHandler
    {
        void TransitionToEndScreen();
    }
}