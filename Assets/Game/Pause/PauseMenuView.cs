using Game.GroundItem;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Pause
{
    /// <summary>
    /// ポーズメニューのUI表示とボタン制御を担当するビュークラス。
    /// ロジックは PauseMenuPresenter に委譲している。
    /// 音量調整スライダーを管理し、ボタンイベントを Presenter へ橋渡しする。
    /// </summary>
    public class PauseMenuView : MonoBehaviour
    {
        [Header("UI設定")]
        [SerializeField] private GameObject pauseMenuPanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button returnToTitleButton;

        [Header("音量調整")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider seVolumeSlider;

        [Header("死亡時ドロップ")]
        [Tooltip("ONなら死亡時に初期武器以外のアイテムを地面へ落とす。未設定でも動作する。")]
        [SerializeField] private Toggle dropItemsOnDeathToggle;

        [Header("設定")]
        [SerializeField] private Core.GameSettings settings;

        [Header("シーン設定")]
        [SerializeField] private string titleSceneName = "TitleScene";

        private PauseMenuPresenter presenter;

        private void Awake()
        {
            // Presenter の初期化
            presenter = new PauseMenuPresenter(this, settings, titleSceneName);

            // 設定を読み込む
            if (settings != null)
            {
                settings.Load();
            }

            // 初期状態では非表示
            Hide();

            // ボタンイベントを設定
            if (resumeButton != null)
            {
                resumeButton.onClick.AddListener(() => presenter.OnResumeButtonClicked());
                resumeButton.interactable = true;
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PauseMenuView] resumeButton が未アサインです");
            }

            if (returnToTitleButton != null)
            {
                returnToTitleButton.onClick.AddListener(() => presenter.OnReturnToTitleButtonClicked());
                returnToTitleButton.interactable = true;
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PauseMenuView] returnToTitleButton が未アサインです");
            }

            if (dropItemsOnDeathToggle != null)
            {
                dropItemsOnDeathToggle.onValueChanged.AddListener(isOn => GroundItemRuntime.DropItemsOnDeath = isOn);
            }

            // 音量スライダーのイベント設定
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(presenter.OnMasterVolumeChanged);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.AddListener(presenter.OnBgmVolumeChanged);
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.onValueChanged.AddListener(presenter.OnSeVolumeChanged);
            }
        }

        private void OnEnable()
        {
            // Presenter の初期化（PauseManager イベント購読）
            presenter?.Initialize();
        }

        private void OnDisable()
        {
            // Presenter の破棄（PauseManager イベント購読解除）
            presenter?.Dispose();
        }

        private void OnDestroy()
        {
            // ボタンイベント解除
            if (resumeButton != null)
            {
                resumeButton.onClick.RemoveAllListeners();
            }

            if (returnToTitleButton != null)
            {
                returnToTitleButton.onClick.RemoveAllListeners();
            }

            if (dropItemsOnDeathToggle != null)
            {
                dropItemsOnDeathToggle.onValueChanged.RemoveAllListeners();
            }

            // スライダーイベント解除
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveAllListeners();
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveAllListeners();
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.onValueChanged.RemoveAllListeners();
            }

            // Presenter の破棄
            presenter?.Dispose();
        }

        /// <summary>
        /// ポーズメニューを表示する（Presenter から呼び出される）。
        /// </summary>
        public void Show()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(true);
            }
        }

        /// <summary>
        /// ポーズメニューを非表示にする（Presenter から呼び出される）。
        /// </summary>
        public void Hide()
        {
            if (pauseMenuPanel != null)
            {
                pauseMenuPanel.SetActive(false);
            }
        }

        /// <summary>
        /// スライダーの値を現在の設定に同期する（Presenter から呼び出される）。
        /// </summary>
        public void UpdateSliderValues()
        {
            if (dropItemsOnDeathToggle != null)
            {
                dropItemsOnDeathToggle.SetIsOnWithoutNotify(GroundItemRuntime.DropItemsOnDeath);
            }

            if (settings == null)
            {
                return;
            }

            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.SetValueWithoutNotify(settings.masterVolume);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.SetValueWithoutNotify(settings.bgmVolume);
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.SetValueWithoutNotify(settings.seVolume);
            }
        }
    }
}

