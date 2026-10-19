using UnityEngine;

namespace Game.CollapseTimer.Parameter
{
    /// <summary>
    /// ダンジョン崩壊タイマーのパラメーター設定
    /// </summary>
    public class DungeonCollapseTimerParameter : ScriptableObject
    {
        [SerializeField] private float initialDurationSeconds = 180f;

        public float InitialDurationSeconds => initialDurationSeconds;
    }
}

