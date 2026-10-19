namespace Game.Pause
{
    /// <summary>
    /// ポーズ入力を一時的に無効化したいUI（ショップ等）が実装するインターフェース。
    /// </summary>
    public interface IPauseBlocker
    {
        bool IsPauseBlocked { get; }
    }
}
