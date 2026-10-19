namespace Game.Search
{
    /// <summary>
    /// 現在の探索フェーズを保存・取得するリポジトリ。
    /// 判定・通知ロジックは持たない。
    /// </summary>
    public class SearchPhaseRepository
    {
        private SearchPhase currentPhase = SearchPhase.NotStarted;
        public void SetPhase(SearchPhase phase)
        {
            currentPhase = phase;
        }
        public SearchPhase GetPhase()
        {
            return currentPhase;
        }
    }
}