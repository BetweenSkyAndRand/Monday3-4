using Game.GameOver;
using Game.Money.Presentation;
using Game.Player;
using Game.Revival.Domain;
using Game.Revival.Parameter;
using Game.Revival.Port;
using Game.Revival.Repository;
using Game.Revival.UseCase;
using Game.Utility;
using UnityEngine;

namespace Game.Revival.Presentation
{
    /// <summary>
    /// 死亡時の復活フローを統括する Presentation 層。
    /// 支払い判定とリスポーン実行を司り、状態遷移の順序を保証する。
    /// </summary>
    public class PlayerRevivalController : MonoBehaviour, IPlayerRevivalHandler
    {
        [Header("参照")]
        [SerializeField] private MoneyWalletController moneyWalletController;
        [SerializeField] private MonoBehaviour playerRespawnHandlerObject;
        [SerializeField] private MonoBehaviour gameplayInputControllerObject;
        [SerializeField] private MonoBehaviour gameOverTriggerObject;

        [Header("通知（IRevivalFlowNotifier 実装）")]
        [SerializeField] private MonoBehaviour[] revivalFlowNotifierObjects;

        [Header("治療費パラメータ")]
        [SerializeField] private RevivalTreatmentParameter revivalTreatmentParameter;
        [SerializeField] private uint fallbackBaseTreatmentCost = 50;
        [SerializeField] private uint fallbackTreatmentCostMultiplier = 2;

        private IPlayerRespawnHandler playerRespawnHandler;
        private IGameplayInputController gameplayInputController;
        private IGameOverTrigger gameOverTrigger;
        private IRevivalFlowNotifier[] revivalFlowNotifiers;

        private IDeathCountRepository deathCountRepository;
        private TreatmentCostCalculatorUseCase treatmentCostCalculatorUseCase;
        private PlayerLifeStateMachine playerLifeStateMachine;

        public PlayerLifeState CurrentState => playerLifeStateMachine != null
            ? playerLifeStateMachine.CurrentState
            : PlayerLifeState.Alive;

        public int CurrentDeathCount => deathCountRepository != null ? deathCountRepository.CurrentCount : 0;

        private void Awake()
        {
            playerRespawnHandler = MonoBehaviourCaster.CastSingle<IPlayerRespawnHandler>(playerRespawnHandlerObject, nameof(PlayerRevivalController));
            gameplayInputController = MonoBehaviourCaster.CastSingle<IGameplayInputController>(gameplayInputControllerObject, nameof(PlayerRevivalController));
            gameOverTrigger = MonoBehaviourCaster.CastSingle<IGameOverTrigger>(gameOverTriggerObject, nameof(PlayerRevivalController));
            revivalFlowNotifiers = MonoBehaviourCaster.CastArray<IRevivalFlowNotifier>(revivalFlowNotifierObjects, nameof(PlayerRevivalController));

            deathCountRepository = new InMemoryDeathCountRepository();
            playerLifeStateMachine = new PlayerLifeStateMachine();

            var baseTreatmentCost = fallbackBaseTreatmentCost;
            var treatmentCostMultiplier = fallbackTreatmentCostMultiplier;
            if (revivalTreatmentParameter != null)
            {
                baseTreatmentCost = revivalTreatmentParameter.BaseTreatmentCost;
                treatmentCostMultiplier = revivalTreatmentParameter.TreatmentCostMultiplier;
            }

            treatmentCostCalculatorUseCase = new TreatmentCostCalculatorUseCase(baseTreatmentCost, treatmentCostMultiplier);

            if (moneyWalletController == null)
            {
                UnityEngine.Debug.LogWarning("[PlayerRevivalController] moneyWalletController が未設定です。復活処理が失敗する可能性があります。", this);
            }
        }

        public void HandleDeath()
        {
            UnityEngine.Debug.Log("[PlayerRevivalController] HandleDeath 開始。");

            if (playerLifeStateMachine == null || !playerLifeStateMachine.TryEnterDeathProcessing())
            {
                UnityEngine.Debug.LogWarning("[PlayerRevivalController] DeathProcessing への遷移に失敗しました。");
                return;
            }

            gameplayInputController?.DisableGameplayInputExceptPause();
            playerRespawnHandler?.RunDeathPreparation();

            var deathCount = deathCountRepository.Increment();
            var treatmentCost = treatmentCostCalculatorUseCase.Calculate(deathCount);
            var currentMoney = moneyWalletController != null ? moneyWalletController.CurrentMoney : 0;
            UnityEngine.Debug.Log($"[PlayerRevivalController] 支払い判定前。死亡回数: {deathCount}, 治療費: {treatmentCost}, 所持金: {currentMoney}");
            NotifyTreatmentCost(deathCount, treatmentCost, currentMoney);

            if (moneyWalletController != null && moneyWalletController.TrySpendMoney(treatmentCost))
            {
                var remainingMoney = moneyWalletController.CurrentMoney;
                UnityEngine.Debug.Log($"[PlayerRevivalController] 支払い成功。支払額: {treatmentCost}, 残高: {remainingMoney}");
                NotifyPaymentSucceeded(treatmentCost, remainingMoney);

                if (!playerLifeStateMachine.TryEnterRespawning())
                {
                    gameplayInputController?.EnableGameplayInputExceptPause();
                    playerLifeStateMachine.EnterAlive();
                    UnityEngine.Debug.LogWarning("[PlayerRevivalController] Respawning への状態遷移に失敗したため Alive に戻します。", this);
                    return;
                }

                playerRespawnHandler?.Respawn();
                gameplayInputController?.EnableGameplayInputExceptPause();
                playerLifeStateMachine.EnterAlive();
                NotifyRespawnCompleted();
                return;
            }
            UnityEngine.Debug.LogWarning($"[PlayerRevivalController] 支払い失敗。必要額: {treatmentCost}, 所持金: {currentMoney}");
            NotifyPaymentFailed(treatmentCost, currentMoney);
            playerLifeStateMachine.EnterGameOver();

            if (gameOverTrigger != null)
            {
                gameOverTrigger.TriggerGameOver();
                return;
            }

            UnityEngine.Debug.LogWarning("[PlayerRevivalController] gameOverTriggerObject が未設定のため、ゲームオーバー遷移を実行できません。", this);
        }

        public void ResetDeathCount()
        {
            deathCountRepository?.Reset();
        }

        private void NotifyTreatmentCost(int deathCount, uint treatmentCost, uint currentMoney)
        {
            foreach (var revivalFlowNotifier in revivalFlowNotifiers)
            {
                revivalFlowNotifier.NotifyTreatmentCost(deathCount, treatmentCost, currentMoney);
            }
        }

        private void NotifyPaymentSucceeded(uint treatmentCost, uint remainingMoney)
        {
            foreach (var revivalFlowNotifier in revivalFlowNotifiers)
            {
                revivalFlowNotifier.NotifyPaymentSucceeded(treatmentCost, remainingMoney);
            }
        }

        private void NotifyPaymentFailed(uint treatmentCost, uint currentMoney)
        {
            foreach (var revivalFlowNotifier in revivalFlowNotifiers)
            {
                revivalFlowNotifier.NotifyPaymentFailed(treatmentCost, currentMoney);
            }
        }

        private void NotifyRespawnCompleted()
        {
            foreach (var revivalFlowNotifier in revivalFlowNotifiers)
            {
                revivalFlowNotifier.NotifyRespawnCompleted();
            }
        }
    }
}



