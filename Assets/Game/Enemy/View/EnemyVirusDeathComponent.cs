using System.Collections;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Game.Enemy
{
    /// <summary>
    /// 敵の死亡演出（VFX・SE・コライダー無効化・Destroy）を担当するコンポーネント。
    /// EnemyVirusHealthComponent の OnDeath イベントを受け取り処理する。
    /// ファイルを分離することで Unity の AddComponent から名前で検索できるようにしている。
    /// </summary>
    public sealed class EnemyVirusDeathComponent : MonoBehaviour
    {
        [SerializeField] private MonoBehaviour[] additionalAiComponentsToDisable;
        [SerializeField] private float fallbackDestroyDelaySeconds = 1.0f;
        [SerializeField] private GameObject deadVfxPrefab;
        [Tooltip("[非推奨] deadSeId を優先してください。AudioClip フォールバック専用。")]
        [SerializeField] private AudioClip deadSe;
        [Tooltip("GamePresentationSettings.sePresentations に登録された SE ID。設定されていれば deadSe より優先される。")]
        [SerializeField] private string deadSeId = "";

        private EnemyVirusHealthComponent enemyVirusHealthComponent;
        private EnemyAnimationHandler animationHandler;
        private Collider[] cachedColliders;
        private bool isDead;

        private void Awake()
        {
            cachedColliders = GetComponentsInChildren<Collider>(true);
            animationHandler = GetComponentInChildren<EnemyAnimationHandler>();

            if (!TryGetComponent(out enemyVirusHealthComponent))
            {
                return;
            }

            enemyVirusHealthComponent.OnDeath += HandleDeath;
        }

        private void OnDestroy()
        {
            if (enemyVirusHealthComponent == null)
            {
                return;
            }

            enemyVirusHealthComponent.OnDeath -= HandleDeath;
        }

        /// <summary>
        /// スポーン時に EnemyVirusSpawner から呼び出す初期化処理。
        /// EnemyVirusViewData から死亡 VFX・SE を上書き設定する。
        /// 未設定（null）の場合は Inspector の値をそのまま使う。
        /// </summary>
        public void Initialize(EnemyVirusViewData enemyViewData)
        {
            if (enemyViewData == null)
            {
                return;
            }

            if (enemyViewData.DeadVfxPrefab != null)
            {
                deadVfxPrefab = enemyViewData.DeadVfxPrefab;
            }

            if (enemyViewData.DeadSe != null)
            {
                deadSe = enemyViewData.DeadSe;
            }

            // ID が設定されていれば優先採用する（Inspector で個別に上書きしていない限り）。
            if (!string.IsNullOrEmpty(enemyViewData.DeadSeId))
            {
                deadSeId = enemyViewData.DeadSeId;
            }
        }

        private void HandleDeath()
        {
            if (isDead)
            {
                return;
            }

            isDead = true;

            DisableObstacleComponents();
            PlayDeathEffects();

            // 死亡アニメーションを再生
            if (animationHandler != null)
            {
                animationHandler.PlayDeathAnimation();
            }

            StartCoroutine(DestroyAfterDeathPresentation());
        }

        private void DisableObstacleComponents()
        {
            if (cachedColliders != null)
            {
                for (var i = 0; i < cachedColliders.Length; i++)
                {
                    var targetCollider = cachedColliders[i];
                    if (targetCollider == null)
                    {
                        continue;
                    }

                    targetCollider.enabled = false;
                }
            }

            if (TryGetComponent<EnemyVirusActionComponent>(out var actionComponent))
            {
                actionComponent.enabled = false;
            }

            if (additionalAiComponentsToDisable == null)
            {
                return;
            }

            for (var i = 0; i < additionalAiComponentsToDisable.Length; i++)
            {
                var targetComponent = additionalAiComponentsToDisable[i];
                if (targetComponent == null)
                {
                    continue;
                }

                targetComponent.enabled = false;
            }
        }

        private void PlayDeathEffects()
        {
            // エフェクトを VFXManager 経由で再生
            if (deadVfxPrefab != null)
            {
                var vfxManager = Effect.VFXManager.Instance;
                if (vfxManager != null)
                {
                    vfxManager.PlayEffect(deadVfxPrefab, transform.position, Quaternion.identity);
                }
                else
                {
                    // フォールバック: VFXManager がない場合は直接実行
                    Object.Instantiate(deadVfxPrefab, transform.position, Quaternion.identity);
                }
            }

            // SE を AudioManager 経由で再生
            if (!string.IsNullOrEmpty(deadSeId) || deadSe != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    // ID 優先 → AudioClip フォールバック（音量ルール統一のため AudioManager 経由）。
                    if (!string.IsNullOrEmpty(deadSeId))
                    {
                        audioManager.PlayConfiguredSe3D(deadSeId, transform.position);
                    }
                    else
                    {
                        audioManager.PlaySe3D(deadSe, transform.position);
                    }
                }
                else if (deadSe != null)
                {
                    // フォールバック: AudioManager がない場合は直接実行（ID 単独は再生不可）
                    AudioSource.PlayClipAtPoint(deadSe, transform.position);
                }
            }
        }

        private IEnumerator DestroyAfterDeathPresentation()
        {
            var delaySeconds = Mathf.Max(0f, fallbackDestroyDelaySeconds);

            // アニメーションがある場合は最長クリップ時間だけ待ってから Destroy する
            if (TryGetComponent<Animator>(out var animator)
                && animator.runtimeAnimatorController != null)
            {
                var clips = animator.runtimeAnimatorController.animationClips;
                for (var i = 0; i < clips.Length; i++)
                {
                    var clip = clips[i];
                    if (clip == null)
                    {
                        continue;
                    }

                    if (clip.length > delaySeconds)
                    {
                        delaySeconds = clip.length;
                    }
                }
            }

            yield return new WaitForSeconds(delaySeconds);
            Destroy(gameObject);
        }
    }
}

