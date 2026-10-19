using Game.Combat;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// 敵がプレイヤー攻撃からダメージを受け取る窓口。
    /// floatダメージをEnemyVirusHealthComponentのuintダメージに変換して委譲する。
    /// </summary>
    public sealed class EnemyVirusDamageHandler : MonoBehaviour, IDamageable
    {
        [Header("デバッグ")]
        [SerializeField] private bool enableDamageReceiveDebugLog = true;

        [SerializeField] private EnemyVirusHealthComponent healthComponent;
        [SerializeField] private EnemyAnimationHandler animationHandler;

        private void Awake()
        {
            if (healthComponent == null)
            {
                healthComponent = GetComponent<EnemyVirusHealthComponent>();
            }

            if (animationHandler == null)
            {
                animationHandler = GetComponentInChildren<EnemyAnimationHandler>();
            }
        }

        public void TakeDamage(float amount)
        {
            if (amount <= 0f)
            {
                return;
            }

            if (healthComponent == null)
            {
                if (enableDamageReceiveDebugLog)
                {
                    UnityEngine.Debug.LogWarning("[EnemyVirusDamageHandler] EnemyVirusHealthComponent が見つかりません。", this);
                }

                return;
            }

            var convertedDamage = (uint)Mathf.CeilToInt(amount);
            if (enableDamageReceiveDebugLog)
            {
                UnityEngine.Debug.Log($"[EnemyVirusDamageHandler] ダメージ受信: enemy={gameObject.name}, raw={amount:F2}, converted={convertedDamage}", this);
            }

            // 被弾アニメーションを再生
            if (animationHandler != null)
            {
                animationHandler.PlayDamageAnimation();
            }

            healthComponent.TakeDamage(convertedDamage);
        }
    }
}

