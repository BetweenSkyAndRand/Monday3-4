using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Effect
{
    /// <summary>
    /// エフェクトを一元管理するシングルトンマネージャー（後方互換ブリッジ）。
    /// 【非推奨】新規実装では VFXManager / ScreenEffectManager を直接使用してください。
    ///
    /// 責務分離：
    ///   - VFXManager: パーティクル（ワールド空間）のInstantiate/削除
    ///   - ScreenEffectManager: 画面フェード/フラッシュ（UGUI Canvas）
    /// </summary>
    [System.Obsolete("Use VFXManager for particle effects or ScreenEffectManager for screen effects.", false)]
    public class EffectManager : MonoBehaviour
    {
        private static EffectManager instance;
        private static bool isDestroying;

        public static EffectManager Instance
        {
            get
            {
                if (isDestroying || !Application.isPlaying)
                {
                    return null;
                }

                if (instance == null)
                {
                    var go = new GameObject("EffectManager");
                    instance = go.AddComponent<EffectManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("設定")]
        [SerializeField] private Core.GameSettings settings;

        // ─── 以下、VFXManager / ScreenEffectManager へのブリッジメソッド ───

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

            LoadSettings();
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

            // VFXManager と ScreenEffectManager へ設定を伝播
            var vfxManager = VFXManager.Instance;
            if (vfxManager != null)
            {
                vfxManager.ApplySettings(newSettings);
            }

            var screenManager = ScreenEffectManager.Instance;
            if (screenManager != null)
            {
                screenManager.ApplySettings(newSettings);
            }
        }

        // ─── パーティクルエフェクト再生（VFXManager へ委譲） ───

        /// <summary>指定位置にエフェクトを再生する（設定でOFFの場合はスキップ）。</summary>
        public void PlayEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)
        {
            var vfxManager = VFXManager.Instance;
            if (vfxManager != null)
            {
                vfxManager.PlayEffect(effectPrefab, position, rotation);
            }
        }

        /// <summary>指定位置にエフェクトを再生する（Transform親子関係あり）。</summary>
        public void PlayEffectAttached(GameObject effectPrefab, Transform parent, Vector3 localPosition)
        {
            var vfxManager = VFXManager.Instance;
            if (vfxManager != null)
            {
                vfxManager.PlayEffectAttached(effectPrefab, parent, localPosition);
            }
        }

        // ─── 画面エフェクト（ScreenEffectManager へ委譲） ───

        /// <summary>画面を徐々に暗転させる（フェードアウト）。</summary>
        public void FadeOut(float duration, System.Action onComplete = null)
        {
            var screenManager = ScreenEffectManager.Instance;
            if (screenManager != null)
            {
                screenManager.FadeOut(duration, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        /// <summary>画面を徐々に明るくする（フェードイン）。</summary>
        public void FadeIn(float duration, System.Action onComplete = null)
        {
            var screenManager = ScreenEffectManager.Instance;
            if (screenManager != null)
            {
                screenManager.FadeIn(duration, onComplete);
            }
            else
            {
                onComplete?.Invoke();
            }
        }

        /// <summary>画面を一瞬光らせる（被弾演出など）。</summary>
        public void Flash(Color color, float duration = 0.2f)
        {
            var screenManager = ScreenEffectManager.Instance;
            if (screenManager != null)
            {
                screenManager.Flash(color, duration);
            }
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

