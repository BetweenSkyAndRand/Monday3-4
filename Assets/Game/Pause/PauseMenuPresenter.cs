using UnityEngine.SceneManagement;

namespace Game.Pause
{
    /// <summary>
    /// ポーズメニューのプレゼンテーション層。
    /// PauseMenuView と PauseManager の橋渡しを行い、ビューの表示制御とイベント処理を担当する。
    /// </summary>
    public class PauseMenuPresenter
    {
        private readonly PauseMenuView view;
        private readonly Core.GameSettings settings;
        private readonly string titleSceneName;

        public PauseMenuPresenter(PauseMenuView view, Core.GameSettings settings, string titleSceneName)
        {
            this.view = view;
            this.settings = settings;
            this.titleSceneName = titleSceneName;
        }

        /// <summary>
        /// 初期化処理。PauseManager のイベントを購読する。
        /// </summary>
        public void Initialize()
        {
            var pauseManager = PauseManager.Instance;
            if (pauseManager != null)
            {
                pauseManager.OnPauseStateChanged += OnPauseStateChanged;
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PauseMenuPresenter] PauseManager のインスタンスが取得できませんでした");
            }
        }

        /// <summary>
        /// 破棄処理。PauseManager のイベント購読を解除する。
        /// </summary>
        public void Dispose()
        {
            if (PauseManager.Instance != null)
            {
                PauseManager.Instance.OnPauseStateChanged -= OnPauseStateChanged;
            }
        }

        /// <summary>
        /// PauseManager の状態が変わったときにビューを更新する。
        /// </summary>
        private void OnPauseStateChanged(bool isPaused)
        {
            if (isPaused)
            {
                view.Show();
                view.UpdateSliderValues();
            }
            else
            {
                view.Hide();
            }
        }

        /// <summary>
        /// Resume ボタンがクリックされたときの処理。
        /// </summary>
        public void OnResumeButtonClicked()
        {
            var pauseManager = PauseManager.Instance;
            if (pauseManager != null)
            {
                pauseManager.Resume();
            }
        }

        /// <summary>
        /// Return to Title ボタンがクリックされたときの処理。
        /// </summary>
        public void OnReturnToTitleButtonClicked()
        {
            // ポーズを解除（Time.timeScaleを戻す）
            var pauseManager = PauseManager.Instance;
            if (pauseManager != null && pauseManager.IsPaused)
            {
                pauseManager.Resume();
            }

            // タイトルシーンへ遷移
            LoadTitleScene();
        }

        /// <summary>
        /// マスター音量スライダーが変更された時の処理。
        /// </summary>
        public void OnMasterVolumeChanged(float value)
        {
            if (settings == null)
            {
                return;
            }

            settings.masterVolume = value;
            settings.Save();
            settings.ApplySettings();
        }

        /// <summary>
        /// BGM 音量スライダーが変更された時の処理。
        /// </summary>
        public void OnBgmVolumeChanged(float value)
        {
            if (settings == null)
            {
                return;
            }

            settings.bgmVolume = value;
            settings.Save();
            settings.ApplySettings();
        }

        /// <summary>
        /// SE 音量スライダーが変更された時の処理。
        /// </summary>
        public void OnSeVolumeChanged(float value)
        {
            if (settings == null)
            {
                return;
            }

            settings.seVolume = value;
            settings.Save();
            settings.ApplySettings();
        }

        /// <summary>
        /// タイトルシーンへ遷移する処理。
        /// </summary>
        private void LoadTitleScene()
        {
            SceneManager.LoadScene(titleSceneName);
        }
    }
}


