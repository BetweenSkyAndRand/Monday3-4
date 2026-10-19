// ReSharper disable once CheckNamespace
namespace Game.Search
{
    /// <summary>
    /// 旧クラス名との後方互換用ラッパー。
    /// 既存シーンやプレハブの参照切れを避けるため残している。
    /// 新規実装では SearchEndInteractableView を使用すること。
    /// </summary>
    [System.Obsolete("Use SearchEndInteractableView instead.")]
    public class SearchEndObject : SearchEndInteractableView
    {
    }
}