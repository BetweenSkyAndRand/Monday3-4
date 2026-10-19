using System.Collections;
using UnityEngine;

namespace Game.Effect
{
    /// <summary>
    /// カメラの画面揺れ（シェイク）演出を一元管理するシングルトンマネージャー。
    /// 責務: 指定カメラの Transform を一時的に揺らすことのみ。
    /// VFXManager（パーティクル）・ScreenEffectManager（フェード/フラッシュ）と責務を分離する。
    /// </summary>
    public class ScreenShakeManager : MonoBehaviour
    {
        private static ScreenShakeManager instance;
        private static bool isDestroying;

        public static ScreenShakeManager Instance
        {
            get
            {
                if (isDestroying || !Application.isPlaying)
                {
                    return null;
                }

                if (instance == null)
                {
                    var go = new GameObject("ScreenShakeManager");
                    instance = go.AddComponent<ScreenShakeManager>();
                    DontDestroyOnLoad(go);
                }

                return instance;
            }
        }

        private Coroutine shakeCoroutine;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// 指定カメラ（未指定時は Camera.main）を duration 秒間揺らす。
        /// 揺れは時間経過とともに減衰する。
        /// </summary>
        public void Shake(float duration, float magnitude, Camera targetCamera = null)
        {
            var cameraToShake = targetCamera != null ? targetCamera : Camera.main;
            if (cameraToShake == null)
            {
                UnityEngine.Debug.LogWarning("[ScreenShakeManager] 対象カメラが見つかりません。Shake をスキップします。");
                return;
            }

            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
            }

            shakeCoroutine = StartCoroutine(ShakeCoroutine(cameraToShake, duration, magnitude));
        }

        private IEnumerator ShakeCoroutine(Camera targetCamera, float duration, float magnitude)
        {
            var originalLocalPosition = targetCamera.transform.localPosition;
            var elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;

                // 経過時間に応じて揺れを減衰させ、崩壊の瞬間ほど揺れが収まっていくようにする
                var damper = 1f - Mathf.Clamp01(elapsed / duration);
                var offset = Random.insideUnitSphere * (magnitude * damper);
                offset.z = 0f;

                targetCamera.transform.localPosition = originalLocalPosition + offset;
                yield return null;
            }

            targetCamera.transform.localPosition = originalLocalPosition;
            shakeCoroutine = null;
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


