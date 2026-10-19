using System.Collections;
using UnityEngine;
using Game.Presentation;

namespace Game.Audio
{
    /// <summary>
    /// BGM・SE を一元管理するシングルトンマネージャー。
    /// 音量調整、フェード、3Dサウンド再生をサポートする。
    /// 責務: BGM/SEの再生管理のみ。演出ロジックは持たない。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        private static AudioManager instance;
        private static bool isDestroying;

        public static AudioManager Instance
        {
            get
            {
                if (isDestroying || !Application.isPlaying)
                {
                    return null;
                }

                if (instance == null)
                {
                    var go = new GameObject("AudioManager");
                    instance = go.AddComponent<AudioManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("AudioSource")]
        [SerializeField] private AudioSource bgmSource;    // BGM専用
        [SerializeField] private AudioSource seSource;     // 2D SE用

        [Header("設定")]
        [SerializeField] private Core.GameSettings settings;    // 設定データへの参照

        // ─── 現在の音量（計算済み） ───
        private float currentMasterVolume = 1f;
        private float currentBgmVolume = 0.7f;
        private float currentSeVolume = 0.8f;

        // ─── フェード制御 ───
        private Coroutine bgmFadeCoroutine;

        // ─── イベント ───
        public event System.Action<float> OnBgmVolumeChanged;
        public event System.Action<float> OnSeVolumeChanged;

        private void Awake()
        {
            UnityEngine.Debug.Log("[AudioManager] Awake reached");
            // シングルトン設定
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
                return;
            }
            InitializeAudioSources();
            LoadSettings();
        }

        private void InitializeAudioSources()
        {
            // BGM AudioSource を生成（持っていない場合）
            if (bgmSource == null)
            {
                var bgmGo = new GameObject("BGM_AudioSource");
                bgmGo.transform.SetParent(transform);
                bgmSource = bgmGo.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
            }

            // SE AudioSource を生成（持っていない場合）
            if (seSource == null)
            {
                var seGo = new GameObject("SE_AudioSource");
                seGo.transform.SetParent(transform);
                seSource = seGo.AddComponent<AudioSource>();
                seSource.loop = false;
                seSource.playOnAwake = false;
            }
        }

        // ─── 設定の適用 ───

        /// <summary>ゲーム起動時に設定を読み込む。</summary>
        private void LoadSettings()
        {
            if (settings != null)
            {
                settings.Load();
                ApplySettings(settings);
            }
        }

        /// <summary>設定を適用する（外部から呼ばれる）。</summary>
        public void ApplySettings(Core.GameSettings newSettings)
        {
            if (newSettings == null)
            {
                return;
            }

            currentMasterVolume = newSettings.masterVolume;
            currentBgmVolume = newSettings.bgmVolume;
            currentSeVolume = newSettings.seVolume;

            UpdateBgmVolume();
            UpdateSeVolume();
        }

        private void UpdateBgmVolume()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = currentMasterVolume * currentBgmVolume;
            }
            OnBgmVolumeChanged?.Invoke(currentMasterVolume * currentBgmVolume);
        }

        private void UpdateSeVolume()
        {
            if (seSource != null)
            {
                seSource.volume = currentMasterVolume * currentSeVolume;
            }
            OnSeVolumeChanged?.Invoke(currentMasterVolume * currentSeVolume);
        }

        // ─── BGM 再生 ───

        /// <summary>BGM を再生する（フェードなし）。</summary>
        public void PlayBgm(AudioClip clip, bool loop = true)
        {
            if (clip == null || bgmSource == null)
            {
                return;
            }

            bgmSource.clip = clip;
            bgmSource.loop = loop;
            bgmSource.Play();
        }

        /// <summary>BGM をフェードインして再生する。</summary>
        public void PlayBgmWithFadeIn(AudioClip clip, float fadeTime = 1f)
        {
            if (clip == null || bgmSource == null)
            {
                return;
            }

            StopBgmFade();
            bgmSource.clip = clip;
            bgmSource.loop = true;
            bgmFadeCoroutine = StartCoroutine(FadeInCoroutine(fadeTime));
        }

        /// <summary>
        /// 中央演出設定に登録されたBGMをID指定で再生する。
        /// </summary>
        public void PlayConfiguredBgm(string bgmId, bool useFadeIn = false, float fadeTime = 1f)
        {
            var presentationSettings = GamePresentationSettings.Instance;
            if (presentationSettings == null)
            {
                UnityEngine.Debug.LogError($"[AudioManager] GamePresentationSettings が見つかりません。BGM ID: {bgmId}");
                return;
            }

            if (!presentationSettings.TryGetBgmPresentation(bgmId, out var bgmPresentation) || bgmPresentation.clip == null)
            {
                UnityEngine.Debug.LogError($"[AudioManager] BGM ID '{bgmId}' が GamePresentationSettings に登録されていません。");
                return;
            }

            UnityEngine.Debug.Log($"[AudioManager] BGM 切り替え: ID='{bgmId}', Clip='{bgmPresentation.clip.name}', FadeIn={useFadeIn}");

            if (useFadeIn)
            {
                PlayBgmWithFadeIn(bgmPresentation.clip, fadeTime);
                return;
            }

            PlayBgm(bgmPresentation.clip, bgmPresentation.loop);
        }

        /// <summary>BGM をフェードアウトして停止する。</summary>
        public void StopBgmWithFadeOut(float fadeTime = 1f)
        {
            StopBgmFade();
            bgmFadeCoroutine = StartCoroutine(FadeOutCoroutine(fadeTime));
        }

        /// <summary>BGM を即座に停止する。</summary>
        public void StopBgm()
        {
            StopBgmFade();
            if (bgmSource != null)
            {
                bgmSource.Stop();
            }
        }

        private void StopBgmFade()
        {
            if (bgmFadeCoroutine != null)
            {
                StopCoroutine(bgmFadeCoroutine);
                bgmFadeCoroutine = null;
            }
        }

        private IEnumerator FadeInCoroutine(float fadeTime)
        {
            bgmSource.volume = 0f;
            bgmSource.Play();

            var elapsed = 0f;
            var targetVolume = currentMasterVolume * currentBgmVolume;

            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(0f, targetVolume, elapsed / fadeTime);
                yield return null;
            }

            bgmSource.volume = targetVolume;
            bgmFadeCoroutine = null;
        }

        private IEnumerator FadeOutCoroutine(float fadeTime)
        {
            var startVolume = bgmSource.volume;
            var elapsed = 0f;

            while (elapsed < fadeTime)
            {
                elapsed += Time.unscaledDeltaTime;
                bgmSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / fadeTime);
                yield return null;
            }

            bgmSource.volume = 0f;
            bgmSource.Stop();
            bgmFadeCoroutine = null;
        }

        // ─── SE 再生 ───

        /// <summary>2D SE を再生する（UI音など）。</summary>
        public void PlaySe(AudioClip clip)
        {
            if (clip == null || seSource == null)
            {
                return;
            }

            seSource.PlayOneShot(clip, currentMasterVolume * currentSeVolume);
        }

        /// <summary>
        /// GamePresentationSettings に登録済みの SE を seId で再生する（2D）。
        /// 既存の `PlaySe(AudioClip)` は AudioClip 直渡し用、こちらは「BGM と同じく ID で管理する形式」。
        /// </summary>
        public void PlayConfiguredSe(string seId)
        {
            if (!TryResolveSeEntry(seId, out var clip, out var volumeScale))
            {
                return;
            }

            if (seSource == null)
            {
                return;
            }

            seSource.PlayOneShot(clip, currentMasterVolume * currentSeVolume * volumeScale);
        }

        /// <summary>
        /// GamePresentationSettings に登録済みの SE を seId で 3D 再生する。
        /// </summary>
        public void PlayConfiguredSe3D(string seId, Vector3 position)
        {
            if (!TryResolveSeEntry(seId, out var clip, out var volumeScale))
            {
                return;
            }

            AudioSource.PlayClipAtPoint(clip, position, currentMasterVolume * currentSeVolume * volumeScale);
        }

        /// <summary>
        /// seId から登録済み SE クリップと音量スケールを解決する。
        /// 登録が無い、クリップが null、GamePresentationSettings が存在しない場合は false を返す。
        /// </summary>
        private static bool TryResolveSeEntry(string seId, out AudioClip clip, out float volumeScale)
        {
            clip = null;
            volumeScale = 1f;

            if (string.IsNullOrEmpty(seId))
            {
                return false;
            }

            var presentationSettings = GamePresentationSettings.Instance;
            if (presentationSettings == null)
            {
                return false;
            }

            if (!presentationSettings.TryGetSePresentation(seId, out var entry) || entry.clip == null)
            {
                return false;
            }

            clip = entry.clip;
            // volumeScale が 0 の場合（未設定の SerializeField デフォルト）は 1 として扱う。
            // これは「SePresentationEntry を Inspector で追加した直後に音が鳴らない」混乱を避けるため。
            volumeScale = entry.volumeScale <= 0f ? 1f : entry.volumeScale;
            return true;
        }

        /// <summary>3D SE を指定位置で再生する（ワールド空間の音）。</summary>
        public void PlaySe3D(AudioClip clip, Vector3 position)
        {
            if (clip == null)
            {
                return;
            }

            // 簡易実装: AudioSource.PlayClipAtPoint を使用
            // 音量は手動で調整できないため、AudioMixerを使う方が本来は良い
            AudioSource.PlayClipAtPoint(clip, position, currentMasterVolume * currentSeVolume);
        }

        /// <summary>3D SE を指定位置で再生する（AudioSource プール版：将来実装）。</summary>
        public void PlaySe3DPooled(AudioClip clip, Vector3 position)
        {
            // TODO: ObjectPoolパターンで3D AudioSourceを使い回す実装
            // 現状は PlaySe3D() を使用
            PlaySe3D(clip, position);
        }
        /// <summary>
        /// 現在再生中の BGM 情報をコンソールに出力する（デバッグ用）。
        /// </summary>
        public void LogCurrentBgmStatus()
        {
            if (bgmSource == null)
            {
                UnityEngine.Debug.Log("[AudioManager] BGM AudioSource が存在しません。");
                return;
            }

            var clipName = bgmSource.clip != null ? bgmSource.clip.name : "None";
            var isPlaying = bgmSource.isPlaying;
            var volume = bgmSource.volume;
            var time = bgmSource.time;

            UnityEngine.Debug.Log($"[AudioManager BGM Status] " +
                                  $"Clip='{clipName}' | " +
                                  $"Playing={isPlaying} | " +
                                  $"Volume={volume:F2} | " +
                                  $"Time={time:F2}s");
        }

        private void OnApplicationQuit()
        {
            isDestroying = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}

