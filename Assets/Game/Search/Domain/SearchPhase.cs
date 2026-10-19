namespace Game.Search
{
    /// <summary>探索フェーズを表す列挙型。</summary>
    public enum SearchPhase
    {
        /// <summary>探索未開始。ビルドフェーズ完了前または初期状態。</summary>
        NotStarted,
        /// <summary>探索中。プレイヤーがトリガーを通過した後。</summary>
        InProgress,
        /// <summary>探索終了。終了ボタン押下後。</summary>
        Ended,
    }
}