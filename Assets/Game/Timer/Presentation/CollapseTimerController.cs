using System;
using UnityEngine;

namespace Game.CollapseTimer.Presentation
{
    /// <summary>
    /// ダンジョン崩壊タイマーのコントローラー
    /// Update() でタイマーを進める
    /// ゲーム開始時にタイマーを起動
    /// イベントを外部向けに公開
    /// </summary>
    public class CollapseTimerController : MonoBehaviour
    {
        [SerializeField] private Game.CollapseTimer.Parameter.DungeonCollapseTimerParameter timerParameter;

        private Game.Timer.UseCase.CollapseTimerUseCase timerUseCase;

        /// <summary>
        /// 外部向けイベント公開
        /// </summary>
        public event Action OnTimerFinished
        {
            add => timerUseCase.OnTimerFinished += value;
            remove => timerUseCase.OnTimerFinished -= value;
        }

        private void Awake()
        {
            if (timerParameter == null)
            {
                UnityEngine.Debug.LogError("[CollapseTimerController] DungeonCollapseTimerParameter が未設定です");
                return;
            }

            timerUseCase = new Game.Timer.UseCase.CollapseTimerUseCase(timerParameter.InitialDurationSeconds);
            UnityEngine.Debug.Log("[CollapseTimerController] Awake：UseCase 初期化完了");
        }

        private void Start()
        {
            StartTimer();
        }

        private void Update()
        {
            if (timerUseCase != null)
            {
                timerUseCase.Tick(Time.deltaTime);
            }
        }

        /// <summary>
        /// タイマーを開始する（外部からも呼び出し可）
        /// </summary>
        public void StartTimer()
        {
            if (timerUseCase != null)
            {
                timerUseCase.StartTimer();
            }
        }

        /// <summary>
        /// タイマーを停止する
        /// </summary>
        public void StopTimer()
        {
            if (timerUseCase != null)
            {
                timerUseCase.StopTimer();
            }
        }

        /// <summary>
        /// タイマーをリセットする
        /// </summary>
        public void ResetTimer()
        {
            if (timerUseCase != null)
            {
                timerUseCase.ResetTimer();
            }
        }

        /// <summary>
        /// 現在の残り時間を取得する
        /// </summary>
        public float GetRemainingTime()
        {
            return timerUseCase?.GetRemainingTime() ?? 0f;
        }

        public bool IsRunning => timerUseCase?.IsRunning ?? false;
        public bool IsFinished => timerUseCase?.IsFinished ?? false;
    }
}

