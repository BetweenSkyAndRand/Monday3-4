// ReSharper disable once CheckNamespace
namespace Game.Interaction
{
    /// <summary>
    /// プレイヤーがインタラクトできるオブジェクトの共通インターフェース。
    /// 表示テキストの取得と、実際のインタラクト処理のみを責務とする。
    /// </summary>
    public interface IInteractableObject
    {
        string GetInteractText();
        void Interact();
    }

    /// <summary>
    /// インタラクト対象の優先順位を指定したいオブジェクトが実装する（未実装は 0 扱い。値が大きいほど優先）。
    /// 同じ優先順位の中では距離が近いものが先になる。
    /// </summary>
    public interface IInteractPriority
    {
        int InteractPriority { get; }
    }
}