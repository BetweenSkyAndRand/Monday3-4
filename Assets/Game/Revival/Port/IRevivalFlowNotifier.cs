namespace Game.Revival.Port
{
    /// <summary>
    /// 復活フロー中の通知を表示層へ橋渡しするポート。
    /// 当面はログ表示、将来はUI演出へ差し替える。
    /// </summary>
    public interface IRevivalFlowNotifier
    {
        void NotifyTreatmentCost(int deathCount, uint treatmentCost, uint currentMoney);

        void NotifyPaymentSucceeded(uint treatmentCost, uint remainingMoney);

        void NotifyPaymentFailed(uint treatmentCost, uint currentMoney);

        void NotifyRespawnCompleted();
    }
}

