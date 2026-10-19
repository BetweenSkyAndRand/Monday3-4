using UnityEngine;

namespace Game.Player.Domain
{
    /// <summary>
    /// プレイヤーステータスの不変データ表現。
    /// HPの状態を保持し、状態遷移は UseCase 経由でのみ行う。
    /// </summary>
    public readonly struct PlayerStatus
    {
        public readonly float MaxHp;
        public readonly float CurrentHp;

        /// <summary>プレイヤーが死亡しているかどうか</summary>
        public bool IsDead => CurrentHp <= 0f;

        public PlayerStatus(float maxHp, float currentHp)
        {
            MaxHp = Mathf.Max(0.01f, maxHp);
            CurrentHp = Mathf.Clamp(currentHp, 0f, MaxHp);
        }

        /// <summary>初期状態のPlayerStatusを生成（最大HP = 現在HP）</summary>
        public static PlayerStatus Create(float maxHp)
        {
            return new PlayerStatus(maxHp, maxHp);
        }

        /// <summary>ダメージを受けた状態の新しいStatusを返す</summary>
        public PlayerStatus TakeDamage(float damageAmount)
        {
            var newCurrentHp = Mathf.Max(0f, CurrentHp - damageAmount);
            return new PlayerStatus(MaxHp, newCurrentHp);
        }

        /// <summary>回復した状態の新しいStatusを返す</summary>
        public PlayerStatus Heal(float healAmount)
        {
            var newCurrentHp = Mathf.Min(MaxHp, CurrentHp + healAmount);
            return new PlayerStatus(MaxHp, newCurrentHp);
        }
    }
}

