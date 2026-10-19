namespace Game.Player
{
    /// <summary>
    /// プレイヤーのインタラクトを一時的に禁止する要因を表すインターフェース。
    /// PlayerInteract はこのインターフェースの配列のみを知り、
    /// ビルドモードといった具体的な理由には依存しない設計にする。
    ///
    /// 実装クラスの例：
    ///   - BuildModeMovementBlocker （ビルドモード中にインタラクト禁止）
    /// </summary>
    public interface IInteractBlocker
    {
        /// <summary>インタラクトを禁止すべき状態なら true を返す。</summary>
        bool IsInteractBlocked { get; }
    }
}

