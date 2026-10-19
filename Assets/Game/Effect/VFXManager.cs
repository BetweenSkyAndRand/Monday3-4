using UnityEngine;

namespace Game.Effect
{
    /// <summary>
    /// パーティクルエフェクト（VFX）の再生を一元管理するシングルトンマネージャー。
    /// 責務: ワールド空間でのエフェクト Instantiate と自動削除のみ。
    /// 画面エフェクト（フェード・フラッシュ）は ScreenEffectManager が担当。
    /// </summary>
    public class VFXManager : MonoBehaviour
    {
        private static VFXManager instance;
        private static bool isDestroying;

        public static VFXManager Instance
        {
            get
            {
                if (isDestroying || !Application.isPlaying)
                {
                    return null;
                }

                if (instance == null)
                {
                    var go = new GameObject("VFXManager");
                    instance = go.AddComponent<VFXManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        [Header("設定")]
        [SerializeField] private Core.GameSettings settings;

        private bool enableVisualEffects = true;

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

            enableVisualEffects = newSettings.enableVisualEffects;
        }

        /// <summary>指定位置にエフェクトを再生する（設定でOFFの場合はスキップ）。</summary>
        public void PlayEffect(GameObject effectPrefab, Vector3 position, Quaternion rotation)
        {
            if (!enableVisualEffects || effectPrefab == null)
            {
                return;
            }

            var effect = Instantiate(effectPrefab, position, rotation);

            // 自動削除（ParticleSystemがあれば再生終了後に削除）
            var ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 3f); // デフォルト3秒後に削除
            }
        }

        /// <summary>指定位置にエフェクトを再生する（Transform親子関係あり）。</summary>
        public void PlayEffectAttached(GameObject effectPrefab, Transform parent, Vector3 localPosition)
        {
            if (!enableVisualEffects || effectPrefab == null || parent == null)
            {
                return;
            }

            var effect = Instantiate(effectPrefab, parent);
            effect.transform.localPosition = localPosition;

            var ps = effect.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                Destroy(effect, ps.main.duration + ps.main.startLifetime.constantMax);
            }
            else
            {
                Destroy(effect, 3f);
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

