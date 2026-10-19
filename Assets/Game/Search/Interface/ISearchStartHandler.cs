namespace Game.Search
{
    /// <summary>
    /// 探索開始の汎用通知を受け取るインターフェース。
    /// BGM・カメラなど、スポーン以外の開始処理を担うクラスが実装する。
    /// 責務：「探索開始を受け取る窓口になる」の 1 点のみ。
    /// </summary>
    public interface ISearchStartHandler
    {
        void OnSearchStart();
    }
}