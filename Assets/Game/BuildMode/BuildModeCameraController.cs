using UnityEngine;
using Unity.Cinemachine;

// ReSharper disable once CheckNamespace
namespace Game.BuildMode
{
    /// <summary>
    /// ビルドモードのカメラ切り替えを担当するマネージャー。
    /// 状態管理は静的クラス BuildModeRepository が行い、このクラスは現在値を監視してカメラのみ制御する。
    /// </summary>
    public class BuildModeCameraController : MonoBehaviour
    {
        [Header("カメラ設定")]
        [SerializeField] private CinemachineCamera playerCamera;
        [SerializeField] private CinemachineCamera buildCamera;
        [SerializeField] private BuildCameraMove buildCameraMove;

        [Header("モード切り替え演出")]
        [SerializeField] private AudioClip buildModeStartSe;
        [SerializeField] private AudioClip buildModeEndSe;
        [SerializeField] private CanvasGroup colorOverlay;  // 色調変更用のCanvas（青みがかった色）
        [SerializeField] private float overlayAlpha = 0.3f;
        [SerializeField] private bool enableModeChangeEffect = true;

        /// <summary>
        /// 有効側カメラに与える優先度
        /// </summary>
        private const int ActivePriority = 10;
        /// <summary>
        /// 無効側カメラに与える優先度
        /// </summary>
        private const int InactivePriority = 0;
        /// <summary>
        /// 前回のビルドモードを記録比較用
        /// </summary>
        private BuildModeType currentBuildMode;

        private void Awake()
        {
            BuildModeRepository.SetBuildMode(BuildModeType.None);
        }
        /// <summary>
        /// 起動時に現在のビルドモードを読み取り、カメラ状態を反映する。
        /// </summary>
        private void Start()
        {
            currentBuildMode = BuildModeRepository.GetBuildMode();
            ApplyCameraMode(currentBuildMode==BuildModeType.Building);

        }

        /// <summary>
        /// ビルドモードの状態を定期的にチェックし、変化があればカメラを切り替える。
        /// BuildModeRepository は現状イベント通知を持たないため、ここではポーリングで監視する。
        /// </summary>
        private void Update()
        {
            var newBuildMode = BuildModeRepository.GetBuildMode();
            if (newBuildMode == currentBuildMode)
            {
                return;
            }
            currentBuildMode = newBuildMode;
            ApplyCameraMode(currentBuildMode == BuildModeType.Building);
        }
        // ── 将来対応メモ ──────────────────────────────────────────────
        // BuildModeRepository.OnBuildModeChanged イベントが有効化されたら
        // Update() ポーリングを廃止し、OnEnable/OnDisable でイベント購読に切り替えると
        // 毎フレームの PlayerPrefs 読み取りを無くせる。

        /// <summary>
        /// ビルドモードの状態に応じてカメラの優先度を切り替える。
        /// このメソッドは表示の反映だけを担当し、状態変更は行わない。
        /// </summary>
        /// <param name="isBuildMode">ビルドモードならtrue，通常モードなら false</param>
        private void ApplyCameraMode(bool isBuildMode)
        {
            if (playerCamera != null)
            {
                playerCamera.Priority = isBuildMode ? InactivePriority : ActivePriority;
            }

            if (buildCamera != null)
            {
                buildCamera.Priority = isBuildMode ? ActivePriority : InactivePriority;
            }

            // ビルドモード時のみカメラ移動コンポーネントを有効化
            if (buildCameraMove != null)
            {
                buildCameraMove.enabled = isBuildMode;
            }

            // モード切り替え演出を再生
            if (enableModeChangeEffect)
            {
                PlayModeChangeEffect(isBuildMode);
            }
        }

        /// <summary>モード切り替え演出を再生する。</summary>
        private void PlayModeChangeEffect(bool isBuildMode)
        {
            // 色調変更
            if (colorOverlay != null)
            {
                colorOverlay.alpha = isBuildMode ? overlayAlpha : 0f;
            }

            // SE再生
            var seClip = isBuildMode ? buildModeStartSe : buildModeEndSe;
            if (seClip != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    audioManager.PlaySe(seClip);
                }
            }
        }
    }
}
