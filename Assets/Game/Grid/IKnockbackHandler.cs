namespace Game.Grid
{
    /// <summary>
    /// 吹き飛ばし処理を受け取るインターフェース。
    /// プレイヤーや他のキャラクターと衝突した際に呼び出される。
    /// </summary>
    public interface IKnockbackHandler
    {
        /// <summary>
        /// 吹き飛ばしを受けた時に呼び出される。
        /// 実装側が吹き飛ばし処理の詳細を決定する。
        /// </summary>
        /// <param name="fromPosition">吹き飛ばす元の位置（プレイヤーなど）</param>
        /// <param name="cellSize">グリッドのセルサイズ</param>
        void OnKnockback(UnityEngine.Vector3 fromPosition, float cellSize);
    }
}

