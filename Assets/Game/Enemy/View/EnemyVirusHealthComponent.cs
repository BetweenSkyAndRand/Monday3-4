using Game.Debug;
using System;
using UnityEngine;

namespace Game.Enemy
{
    public sealed class EnemyVirusHealthComponent : MonoBehaviour
    {
        [Header("デバッグ")]
        [SerializeField] private bool enableDamageDebugLog = true;

        [Header("Damage Feedback")]
        [SerializeField] private float blinkDurationSeconds = 0.5f;
        [SerializeField] private float invincibilityDurationSeconds = 0.5f;
        [SerializeField] private float blinkIntervalSeconds = 0.1f;
        [SerializeField] private GameObject hitVfxPrefab;
        [Tooltip("[非推奨] damageSeId を優先してください。AudioClip フォールバック専用。")]
        [SerializeField] private AudioClip damageSe;
        [Tooltip("GamePresentationSettings.sePresentations に登録された SE ID。設定されていれば damageSe より優先される。")]
        [SerializeField] private string damageSeId = "";

        private Renderer[] cachedRenderers;
        private float blinkTimer;
        private float invincibilityTimer;
        private float blinkIntervalTimer;
        private bool isBlinkVisible = true;

        public uint CurrentHp { get; private set; }
        public uint MaxHp { get; private set; }
        public bool IsDead => CurrentHp == 0;
        public bool IsInvincible => invincibilityTimer > 0f;

        public event Action<uint> OnDamaged;
        public event Action<float, float> OnHpChanged;
        public event Action OnDeath;

        private void Awake()
        {
            cachedRenderers = GetComponentsInChildren<Renderer>(true);
        }

        private void Update()
        {
            UpdateInvincibilityTimer();
            UpdateBlinkTimer();
        }

        public void Initialize(uint hp, EnemyVirusViewData enemyViewData = null)
        {
            MaxHp = hp;
            CurrentHp = hp;
            blinkTimer = 0f;
            invincibilityTimer = 0f;
            blinkIntervalTimer = 0f;
            isBlinkVisible = true;
            SetRenderersVisible(true);

            if (enemyViewData != null)
            {
                if (enemyViewData.HitVfxPrefab != null)
                {
                    hitVfxPrefab = enemyViewData.HitVfxPrefab;
                }

                if (enemyViewData.DamageSe != null)
                {
                    damageSe = enemyViewData.DamageSe;
                }

                // ID 優先採用（Inspector で個別に上書きしていない限り）
                if (!string.IsNullOrEmpty(enemyViewData.DamageSeId))
                {
                    damageSeId = enemyViewData.DamageSeId;
                }
            }
        }

        public void TakeDamage(uint damageAmount)
        {
            if (damageAmount == 0)
            {
                return;
            }

            if (IsDead || IsInvincible)
            {
                return;
            }

            var nextHp = damageAmount >= CurrentHp ? 0u : CurrentHp - damageAmount;
            CurrentHp = nextHp;

            if (enableDamageDebugLog)
            {
                UnityEngine.Debug.Log($"[EnemyVirusHealthComponent] 被ダメ: enemy={gameObject.name}, damage={damageAmount}, hp={CurrentHp}", this);
            }

            if (hitVfxPrefab != null)
            {
                var vfxManager = Effect.VFXManager.Instance;
                if (vfxManager != null)
                {
                    vfxManager.PlayEffect(hitVfxPrefab, transform.position, Quaternion.identity);
                }
            }

            GameLogger.Log(LogCategory.Enemy, $"{gameObject.name} が {damageAmount} ダメージを受けました（残りHP: {CurrentHp}）");

            if (!string.IsNullOrEmpty(damageSeId) || damageSe != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    // ID 優先 → AudioClip フォールバック（AudioManager 経由で音量ルール統一）。
                    if (!string.IsNullOrEmpty(damageSeId))
                    {
                        audioManager.PlayConfiguredSe3D(damageSeId, transform.position);
                    }
                    else
                    {
                        audioManager.PlaySe3D(damageSe, transform.position);
                    }
                }
                else if (damageSe != null)
                {
                    AudioSource.PlayClipAtPoint(damageSe, transform.position);
                }
            }

            OnDamaged?.Invoke(damageAmount);
            OnHpChanged?.Invoke(CurrentHp, MaxHp);

            if (CurrentHp == 0)
            {
                if (enableDamageDebugLog)
                {
                    UnityEngine.Debug.Log($"[EnemyVirusHealthComponent] 撃破: enemy={gameObject.name}", this);
                }

                GameLogger.Log(LogCategory.Enemy, $"{gameObject.name} が倒されました！");
                // 死亡時は点滅を開始せず、死亡遷移を優先する。
                SetRenderersVisible(true);
                OnDeath?.Invoke();
                return;
            }

            invincibilityTimer = Mathf.Max(0f, invincibilityDurationSeconds);
            blinkTimer = Mathf.Max(0f, blinkDurationSeconds);
            blinkIntervalTimer = 0f;
            isBlinkVisible = false;
            SetRenderersVisible(isBlinkVisible);
        }

        private void UpdateInvincibilityTimer()
        {
            if (invincibilityTimer <= 0f)
            {
                return;
            }

            invincibilityTimer -= Time.deltaTime;
            if (invincibilityTimer < 0f)
            {
                invincibilityTimer = 0f;
            }
        }

        private void UpdateBlinkTimer()
        {
            if (blinkTimer <= 0f)
            {
                return;
            }

            blinkTimer -= Time.deltaTime;
            blinkIntervalTimer += Time.deltaTime;

            if (blinkIntervalTimer >= Mathf.Max(0.01f, blinkIntervalSeconds))
            {
                blinkIntervalTimer = 0f;
                isBlinkVisible = !isBlinkVisible;
                SetRenderersVisible(isBlinkVisible);
            }

            if (blinkTimer <= 0f)
            {
                blinkTimer = 0f;
                isBlinkVisible = true;
                SetRenderersVisible(true);
            }
        }

        private void SetRenderersVisible(bool isVisible)
        {
            if (cachedRenderers == null)
            {
                return;
            }

            for (var i = 0; i < cachedRenderers.Length; i++)
            {
                var targetRenderer = cachedRenderers[i];
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.enabled = isVisible;
            }
        }
    }
}

