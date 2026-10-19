using Game.Revival.Port;
using UnityEngine;

namespace Game.Revival.Presentation
{
    /// <summary>
    /// 復活フロー通知のログ表示実装。
    /// UI未実装期間の代替手段として使用する。
    /// </summary>
    public class DebugRevivalFlowNotifier : MonoBehaviour, IRevivalFlowNotifier
    {
        public void NotifyTreatmentCost(int deathCount, uint treatmentCost, uint currentMoney)
        {
            UnityEngine.Debug.Log($"[Revival] 死亡回数: {deathCount} / 治療費 {treatmentCost} 円を支払います。現在所持金: {currentMoney} 円");
        }

        public void NotifyPaymentSucceeded(uint treatmentCost, uint remainingMoney)
        {
            UnityEngine.Debug.Log($"[Revival] 治療費の支払いに成功しました。支払い額: {treatmentCost} 円 / 残高: {remainingMoney} 円");
        }

        public void NotifyPaymentFailed(uint treatmentCost, uint currentMoney)
        {
            UnityEngine.Debug.LogWarning($"[Revival] 所持金不足で支払い失敗。必要額: {treatmentCost} 円 / 現在所持金: {currentMoney} 円");
        }

        public void NotifyRespawnCompleted()
        {
            UnityEngine.Debug.Log("[Revival] 復活処理が完了しました。リスポーンします。");
        }
    }
}

