using System.Collections;
using UnityEngine;

namespace Game.UI
{
    /// <summary>
    /// UI要素にフェードイン・フェードアウト・スライドアニメーションを提供するヘルパークラス。
    /// CanvasGroupを使用してアルファ値を変更し、スムーズな演出を実現する。
    /// 責務: UIアニメーションの再生のみ。
    /// </summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class UIFadeAnimation : MonoBehaviour
    {
        [Header("アニメーション設定")]
        [SerializeField] private float fadeTime = 0.3f;
        [SerializeField] private bool hideOnAwake;

        private CanvasGroup canvasGroup;
        private Coroutine currentAnimation;

        private void Awake()
        {
            canvasGroup = GetComponent<CanvasGroup>();

            if (hideOnAwake && canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                gameObject.SetActive(false);
            }
        }

        /// <summary>フェードインして表示する。</summary>
        public void FadeIn()
        {
            gameObject.SetActive(true);
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(FadeCoroutine(0f, 1f, fadeTime));
        }

        /// <summary>フェードアウトして非表示にする。</summary>
        public void FadeOut()
        {
            StopCurrentAnimation();
            currentAnimation = StartCoroutine(FadeCoroutine(1f, 0f, fadeTime, () =>
            {
                gameObject.SetActive(false);
            }));
        }

        /// <summary>即座に表示する（フェードなし）。</summary>
        public void Show()
        {
            StopCurrentAnimation();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
            }
            gameObject.SetActive(true);
        }

        /// <summary>即座に非表示にする（フェードなし）。</summary>
        public void Hide()
        {
            StopCurrentAnimation();
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
            }
            gameObject.SetActive(false);
        }

        private void StopCurrentAnimation()
        {
            if (currentAnimation != null)
            {
                StopCoroutine(currentAnimation);
                currentAnimation = null;
            }
        }

        private IEnumerator FadeCoroutine(float start, float end, float duration, System.Action onComplete = null)
        {
            if (canvasGroup == null)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.unscaledDeltaTime; // ポーズ中も動作するように unscaledDeltaTime を使用
                canvasGroup.alpha = Mathf.Lerp(start, end, elapsed / duration);
                yield return null;
            }

            canvasGroup.alpha = end;
            onComplete?.Invoke();
            currentAnimation = null;
        }
    }
}

