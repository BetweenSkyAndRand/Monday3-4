using System;
using Game.GameOver;
using Game.Player.Domain;
using Game.Player.Parameter;
using Game.Player.UseCase;
using Game.Player.View;
using Game.Utility;
using UnityEngine;

namespace Game.Player.Presentation
{
    /// <summary>
    /// プレイヤーステータスの管理コントローラー（Presentation層）。
    /// ランタイム状態保持、UseCase呼び出し、他システム連携、UI通知を担当。
    /// </summary>
    public class PlayerStatusController : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private PlayerStatusParameter statusParameter;
        [SerializeField] private PlayerHpBarView playerHpBarView;
        [SerializeField] private PlayerDownStateBlocker playerDownStateBlocker;
        [SerializeField] private MonoBehaviour playerRevivalHandlerObject;
        [SerializeField] private MonoBehaviour gameOverTriggerObject;

        private PlayerStatusUseCase playerStatusUseCase;
        private PlayerStatus currentStatus;
        private IPlayerRevivalHandler playerRevivalHandler;
        private IGameOverTrigger gameOverTrigger;
        private bool hasDeadProcessed;

        /// <summary>HP変化イベント（現在HP, 最大HP）</summary>
        public event Action<float, float> OnHpChanged;

        /// <summary>死亡イベント</summary>
        public event Action OnDead;

        private void Awake()
        {
            playerStatusUseCase = new PlayerStatusUseCase();

            var initialMaxHp = statusParameter != null ? statusParameter.MaxHp : 100f;
            currentStatus = PlayerStatus.Create(initialMaxHp);

            if (playerRevivalHandlerObject == null)
            {
                playerRevivalHandlerObject = FindInterfaceComponentOnSelf<IPlayerRevivalHandler>();
            }

            playerRevivalHandler = MonoBehaviourCaster.CastSingle<IPlayerRevivalHandler>(playerRevivalHandlerObject, nameof(PlayerStatusController));

            if (gameOverTriggerObject != null)
            {
                gameOverTrigger = gameOverTriggerObject as IGameOverTrigger;
            }

            UnityEngine.Debug.Log($"[PlayerStatusController] 初期化完了。HP: {currentStatus.CurrentHp} / {currentStatus.MaxHp}", this);
        }

        private void Start()
        {
            UpdateView();
        }

        /// <summary>ダメージを受ける（外部入力窓口）</summary>
        public void ApplyDamage(float rawDamage)
        {
            if (rawDamage <= 0f)
            {
                return;
            }

            var previousHp = currentStatus.CurrentHp;
            currentStatus = playerStatusUseCase.ApplyDamage(currentStatus, rawDamage);

            UnityEngine.Debug.Log($"[PlayerStatusController] ダメージ受信: {rawDamage:F1} → HP {previousHp:F1} → {currentStatus.CurrentHp:F1}", this);

            UpdateView();

            if (!hasDeadProcessed && playerStatusUseCase.IsPlayerDead(currentStatus))
            {
                HandleDeathOnce();
            }
        }

        /// <summary>回復する</summary>
        public void Heal(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            var previousHp = currentStatus.CurrentHp;
            currentStatus = playerStatusUseCase.ApplyHeal(currentStatus, amount);

            UnityEngine.Debug.Log($"[PlayerStatusController] 回復: +{amount:F1} → HP {previousHp:F1} → {currentStatus.CurrentHp:F1}", this);

            UpdateView();
        }

        /// <summary>リスポーン成功後にHPと死亡済みフラグを初期化する。</summary>
        public void RestoreToFullHealth()
        {
            currentStatus = PlayerStatus.Create(currentStatus.MaxHp);
            hasDeadProcessed = false;

            UnityEngine.Debug.Log($"[PlayerStatusController] HPを全回復しました。HP: {currentStatus.CurrentHp} / {currentStatus.MaxHp}", this);
            UpdateView();
        }

        /// <summary>現在のHPを取得</summary>
        public float GetCurrentHp()
        {
            return currentStatus.CurrentHp;
        }

        /// <summary>最大HPを取得</summary>
        public float GetMaxHp()
        {
            return currentStatus.MaxHp;
        }

        /// <summary>HPを表示に反映</summary>
        private void UpdateView()
        {
            if (playerHpBarView != null)
            {
                playerHpBarView.SetHp(currentStatus.CurrentHp, currentStatus.MaxHp);
            }

            OnHpChanged?.Invoke(currentStatus.CurrentHp, currentStatus.MaxHp);
        }

        /// <summary>死亡処理（一度だけ実行）</summary>
        private void HandleDeathOnce()
        {
            hasDeadProcessed = true;
            UnityEngine.Debug.Log("[PlayerStatusController] プレイヤーが死亡しました。", this);

            OnDead?.Invoke();

            if (playerDownStateBlocker != null)
            {
                playerDownStateBlocker.SetDown();
            }

            if (playerRevivalHandler != null)
            {
                playerRevivalHandler.HandleDeath();
                return;
            }

            if (gameOverTrigger != null)
            {
                gameOverTrigger.TriggerGameOver();
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PlayerStatusController] IGameOverTrigger が未設定のため、ゲームオーバー遷移ができません。", this);
            }
        }

        private MonoBehaviour FindInterfaceComponentOnSelf<T>() where T : class
        {
            var components = GetComponents<MonoBehaviour>();
            foreach (var component in components)
            {
                if (component is T)
                {
                    return component;
                }
            }

            return null;
        }
    }
}

