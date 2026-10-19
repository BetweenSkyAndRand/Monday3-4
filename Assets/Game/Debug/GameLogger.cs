using UnityEngine;

namespace Game.Debug
{
    /// <summary>
    /// カテゴリ別にログを制御できるロガークラス。
    /// LogSettings ScriptableObject で各カテゴリのON/OFFを切り替えられる。
    ///
    /// 使用例:
    /// GameLogger.Log(LogCategory.Player, "プレイヤーが移動しました");
    /// GameLogger.LogWarning(LogCategory.Enemy, "敵のAI初期化に失敗");
    /// </summary>
    public static class GameLogger
    {
        private static LogSettings settings;
        private const string SettingsPath = "LogSettings";

        /// <summary>
        /// LogSettings をロードする（初回アクセス時に実行）
        /// </summary>
        private static LogSettings Settings
        {
            get
            {
                if (settings == null)
                {
                    settings = Resources.Load<LogSettings>(SettingsPath);

                    if (settings == null)
                    {
                        UnityEngine.Debug.LogWarning($"[GameLogger] LogSettings が {SettingsPath} に見つかりません。すべてのログが有効になります。");
                    }
                }
                return settings;
            }
        }

        /// <summary>
        /// カテゴリ別の通常ログ。
        /// </summary>
        public static void Log(LogCategory category, string message, Object context = null)
        {
            if (Settings != null && !Settings.IsEnabled(category))
            {
                return;
            }

            var prefix = GetColoredPrefix(category);
            UnityEngine.Debug.Log($"{prefix} {message}", context);
        }

        /// <summary>
        /// カテゴリ別の警告ログ。
        /// </summary>
        public static void LogWarning(LogCategory category, string message, Object context = null)
        {
            if (Settings != null && !Settings.IsEnabled(category))
            {
                return;
            }

            var prefix = GetColoredPrefix(category);
            UnityEngine.Debug.LogWarning($"{prefix} {message}", context);
        }

        /// <summary>
        /// カテゴリ別のエラーログ。
        /// エラーは常に出力する（ON/OFF設定を無視）。
        /// </summary>
        public static void LogError(LogCategory category, string message, Object context = null)
        {
            var prefix = GetColoredPrefix(category);
            UnityEngine.Debug.LogError($"{prefix} {message}", context);
        }

        /// <summary>
        /// カテゴリごとに色付きプレフィックスを返す。
        /// </summary>
        private static string GetColoredPrefix(LogCategory category)
        {
            return category switch
            {
                LogCategory.Player => "<color=cyan>[Player]</color>",
                LogCategory.Enemy => "<color=red>[Enemy]</color>",
                LogCategory.Inventory => "<color=green>[Inventory]</color>",
                LogCategory.Dungeon => "<color=magenta>[Dungeon]</color>",
                LogCategory.Combat => "<color=yellow>[Combat]</color>",
                LogCategory.UI => "<color=blue>[UI]</color>",
                LogCategory.Object => "<color=orange>[Object]</color>",
                LogCategory.System => "<color=white>[System]</color>",
                LogCategory.Other => "<color=grey>[Other]</color>",
                _ => "[Unknown]"
            };
        }

        /// <summary>
        /// ランタイムでLogSettingsを更新する（テスト用）。
        /// </summary>
        public static void SetSettings(LogSettings newSettings)
        {
            settings = newSettings;
        }
    }
}

