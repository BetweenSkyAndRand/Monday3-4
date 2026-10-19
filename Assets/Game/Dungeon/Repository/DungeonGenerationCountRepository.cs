// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// これまでにダンジョンを生成（部屋を追加）した累計回数を保持する。
    /// 崩壊してもリセットしない（敵強化の基準値として使うため）。保存・取得のみを担う。
    /// </summary>
    public class DungeonGenerationCountRepository
    {
        public int Count { get; private set; }

        public void Increment()
        {
            Count++;
        }
    }
}

