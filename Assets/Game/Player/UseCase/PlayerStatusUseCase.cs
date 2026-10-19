using Game.Player.Domain;

namespace Game.Player.UseCase
{
    /// <summary>
    /// プレイヤーステータスのビジネスロジック層。
    /// HPの増減ルール、死亡判定などゲームルールを担当。
    /// Unity API に依存しない純C#クラス。
    /// </summary>
    public class PlayerStatusUseCase
    {
        /// <summary>ダメージを適用する</summary>
        public PlayerStatus ApplyDamage(PlayerStatus currentStatus, float damageAmount)
        {
            if (damageAmount <= 0f)
            {
                return currentStatus;
            }

            return currentStatus.TakeDamage(damageAmount);
        }

        /// <summary>回復を適用する</summary>
        public PlayerStatus ApplyHeal(PlayerStatus currentStatus, float healAmount)
        {
            if (healAmount <= 0f)
            {
                return currentStatus;
            }

            return currentStatus.Heal(healAmount);
        }

        /// <summary>プレイヤーが死亡しているかどうかを判定</summary>
        public bool IsPlayerDead(PlayerStatus status)
        {
            return status.IsDead;
        }
    }
}

