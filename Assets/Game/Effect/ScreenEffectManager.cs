using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Effect
{
    /// <summary>
    /// 画面エフェクト（フェード・フラッシュ）を管理するシングルトンマネージャー。
    /// 責務: UGUI Canvas + Image を使った画面全体の演出のみ。
    /// パーティクル（VFX）は VFXManager が担当。
    /// </summary>
    public class ScreenEffectManager : MonoBehaviour
    {
        private static ScreenEffectManager instance;
        private static bool isDestroying;

        public static ScreenEffectManager Instance
        {
            get
            {
                if (isDestroying || !Application.isPlaying)
                {
                    return null;
                }

                if (instance == null)
                {
                    var go = new GameObject("ScreenEffectManager");
                    instance = go.AddComponent<ScreenEffectManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("設定")]
        [SerializeField] private Core.GameSettings settings;

        [Header("画面エフェクト用UI")]
        [SerializeField] private Canvas screenEffectCanvas;
        [SerializeField] private CanvasGroup screenEffectCanvasGroup;
        [SerializeField] private Image fadeImage;
        [SerializeField] private Image flashImage;

        private bool enableScreenEffects = true;
        private Coroutine fadeCoroutine;
        private Coroutine flashCoroutine;

        private void Awake()
        {
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

            InitializeScreenEffects();
            LoadSettings();
        }

        private void InitializeScreenEffects()
        {
            // 画面エフェクト用のCanvasが無い場合は生成
            if (screenEffectCanvas == null)
            {
                var canvasGo = new GameObject("ScreenEffectCanvas");
                canvasGo.transform.SetParent(transform);

                screenEffectCanvas = canvasGo.AddComponent<Canvas>();
                screenEffectCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
                screenEffectCanvas.sortingOrder = 9999; // 最前面に表示

                screenEffectCanvasGroup = canvasGo.AddComponent<CanvasGroup>();
                screenEffectCanvasGroup.interactable = false;
                screenEffectCanvasGroup.blocksRaycasts = false;
            }

            // フェード用Imageが無い場合は生成
            if (fadeImage == null)
            {
                var fadeGo = new GameObject("FadeImage");
                fadeGo.transform.SetParent(screenEffectCanvas.transform);
                fadeImage = fadeGo.AddComponent<Image>();
                fadeImage.color = new Color(0, 0, 0, 0);

                var rect = fadeImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;

                fadeImage.gameObject.SetActive(false);
            }

            // フラッシュ用Imageが無い場合は生成
            if (flashImage == null)
            {
                var flashGo = new GameObject("FlashImage");
                flashGo.transform.SetParent(screenEffectCanvas.transform);
                flashImage = flashGo.AddComponent<Image>();
                flashImage.color = new Color(1, 0, 0, 0);

                var rect = flashImage.rectTransform;
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.sizeDelta = Vector2.zero;
                rect.anchoredPosition = Vector2.zero;

                flashImage.gameObject.SetActive(false);
            }
        }

        private void LoadSettings()
        {
            if (settings != null)
            {
                settings.Load();
                ApplySettings(settings);
            }
        }

        public void ApplySettings(Core.GameSettings newSettings)
        {
            if (newSettings == null)
            {
                return;
            }

            enableScreenEffects = newSettings.enableScreenEffects;
        }

        /// <summary>画面を徐々に暗転させる（フェードアウト）。</summary>
        public void FadeOut(float duration, System.Action onComplete = null)
        {
            if (!enableScreenEffects)
            {
                onComplete?.Invoke();
                return;
            }

            StopFade();
            fadeCoroutine = StartCoroutine(FadeCoroutine(0f, 1f, duration, onComplete));
        }

        /// <summary>画面を徐々に明るくする（フェードイン）。</summary>
        public void FadeIn(float duration, System.Action onComplete = null)
        {
            if (!enableScreenEffects)
            {
                onComplete?.Invoke();
                return;
            }

            StopFade();
            fadeCoroutine = StartCoroutine(FadeCoroutine(1f, 0f, duration, onComplete));
        }

        private void StopFade()
        {
            if (fadeCoroutine != null)
            {
                StopCoroutine(fadeCoroutine);
                fadeCoroutine = null;
            }
        }

        private IEnumerator FadeCoroutine(float startAlpha, float endAlpha, float duration, System.Action onComplete)
        {
            fadeImage.gameObject.SetActive(true);
            var color = fadeImage.color;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                color.a = Mathf.Lerp(startAlpha, endAlpha, elapsed / duration);
                fadeImage.color = color;
                yield return null;
            }

            color.a = endAlpha;
            fadeImage.color = color;

            if (endAlpha <= 0f)
            {
                fadeImage.gameObject.SetActive(false);
            }

            onComplete?.Invoke();
            fadeCoroutine = null;
        }

        /// <summary>画面を一瞬光らせる（被弾演出など）。</summary>
        public void Flash(Color color, float duration = 0.2f)
        {
            if (!enableScreenEffects)
            {
                return;
            }

            StopFlash();
            flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
        }

        private void StopFlash()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
        }

        private IEnumerator FlashCoroutine(Color color, float duration)
        {
            flashImage.gameObject.SetActive(true);
            flashImage.color = color;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime;
                var alpha = Mathf.Lerp(color.a, 0f, elapsed / duration);
                flashImage.color = new Color(color.r, color.g, color.b, alpha);
                yield return null;
            }

            flashImage.color = new Color(color.r, color.g, color.b, 0f);
            flashImage.gameObject.SetActive(false);
            flashCoroutine = null;
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

