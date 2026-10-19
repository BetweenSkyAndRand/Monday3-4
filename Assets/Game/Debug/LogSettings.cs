using System;
using UnityEngine;

namespace Game.Debug
{
    /// <summary>
    /// デバッグログのカテゴリ別ON/OFF設定を保持するScriptableObject。
    /// Inspector で各カテゴリのログを個別に有効化/無効化できる。
    /// </summary>
    [CreateAssetMenu(fileName = "LogSettings", menuName = "Game/Debug/LogSettings")]
    public class LogSettings : ScriptableObject
    {
        [Header("ログカテゴリON/OFF設定")]
        [Tooltip("チェックを入れたカテゴリのログのみが出力されます")]
        public bool enablePlayerLogs = true;
        public bool enableEnemyLogs = false;
        public bool enableInventoryLogs = true;
        public bool enableDungeonLogs = false;
        public bool enableCombatLogs = true;
        public bool enableUILogs = true;
        public bool enableObjectLogs = true;
        public bool enableSystemLogs = true;
        public bool enableOtherLogs = true;

        [Header("全体設定")]
        [Tooltip("すべてのカテゴリを無効化（リリース時などに使用）")]
        public bool disableAllLogs = false;

        /// <summary>
        /// 指定されたカテゴリのログが有効かどうかを返す。
        /// </summary>
        public bool IsEnabled(LogCategory category)
        {
            if (disableAllLogs)
            {
                return false;
            }

            return category switch
            {
                LogCategory.Player => enablePlayerLogs,
                LogCategory.Enemy => enableEnemyLogs,
                LogCategory.Inventory => enableInventoryLogs,
                LogCategory.Dungeon => enableDungeonLogs,
                LogCategory.Combat => enableCombatLogs,
                LogCategory.UI => enableUILogs,
                LogCategory.Object => enableObjectLogs,
                LogCategory.System => enableSystemLogs,
                LogCategory.Other => enableOtherLogs,
                _ => true
            };
        }
    }
}

