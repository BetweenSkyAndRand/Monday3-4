using System;
using UnityEngine;

namespace Game.Timer.UseCase
{
    /// <summary>
    /// ダンジョン崩壊タイマーのユースケース
    /// タイマーの操作、イベント発火を担当
    /// </summary>
    public class CollapseTimerUseCase
    {
        private Game.CollapseTimer.Domain.CountdownTimerEntity timerEntity;
        private bool hasRaisedFinishedEvent;

        /// <summary>
        /// タイマーが終了した際に発火するイベント
        /// </summary>
        public event Action OnTimerFinished;

        public bool IsRunning => timerEntity.IsRunning;
        public bool IsFinished => timerEntity.IsFinished;

        /// <summary>
        /// コンストラクタ
        /// </summary>
        public CollapseTimerUseCase(float initialDurationSeconds)
        {
            timerEntity = new Game.CollapseTimer.Domain.CountdownTimerEntity(initialDurationSeconds);
            hasRaisedFinishedEvent = false;
        }

        /// <summary>
        /// タイマーを開始する
        /// </summary>
        public void StartTimer()
        {
            timerEntity = timerEntity.Start();
            hasRaisedFinishedEvent = false;
            UnityEngine.Debug.Log($"[CollapseTimerUseCase] タイマー開始：残り時間 {timerEntity.RemainingTimeSeconds}秒");
        }

        /// <summary>
        /// タイマーを停止する
        /// </summary>
        public void StopTimer()
        {
            timerEntity = timerEntity.Stop();
            UnityEngine.Debug.Log($"[CollapseTimerUseCase] タイマー停止：残り時間 {timerEntity.RemainingTimeSeconds}秒");
        }

        /// <summary>
        /// タイマーをリセットする
        /// </summary>
        public void ResetTimer()
        {
            timerEntity = timerEntity.Reset();
            hasRaisedFinishedEvent = false;
            UnityEngine.Debug.Log("[CollapseTimerUseCase] タイマーリセット");
        }

        /// <summary>
        /// 毎フレーム呼ぶ：タイマーを進める＆イベント発火
        /// 終了瞬間のみイベント発火する（多重発火防止）
        /// </summary>
        public void Tick(float deltaTime)
        {
            timerEntity = timerEntity.Tick(deltaTime);

            if (timerEntity.IsFinished && !hasRaisedFinishedEvent)
            {
                hasRaisedFinishedEvent = true;
                UnityEngine.Debug.Log("[CollapseTimerUseCase] タイマー終了 → OnTimerFinished イベント発火");
                OnTimerFinished?.Invoke();
            }
        }

        /// <summary>
        /// 現在の残り時間を取得する
        /// </summary>
        public float GetRemainingTime()
        {
            return timerEntity.RemainingTimeSeconds;
        }
    }
}

