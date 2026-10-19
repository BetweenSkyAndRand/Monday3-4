namespace Game.Player
{
    /// <summary>
    /// プレイヤーの移動を一時的に禁止する要因を表すインターフェース。
    /// PlayerGridMover はこのインターフェースの配列のみを知り、
    /// ビルドモードや攻撃状態といった具体的な理由には依存しない。
    ///
    /// 実装クラスの例：
    ///   - BuildModeMovementBlocker （ビルドモード中に移動禁止）
    ///   - PlayerAttack             （攻撃モーション中に移動禁止）
    /// </summary>
    public interface IMovementBlocker
    {
        /// <summary>移動を禁止すべき状態なら true を返す。</summary>
        bool IsMovementBlocked { get; }
    }
}


