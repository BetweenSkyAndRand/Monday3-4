using UnityEngine;
using Game.Combat;
using Game.Player;
using Game.Player.Presentation;
using Game.GameOver;

namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤーのダメージ処理を管理するクラス。
    /// IDamageableを実装し、敵からの攻撃を受け付ける。
    /// ダメージを受けると HP が減る。
    /// また IPlayerLethalDamageHandler を実装し、ダンジョン崩壊など
    /// 通常のダメージ計算を経由しない即死イベントも受け付ける。
    /// </summary>
    public class PlayerDamageHandler : MonoBehaviour, IDamageable, IPlayerLethalDamageHandler
    {
        [Header("参照")]
        [SerializeField] private PlayerStatusController playerStatusController;
        [SerializeField] private PlayerHitEffect hitEffect;
        [SerializeField] private PlayerAnimationHandler animationHandler;

        [Header("即死イベント（ダンジョン崩壊等）")]
        [Tooltip("GameOverHandler をアタッチしたオブジェクトをここに設定する（IGameOverTrigger実装）。playerRespawnController が設定されている場合はフォールバック用途のみに使用される。")]
        [SerializeField] private MonoBehaviour gameOverTriggerObject;
        [SerializeField] private PlayerDownStateBlocker playerDownStateBlocker;

        [Header("リスポーン処理（未設定の場合は従来通りゲームオーバーへフォールバックする）")]
        [Tooltip("設定されている場合、Kill() は復活フロー（死亡回数加算・治療費支払い・リスポーン）へ委譲する。")]
        [SerializeField] private MonoBehaviour playerRevivalHandlerObject;

        [Header("互換フォールバック（旧リスポーン処理）")]
        [Tooltip("playerRevivalHandlerObject が未設定の場合のみ使用する。")]
        [SerializeField] private PlayerRespawnController playerRespawnController;

        private IGameOverTrigger gameOverTrigger;
        private IPlayerRevivalHandler playerRevivalHandler;


        private void Awake()
        {
            if (playerStatusController == null)
            {
                playerStatusController = FindFirstObjectByType<PlayerStatusController>();
                if (playerStatusController == null)
                {
                    UnityEngine.Debug.LogWarning("[PlayerDamageHandler] PlayerStatusController が見つかりません。");
                }
            }

            if (hitEffect == null)
            {
                hitEffect = GetComponent<PlayerHitEffect>();
            }

            if (animationHandler == null)
            {
                animationHandler = GetComponentInChildren<PlayerAnimationHandler>();
            }

            if (playerDownStateBlocker == null)
            {
                playerDownStateBlocker = GetComponent<PlayerDownStateBlocker>();
            }

            playerRevivalHandler = playerRevivalHandlerObject as IPlayerRevivalHandler;
            if (playerRevivalHandlerObject != null && playerRevivalHandler == null)
            {
                UnityEngine.Debug.LogWarning("[PlayerDamageHandler] playerRevivalHandlerObject が IPlayerRevivalHandler を実装していません。", this);
            }

            if (playerRevivalHandler == null)
            {
                playerRevivalHandler = GetComponent<IPlayerRevivalHandler>();
            }

            // Unity は interface の SerializeField をサポートしないため、実行時にキャストする
            if (gameOverTriggerObject != null)
            {
                gameOverTrigger = gameOverTriggerObject as IGameOverTrigger;
                if (gameOverTrigger == null)
                {
                    UnityEngine.Debug.LogWarning("[PlayerDamageHandler] gameOverTriggerObject が IGameOverTrigger を実装していません。", this);
                }
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PlayerDamageHandler] gameOverTriggerObject が未設定です。即死イベント（Kill）が機能しません。", this);
            }
        }

        /// <summary>
        /// ダメージを受ける（IDamageableの実装）。
        /// 受けたダメージをプレイヤーHPに適用する。
        /// </summary>
        /// <param name="amount">受けるダメージ量</param>
        public void TakeDamage(float amount)
        {
            if (amount <= 0)
            {
                return;
            }

            UnityEngine.Debug.Log($"[PlayerDamageHandler] ダメージを受けました: {amount}");

            // 被弾演出を再生
            if (hitEffect != null)
            {
                hitEffect.PlayHitEffect();
            }

            // 被弾アニメーションを再生
            if (animationHandler != null)
            {
                animationHandler.PlayDamageAnimation();
            }

            // HP減少を適用
            if (playerStatusController != null)
            {
                playerStatusController.ApplyDamage(amount);
            }
        }

        // ─── IPlayerLethalDamageHandler ────────────────────────────────

        /// <summary>
        /// プレイヤーを即座に死亡させる（IPlayerLethalDamageHandlerの実装）。
        /// ダンジョン崩壊など、通常のダメージ計算（HP）を経由しない即死イベントから呼ばれる。
        /// playerRespawnController が設定されている場合はリスポーン処理へ委譲する。
        /// 未設定の場合のみ、従来通り GameOver 遷移へフォールバックする（後方互換）。
        /// </summary>
        public void Kill()
        {
            UnityEngine.Debug.Log("[PlayerDamageHandler] 即死イベントを受信しました。");

            if (playerDownStateBlocker != null)
            {
                playerDownStateBlocker.SetDown();
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PlayerDamageHandler] playerDownStateBlocker が未設定です。", this);
            }
            UnityEngine.Debug.Log($"[PlayerDamageHandler] playerRevivalHandler is null: {playerRevivalHandler == null}");
            UnityEngine.Debug.Log($"[PlayerDamageHandler] playerRespawnController is null: {playerRespawnController == null}");
            if (playerRevivalHandler != null)
            {
                playerRevivalHandler.HandleDeath();
                return;
            }

            if (playerRespawnController != null)
            {
                playerRespawnController.HandleDeath();
                return;
            }

            UnityEngine.Debug.LogWarning("[PlayerDamageHandler] playerRespawnController が未設定です。従来通りゲームオーバーへフォールバックします。", this);

            if (gameOverTrigger != null)
            {
                gameOverTrigger.TriggerGameOver();
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PlayerDamageHandler] gameOverTriggerObject が未設定のため、即死イベントを処理できません。", this);
            }
        }
    }
}


