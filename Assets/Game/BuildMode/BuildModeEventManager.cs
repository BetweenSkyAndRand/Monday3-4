using System;

namespace Game.BuildMode
{
    /// <summary>
    /// ビルドモード変更イベントを管理するクラス。
    /// Repository から分離され、イベント通知の責務のみを担当する。
    /// BGM 切り替えなど演出系がこのイベントを購読する。
    /// </summary>
    public static class BuildModeEventManager
    {
        /// <summary>
        /// ビルドモード変更を通知するイベント。引数は新しいビルドモード。
        /// 同一フレーム中に値が変わらない場合は発火しない。
        /// </summary>
        public static event Action<BuildModeType> OnBuildModeChanged;

        /// <summary>
        /// ビルドモード変更を通知する。
        /// BuildModeRepository.SetBuildMode() 内から呼び出される。
        /// </summary>
        public static void NotifyBuildModeChanged(BuildModeType newMode)
        {
            OnBuildModeChanged?.Invoke(newMode);
        }
    }
}

