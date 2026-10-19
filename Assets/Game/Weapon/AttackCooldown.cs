using UnityEngine;

namespace Game.Weapon
{
    /// <summary>
    /// 攻撃クールタイムの計測を担当する純粋 C# クラス。
    /// Time.time ベースで管理するため MonoBehaviour 不要。
    /// クールタイムのロジックのみを持ち、攻撃処理には関与しない。
    /// </summary>
    public class AttackCooldown
    {
        private readonly float cooldownTime;

        // 次に攻撃可能になる時刻
        private float nextAttackTime = 0f;

        public AttackCooldown(float cooldownTime)
        {
            this.cooldownTime = cooldownTime;
        }

        /// <summary>現在攻撃可能かどうか。</summary>
        public bool IsReady => Time.time >= nextAttackTime;

        /// <summary>次の攻撃まで残り何秒かを返す（UI 表示用）。</summary>
        public float Remaining => Mathf.Max(0f, nextAttackTime - Time.time);

        /// <summary>
        /// 攻撃が開始されたときに呼び出す。
        /// 次の攻撃可能時刻を更新する。
        /// </summary>
        public void Use()
        {
            nextAttackTime = Time.time + cooldownTime;
        }

        /// <summary>
        /// 武器切り替え時などにクールタイムをリセットする。
        /// nextAttackTime を 0 にして即時解放する。
        /// </summary>
        public void Reset()
        {
            nextAttackTime = 0f;
        }
    }
}

