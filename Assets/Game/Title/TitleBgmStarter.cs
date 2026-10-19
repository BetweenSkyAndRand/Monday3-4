using UnityEngine;

namespace Game.Title
{
    /// <summary>
    /// タイトルシーン専用のBGM再生スターター。
    /// Start() で AudioManager にBGM再生を依頼する。
    /// </summary>
    public class TitleBgmStarter : MonoBehaviour
    {
        [Tooltip("GamePresentationSettings に登録された BGM ID。空の場合は audioClip を直接使用する。")]
        [SerializeField] private string bgmId = "title_bgm";

        [Tooltip("bgmId 未設定時のフォールバック用 AudioClip。")]
        [SerializeField] private AudioClip audioClip;

        [Header("フェード設定")]
        [SerializeField] private bool useFadeIn = true;
        [SerializeField] private float fadeInDuration = 1f;

        private void Start()
        {
            var audioManager = Audio.AudioManager.Instance;
            if (audioManager == null)
            {
                return;
            }

            // ID 指定がある場合は GamePresentationSettings 経由で再生
            if (!string.IsNullOrEmpty(bgmId))
            {
                audioManager.PlayConfiguredBgm(bgmId, useFadeIn, fadeInDuration);
                return;
            }

            // フォールバック: AudioClip 直接指定
            if (audioClip != null)
            {
                if (useFadeIn)
                {
                    audioManager.PlayBgmWithFadeIn(audioClip, fadeInDuration);
                    return;
                }
                audioManager.PlayBgm(audioClip);
            }
        }
    }
}
