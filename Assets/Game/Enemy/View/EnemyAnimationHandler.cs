using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// 敵モデルのアニメーション（攻撃・被弾・死亡）を管理するコンポーネント。
    /// Animator の各トリガーを呼び出す薄いラッパー。
    /// 敵モデルが Generic リグの場合、アニメーションクリップは
    /// EnemyCombatAnimatorController の各ステートに手動で割り当てる必要がある。
    /// モデルを Humanoid に変更すれば Human Animations パックのクリップが使用可能になる。
    /// </summary>
    public class EnemyAnimationHandler : MonoBehaviour
    {
        /// <summary>アニメーターのトリガー名定数。AnimatorController 側と一致させること。</summary>
        public const string TriggerAttack = "Attack";
        public const string TriggerDamage = "Damage";
        public const string TriggerDeath = "Death";

        // StringToHash でハッシュをキャッシュし、毎フレームの文字列変換コストを省く。
        private static readonly int hashAttack = Animator.StringToHash(TriggerAttack);
        private static readonly int hashDamage = Animator.StringToHash(TriggerDamage);
        private static readonly int hashDeath = Animator.StringToHash(TriggerDeath);

        [Header("参照")]
        [Tooltip("敵モデルの Animator。未設定の場合は自身および子オブジェクトから自動検索する。")]
        [SerializeField] private Animator enemyAnimator;

        private void Awake()
        {
            if (enemyAnimator == null)
            {
                enemyAnimator = GetComponentInChildren<Animator>();
            }
        }

        /// <summary>
        /// 攻撃アニメーションを再生する。
        /// Attack トリガーを発火し Attack ステートへ遷移させる。
        /// </summary>
        public void PlayAttackAnimation()
        {
            if (enemyAnimator == null)
            {
                return;
            }

            enemyAnimator.ResetTrigger(hashAttack);
            enemyAnimator.SetTrigger(hashAttack);
        }

        /// <summary>
        /// 被弾アニメーションを再生する。
        /// Damage トリガーを発火し TakeHit ステートへ遷移させる。
        /// </summary>
        public void PlayDamageAnimation()
        {
            if (enemyAnimator == null)
            {
                return;
            }

            enemyAnimator.ResetTrigger(hashDamage);
            enemyAnimator.SetTrigger(hashDamage);
        }

        /// <summary>
        /// 死亡アニメーションを再生する。
        /// Death トリガーを発火し Death ステートへ遷移させる（遷移後はループしない）。
        /// </summary>
        public void PlayDeathAnimation()
        {
            if (enemyAnimator == null)
            {
                return;
            }

            enemyAnimator.ResetTrigger(hashDeath);
            enemyAnimator.SetTrigger(hashDeath);
        }
    }
}
