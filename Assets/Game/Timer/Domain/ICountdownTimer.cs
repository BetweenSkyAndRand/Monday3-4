namespace Game.Timer.Domain
{
    /// <summary>
    /// カウントダウンタイマーのインターフェース（拡張用）
    /// </summary>
    public interface ICountdownTimer
    {
        float RemainingSeconds { get; }
        bool IsRunning { get; }
        bool IsFinished { get; }

        void Start();
        void Stop();
        void Reset(float durationSeconds);
        void Tick(float deltaTime);
    }
}

