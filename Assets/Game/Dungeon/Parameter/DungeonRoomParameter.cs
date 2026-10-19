using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// 部屋を構成するセル数などのパラメーターを保持する ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonRoomParameter", menuName = "Dungeon/RoomParameter")]
    public class DungeonRoomParameter : ScriptableObject
    {
        [SerializeField] private int roomWidth  = 13;
        [SerializeField] private int roomHeight = 13;

        [Header("部屋数の上限")]
        [Tooltip("配置できる部屋数の上限（中央の拠点部屋は数えない）。この値を変えるだけで上限を変更できる。")]
        [Min(1)]
        [SerializeField] private int maxPlacedRoomCount = 60;

        /// <summary>部屋の横セル数</summary>
        public int RoomWidth  => roomWidth;

        /// <summary>部屋の縦セル数</summary>
        public int RoomHeight => roomHeight;

        /// <summary>配置できる部屋数の上限（中央の拠点部屋を除く）</summary>
        public int MaxPlacedRoomCount => maxPlacedRoomCount;
    }
}

