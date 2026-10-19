// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// 部屋配置の判定結果。配置不可の理由をUIに伝えるために bool ではなく列挙型にしている。
    /// </summary>
    public enum DungeonPlacementResult
    {
        Placeable,
        Overlapped,
        NoNeighbor,
        RoomLimitReached
    }
}
