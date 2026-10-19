using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// セルのサイズや色などの表示パラメーターを保持する ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "DungeonCellParameter", menuName = "Dungeon/CellParameter")]
    public class DungeonCellParameter : ScriptableObject
    {
        [SerializeField] private float cellSize = 1f;
        [SerializeField] private Color colorPlaceable = new Color(0f, 1f, 0f, 0.4f);
        [SerializeField] private Color colorBlocked   = new Color(1f, 0f, 0f, 0.4f);

        /// <summary>セル 1 つのワールド幅（1 → 1m）</summary>
        public float CellSize => cellSize;

        /// <summary>配置可能時のセル色（緑）</summary>
        public Color ColorPlaceable => colorPlaceable;

        /// <summary>配置不可時のセル色（赤）</summary>
        public Color ColorBlocked => colorBlocked;
    }
}

