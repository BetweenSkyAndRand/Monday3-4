// ReSharper disable once CheckNamespace

namespace Game.BuildMode
{
    /// <summary>
    /// ビルドモードの状態を一元管理する静的リポジトリ。
    /// このクラスは今のモードを保存する責務のみを持ち、切り替えのロジックは持たない。
    /// イベント通知は BuildModeEventManager に委譲している。
    ///
    /// インメモリ管理（static フィールド）を採用している。
    /// PlayerPrefs による永続化は行わない。ゲーム起動のたびに None からスタートするのが正しい挙動のため。
    /// （以前 PlayerPrefs を使用していたが、Building 状態のまま終了すると次回起動時も
    ///   Building 状態で始まるバグがあったため廃止した）
    /// </summary>
    public static class BuildModeRepository
    {
        /// <summary>
        /// 現在のビルドモード。ゲーム起動時は None から始まる。
        /// </summary>
        private static BuildModeType currentMode = BuildModeType.None;

        /// <summary>
        /// 現在のビルドモードを保存する。
        /// 呼び出し側は［どの状態にするか］だけを決め、このクラスに保存を移譲する。
        /// 値が変更された場合、BuildModeEventManager 経由でイベントを発火する。
        /// </summary>
        /// <param name="buildModeType">保存したいビルドモード</param>
        public static void SetBuildMode(BuildModeType buildModeType)
        {
            if (currentMode == buildModeType)
            {
                return;
            }

            currentMode = buildModeType;

            // 値が変わった時のみ BuildModeEventManager 経由で通知する
            BuildModeEventManager.NotifyBuildModeChanged(buildModeType);
        }

        /// <summary>
        /// 現在のビルドモードを取得する。
        /// </summary>
        /// <returns>現在のビルドモード</returns>
        public static BuildModeType GetBuildMode()
        {
            return currentMode;
        }
    }
}
