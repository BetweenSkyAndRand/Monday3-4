using UnityEngine;
using UnityEngine.UI;

namespace Game.Pause
{
    /// <summary>
    /// ポーズメニューの設定UIを管理するクラス。
    /// 音量調整スライダー、エフェクトON/OFFトグルを提供する。
    /// 責務: UI表示と設定値の反映のみ。設定ロジックは GameSettings に委譲。
    /// </summary>
    public class PauseSettingsView : MonoBehaviour
    {
        [Header("設定データ")]
        [SerializeField] private Core.GameSettings settings;

        [Header("音量スライダー")]
        [SerializeField] private Slider masterVolumeSlider;
        [SerializeField] private Slider bgmVolumeSlider;
        [SerializeField] private Slider seVolumeSlider;

        [Header("エフェクトトグル")]
        [SerializeField] private Toggle visualEffectsToggle;
        [SerializeField] private Toggle screenEffectsToggle;

        [Header("デバッグ")]
        [SerializeField] private bool enableDebugLog;

        private void Awake()
        {
            if (settings == null)
            {
                UnityEngine.Debug.LogError("[PauseSettingsView] GameSettings が未設定です。");
                return;
            }

            // スライダーのイベントを登録
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.AddListener(OnMasterVolumeChanged);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.AddListener(OnBgmVolumeChanged);
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.onValueChanged.AddListener(OnSeVolumeChanged);
            }

            // トグルのイベントを登録
            if (visualEffectsToggle != null)
            {
                visualEffectsToggle.onValueChanged.AddListener(OnVisualEffectsToggled);
            }

            if (screenEffectsToggle != null)
            {
                screenEffectsToggle.onValueChanged.AddListener(OnScreenEffectsToggled);
            }
        }

        private void OnEnable()
        {
            // ポーズメニューが開かれたら設定を読み込んで反映
            LoadAndApplySettings();
        }

        private void OnDestroy()
        {
            // イベントを解除
            if (masterVolumeSlider != null)
            {
                masterVolumeSlider.onValueChanged.RemoveListener(OnMasterVolumeChanged);
            }

            if (bgmVolumeSlider != null)
            {
                bgmVolumeSlider.onValueChanged.RemoveListener(OnBgmVolumeChanged);
            }

            if (seVolumeSlider != null)
            {
                seVolumeSlider.onValueChanged.RemoveListener(OnSeVolumeChanged);
            }

            if (visualEffectsToggle != null)
            {
                visualEffectsToggle.onValueChanged.RemoveListener(OnVisualEffectsToggled);
            }

            if (screenEffectsToggle != null)
            {
                screenEffectsToggle.onValueChanged.RemoveListener(OnScreenEffectsToggled);
            }
        }

        /// <summary>設定を読み込んでUIに反映する。</summary>
        private void LoadAndApplySettings()
        {
            if (settings == null)
            {
                return;
            }

            settings.Load();

            // スライダーに値を設定（イベント発火を抑制）
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

            // トグルに値を設定（イベント発火を抑制）
            if (visualEffectsToggle != null)
            {
                visualEffectsToggle.SetIsOnWithoutNotify(settings.enableVisualEffects);
            }

            if (screenEffectsToggle != null)
            {
                screenEffectsToggle.SetIsOnWithoutNotify(settings.enableScreenEffects);
            }

            if (enableDebugLog)
            {
                UnityEngine.Debug.Log($"[PauseSettingsView] 設定をUIに反映しました。Master: {settings.masterVolume}");
            }
        }

        // ─── スライダー変更イベント ───

        private void OnMasterVolumeChanged(float value)
        {
            if (settings == null)
            {
                return;
            }

            settings.masterVolume = value;
            settings.Save();
            settings.ApplySettings();

            if (enableDebugLog)
            {
                UnityEngine.Debug.Log($"[PauseSettingsView] マスター音量変更: {value}");
            }
        }

        private void OnBgmVolumeChanged(float value)
        {
            if (settings == null)
            {
                return;
            }

            settings.bgmVolume = value;
            settings.Save();
            settings.ApplySettings();

            if (enableDebugLog)
            {
                UnityEngine.Debug.Log($"[PauseSettingsView] BGM音量変更: {value}");
            }
        }

        private void OnSeVolumeChanged(float value)
        {
            if (settings == null)
            {
                return;
            }

            settings.seVolume = value;
            settings.Save();
            settings.ApplySettings();

            if (enableDebugLog)
            {
                UnityEngine.Debug.Log($"[PauseSettingsView] SE音量変更: {value}");
            }
        }

        // ─── トグル変更イベント ───

        private void OnVisualEffectsToggled(bool isOn)
        {
            if (settings == null)
            {
                return;
            }

            settings.enableVisualEffects = isOn;
            settings.Save();
            settings.ApplySettings();

            if (enableDebugLog)
            {
                UnityEngine.Debug.Log($"[PauseSettingsView] エフェクト表示: {isOn}");
            }
        }

        private void OnScreenEffectsToggled(bool isOn)
        {
            if (settings == null)
            {
                return;
            }

            settings.enableScreenEffects = isOn;
            settings.Save();
            settings.ApplySettings();

            if (enableDebugLog)
            {
                UnityEngine.Debug.Log($"[PauseSettingsView] 画面エフェクト: {isOn}");
            }
        }
    }
}

