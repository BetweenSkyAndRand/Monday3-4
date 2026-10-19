using UnityEngine;

namespace Game.CollapseTimer.Domain
{
    /// <summary>
    /// カウントダウンタイマーの状態を管理するエンティティ
    /// readonly struct として不変性を確保
    /// </summary>
    public readonly struct CountdownTimerEntity
    {
        private readonly float initialDurationSeconds;
        private readonly float remainingTimeSeconds;
        private readonly bool isRunning;
        private readonly bool isFinished;

        public float RemainingTimeSeconds => remainingTimeSeconds;
        public bool IsRunning => isRunning;
        public bool IsFinished => isFinished;

        /// <summary>
        /// コンストラクタ：初期化
        /// </summary>
        public CountdownTimerEntity(float initialDurationSeconds)
        {
            this.initialDurationSeconds = initialDurationSeconds;
            this.remainingTimeSeconds = initialDurationSeconds;
            this.isRunning = false;
            this.isFinished = false;
        }

        /// <summary>
        /// タイマーを開始する
        /// </summary>
        public CountdownTimerEntity Start()
        {
            return new CountdownTimerEntity(
                initialDurationSeconds,
                remainingTimeSeconds,
                isRunning: true,
                isFinished
            );
        }

        /// <summary>
        /// タイマーを停止する
        /// </summary>
        public CountdownTimerEntity Stop()
        {
            return new CountdownTimerEntity(
                initialDurationSeconds,
                remainingTimeSeconds,
                isRunning: false,
                isFinished
            );
        }

        /// <summary>
        /// タイマーをリセットする
        /// </summary>
        public CountdownTimerEntity Reset()
        {
            return new CountdownTimerEntity(
                initialDurationSeconds,
                initialDurationSeconds,
                isRunning: false,
                isFinished: false
            );
        }

        /// <summary>
        /// 毎フレーム時間を進める
        /// </summary>
        public CountdownTimerEntity Tick(float deltaTime)
        {
            if (!isRunning)
            {
                return this;
            }

            var newRemainingTime = remainingTimeSeconds - deltaTime;
            var hasFinished = newRemainingTime <= 0f;

            return new CountdownTimerEntity(
                initialDurationSeconds,
                Mathf.Max(0f, newRemainingTime),
                isRunning: !hasFinished,
                isFinished: hasFinished
            );
        }

        /// <summary>
        /// プライベートコンストラクタ：内部生成用
        /// </summary>
        private CountdownTimerEntity(
            float initialDurationSeconds,
            float remainingTimeSeconds,
            bool isRunning,
            bool isFinished)
        {
            this.initialDurationSeconds = initialDurationSeconds;
            this.remainingTimeSeconds = remainingTimeSeconds;
            this.isRunning = isRunning;
            this.isFinished = isFinished;
        }
    }
}

