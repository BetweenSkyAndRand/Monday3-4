using Game.BuildMode;
using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// ビルドモード（ダンジョン生成中）の BGM を切り替えるハンドラー。
    /// BuildModeEventManager.OnBuildModeChanged を購読し、Building 突入時にビルドモード用 BGM を、
    /// None に戻ったとき（ビルドモード終了時）にサーチ前 BGM へ戻す。
    /// </summary>
    public class BuildModeBgmHandler : MonoBehaviour
    {
        [Header("BGM ID 設定")]
        [Tooltip("ビルドモード（ダンジョン生成中）に再生する BGM の ID。")]
        [SerializeField] private string buildingBgmId = "build_mode_bgm";

        [Tooltip("ビルドモード終了時に戻すサーチ前 BGM の ID。")]
        [SerializeField] private string preSearchBgmId = "pre_search_bgm";

        [Header("フェード設定")]
        [SerializeField] private bool useFadeIn = true;
        [SerializeField] private float fadeInDuration = 1f;

        [Header("Debug")]
        [SerializeField] private bool enableTransitionLog = true;

        private BuildModeType lastBuildMode = BuildModeType.None;
        private void Awake()
        {
            UnityEngine.Debug.Log("[BuildModeBgmHandler] Awake called");
        }

        private void OnEnable()
        {
            UnityEngine.Debug.Log("[BuildModeBgmHandler] OnEnable called");
            BuildModeEventManager.OnBuildModeChanged += HandleBuildModeChanged;
        }

        private void OnDisable()
        {
            BuildModeEventManager.OnBuildModeChanged -= HandleBuildModeChanged;
        }

        private void HandleBuildModeChanged(BuildModeType newMode)
        {
            if (enableTransitionLog)
            {
                LogBuildModeTransition(lastBuildMode, newMode);
            }

            var audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                UnityEngine.Debug.LogError("[BuildModeBgmHandler] AudioManager が見つかりません。");
                lastBuildMode = newMode;
                return;
            }

            switch (newMode)
            {
                case BuildModeType.Building:
                    UnityEngine.Debug.Log($"[BuildModeBgmHandler] ダンジョン生成中の BGM を再生: {buildingBgmId}");
                    audioManager.PlayConfiguredBgm(buildingBgmId, useFadeIn, fadeInDuration);
                    break;

                case BuildModeType.None:
                    UnityEngine.Debug.Log($"[BuildModeBgmHandler] サーチ前 BGM に戻す: {preSearchBgmId}");
                    audioManager.PlayConfiguredBgm(preSearchBgmId, useFadeIn, fadeInDuration);
                    break;
            }

            lastBuildMode = newMode;
        }

        private void LogBuildModeTransition(BuildModeType oldMode, BuildModeType newMode)
        {
            if (oldMode == newMode)
            {
                return;
            }

            if (oldMode == BuildModeType.None && newMode == BuildModeType.Building)
            {
                UnityEngine.Debug.Log("[BuildModeBgmHandler] ビルドモードに入りました。");
                return;
            }

            if (oldMode == BuildModeType.Building && newMode == BuildModeType.None)
            {
                UnityEngine.Debug.Log("[BuildModeBgmHandler] ビルドモードを抜けました。");
                return;
            }

            UnityEngine.Debug.Log($"[BuildModeBgmHandler] ビルドモード遷移: {oldMode} -> {newMode}");
        }
    }
}



