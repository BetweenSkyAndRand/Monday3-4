using UnityEngine;

namespace Game.Dungeon.Presentation
{
    /// <summary>
    /// ダンジョン崩壊を管理するシステム
    /// CollapseTimerControllerのイベントを購読して崩壊処理を実行
    /// </summary>
    public class DungeonCollapseManager : MonoBehaviour
    {
        [SerializeField] private Game.CollapseTimer.Presentation.CollapseTimerController timerController;

        private void Start()
        {
            if (timerController == null)
            {
                UnityEngine.Debug.LogError("[DungeonCollapseManager] CollapseTimerController が未設定です");
                return;
            }

            timerController.OnTimerFinished += HandleDungeonCollapse;
            UnityEngine.Debug.Log("[DungeonCollapseManager] Start：タイマーイベントを購読開始");
        }

        private void OnDestroy()
        {
            if (timerController != null)
            {
                timerController.OnTimerFinished -= HandleDungeonCollapse;
            }
        }

        /// <summary>
        /// タイマー終了時に呼ばれるコールバック
        /// </summary>
        private void HandleDungeonCollapse()
        {
            UnityEngine.Debug.Log("[DungeonCollapseManager] ダンジョン崩壊イベント受信→崩壊処理開始");
            // 実装予定：
            // - ゲームオーバー状態への遷移
            // - 敵消滅処理
            // - 崩壊VFX・効果音再生
        }
    }
}

