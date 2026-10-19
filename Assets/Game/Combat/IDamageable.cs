namespace Game.Combat
{
    /// <summary>
    /// ダメージを受けられるオブジェクトの共通インターフェース。
    /// プレイヤー攻撃処理（楠元）と敵ダメージ受付（松尾）を疎結合につなぐ。
    /// 敵の MonoBehaviour がこのインターフェースを実装する。
    /// </summary>
    public interface IDamageable
    {
        /// <summary>
        /// ダメージを受ける。
        /// </summary>
        /// <param name="amount">与えるダメージ量。</param>
        void TakeDamage(float amount);
    }
}

