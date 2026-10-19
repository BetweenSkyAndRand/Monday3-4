using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// ゲーム全体の設定を管理する ScriptableObject。
    /// PlayerPrefs で設定を保存・読み込みする。
    /// 音量、エフェクトのON/OFFなど、ユーザーが変更可能な設定を一元管理する。
    /// </summary>
    [CreateAssetMenu(fileName = "GameSettings", menuName = "Game/Core/Game Settings")]
    public class GameSettings : ScriptableObject
    {
        // ─── 音量設定 ───
        [Header("音量設定（0.0 ～ 1.0）")]
        [Range(0f, 1f)] public float masterVolume = 1f;
        [Range(0f, 1f)] public float bgmVolume = 0.7f;
        [Range(0f, 1f)] public float seVolume = 0.8f;

        // ─── エフェクト設定 ───
        [Header("エフェクト設定")]
        public bool enableVisualEffects = true;  // エフェクト表示ON/OFF
        public bool enableScreenEffects = true;  // 画面エフェクト（フラッシュ等）ON/OFF

        // ─── その他の設定 ───
        [Header("その他")]
        public bool enableVibration = true;  // 将来的なコントローラー振動用

        // ─── PlayerPrefs キー定数 ───
        private const string KeyMasterVolume = "Settings_MasterVolume";
        private const string KeyBgmVolume = "Settings_BgmVolume";
        private const string KeySeVolume = "Settings_SeVolume";
        private const string KeyEnableVisualEffects = "Settings_EnableVisualEffects";
        private const string KeyEnableScreenEffects = "Settings_EnableScreenEffects";
        private const string KeyEnableVibration = "Settings_EnableVibration";

        // ─── 保存・読み込み ───

        /// <summary>現在の設定を PlayerPrefs に保存する。</summary>
        public void Save()
        {
            PlayerPrefs.SetFloat(KeyMasterVolume, masterVolume);
            PlayerPrefs.SetFloat(KeyBgmVolume, bgmVolume);
            PlayerPrefs.SetFloat(KeySeVolume, seVolume);
            PlayerPrefs.SetInt(KeyEnableVisualEffects, enableVisualEffects ? 1 : 0);
            PlayerPrefs.SetInt(KeyEnableScreenEffects, enableScreenEffects ? 1 : 0);
            PlayerPrefs.SetInt(KeyEnableVibration, enableVibration ? 1 : 0);
            PlayerPrefs.Save();

            UnityEngine.Debug.Log("[GameSettings] 設定を保存しました。");
        }

        /// <summary>PlayerPrefs から設定を読み込む。</summary>
        public void Load()
        {
            masterVolume = PlayerPrefs.GetFloat(KeyMasterVolume, 1f);
            bgmVolume = PlayerPrefs.GetFloat(KeyBgmVolume, 0.7f);
            seVolume = PlayerPrefs.GetFloat(KeySeVolume, 0.8f);
            enableVisualEffects = PlayerPrefs.GetInt(KeyEnableVisualEffects, 1) == 1;
            enableScreenEffects = PlayerPrefs.GetInt(KeyEnableScreenEffects, 1) == 1;
            enableVibration = PlayerPrefs.GetInt(KeyEnableVibration, 1) == 1;

            UnityEngine.Debug.Log($"[GameSettings] 設定を読み込みました。Master: {masterVolume}, BGM: {bgmVolume}, SE: {seVolume}");
        }

        /// <summary>設定をデフォルト値にリセットする。</summary>
        public void ResetToDefault()
        {
            masterVolume = 1f;
            bgmVolume = 0.7f;
            seVolume = 0.8f;
            enableVisualEffects = true;
            enableScreenEffects = true;
            enableVibration = true;
            Save();

            UnityEngine.Debug.Log("[GameSettings] 設定をデフォルトにリセットしました。");
        }

        /// <summary>設定変更を各マネージャーに適用する。</summary>
        public void ApplySettings()
        {
            var audioManager = Audio.AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.ApplySettings(this);
            }

            var vfxManager = Effect.VFXManager.Instance;
            if (vfxManager != null)
            {
                vfxManager.ApplySettings(this);
            }

            var screenEffectManager = Effect.ScreenEffectManager.Instance;
            if (screenEffectManager != null)
            {
                screenEffectManager.ApplySettings(this);
            }
        }
    }
}


