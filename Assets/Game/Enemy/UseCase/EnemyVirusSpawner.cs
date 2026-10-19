using System;
using UnityEngine;
using Game.Enemy.View;
using Object = UnityEngine.Object;

namespace Game.Enemy.UseCase
{
    public sealed class EnemyVirusSpawner : IEnemyVirusSpawner
    {
        private readonly IEnemyVirusResolver enemyVirusResolver;
        private readonly Transform enemiesParent;

        public EnemyVirusSpawner(IEnemyVirusResolver enemyVirusResolver, Transform enemiesParent = null)
        {
            this.enemyVirusResolver = enemyVirusResolver;
            this.enemiesParent = enemiesParent;
        }

        public bool TrySpawn(
            EnemyVirusSpawnCommand spawnCommand,
            out GameObject spawnedEnemyObject,
            out EnemyVirusSpawnErrorType spawnErrorType,
            out EnemyVirusResolveErrorType resolveErrorType)
        {
            if (!enemyVirusResolver.TryResolveSpawnContext(
                    spawnCommand.Uid,
                    out var spawnContext,
                    out resolveErrorType))
            {
                spawnedEnemyObject = null;
                spawnErrorType = EnemyVirusSpawnErrorType.ResolveFailed;
                return false;
            }

            return TryCreateEnemyObject(
                spawnContext,
                spawnCommand,
                out spawnedEnemyObject,
                out spawnErrorType);
        }

        private bool TryCreateEnemyObject(
            EnemyVirusSpawnContext spawnContext,
            EnemyVirusSpawnCommand spawnCommand,
            out GameObject spawnedEnemyObject,
            out EnemyVirusSpawnErrorType spawnErrorType)
        {
            var prefab = ResolveSpawnPrefab(spawnContext);
            if (prefab == null)
            {
                spawnedEnemyObject = null;
                spawnErrorType = EnemyVirusSpawnErrorType.PrefabMissing;
                return false;
            }

            try
            {
                spawnedEnemyObject = Object.Instantiate(prefab, spawnCommand.SpawnPosition, Quaternion.identity, enemiesParent);
            }
            catch (Exception exception)
            {
                UnityEngine.Debug.LogError(exception);
                spawnedEnemyObject = null;
                spawnErrorType = EnemyVirusSpawnErrorType.InstantiateFailed;
                return false;
            }

            InitializeView(spawnedEnemyObject, spawnContext);
            InitializeLogic(spawnedEnemyObject, spawnContext, spawnCommand.HpBonus, spawnCommand.AttackPowerBonus, spawnCommand.DropRateMultiplier);

            spawnErrorType = EnemyVirusSpawnErrorType.None;
            return true;
        }

        private static GameObject ResolveSpawnPrefab(EnemyVirusSpawnContext spawnContext)
        {
            return spawnContext.EnemyViewData.EnemyVirusPrefab;
        }

        private static void InitializeView(GameObject spawnedEnemyObject, EnemyVirusSpawnContext spawnContext)
        {
            var enemyViewData = spawnContext.EnemyViewData;
            var targetTransform = spawnedEnemyObject.transform;

            // スケールを先に適用してからバウンズを使って地面に揃える
            targetTransform.localScale = enemyViewData.ModelLocalScale;
            AlignBottomToGround(spawnedEnemyObject);

            // スケール・地面揃えの後に追加オフセットを適用する
            targetTransform.localPosition += enemyViewData.ModelLocalOffset;

            if (spawnedEnemyObject.TryGetComponent<Animator>(out var animator)
                && enemyViewData.AnimatorController != null)
            {
                animator.runtimeAnimatorController = enemyViewData.AnimatorController;
            }

            // エフェクトを VFXManager 経由で再生
            if (enemyViewData.SpawnVfxPrefab != null)
            {
                var vfxManager = Effect.VFXManager.Instance;
                if (vfxManager != null)
                {
                    vfxManager.PlayEffect(enemyViewData.SpawnVfxPrefab, targetTransform.position, Quaternion.identity);
                }
                else
                {
                    // フォールバック: VFXManager がない場合は直接実行
                    Object.Instantiate(enemyViewData.SpawnVfxPrefab, targetTransform.position, Quaternion.identity);
                }
            }

            // SE を AudioManager 経由で再生
            // ID 優先（PlayConfiguredSe3D）。未設定なら AudioClip 直渡しにフォールバックする。
            var spawnSeId = enemyViewData.SpawnSeId;
            var spawnSeClip = enemyViewData.SpawnSe;
            if (!string.IsNullOrEmpty(spawnSeId) || spawnSeClip != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    if (!string.IsNullOrEmpty(spawnSeId))
                    {
                        audioManager.PlayConfiguredSe3D(spawnSeId, targetTransform.position);
                    }
                    else
                    {
                        audioManager.PlaySe3D(spawnSeClip, targetTransform.position);
                    }
                }
                else if (spawnSeClip != null)
                {
                    // フォールバック: AudioManager がない場合は直接実行（ID 単独は不可）
                    AudioSource.PlayClipAtPoint(spawnSeClip, targetTransform.position);
                }
            }
        }

        /// <summary>
        /// モデルのバウンズ最小 Y をスポーン地面高度（= 現在の transform.position.y）に揃える。
        /// SkinnedMeshRenderer などピボット位置が中心のモデルが地面に埋まることを防ぐ。
        /// 床面 Y が 0 以外（例: ダンジョンタイル上面 Y=0.5）でも正しく動作する。
        /// Renderer が存在しない場合は何もしない。
        /// </summary>
        private static void AlignBottomToGround(GameObject target)
        {
            var renderers = target.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0)
            {
                return;
            }

            var minY = float.MaxValue;
            foreach (var targetRenderer in renderers)
            {
                if (targetRenderer.bounds.min.y < minY)
                {
                    minY = targetRenderer.bounds.min.y;
                }
            }

            // バウンズ底辺をスポーン地面高度（transform.position.y = GetWorldPosition で確定した床面 Y）に合わせる
            var groundY = target.transform.position.y;
            var yAdjustment = groundY - minY;

            if (Mathf.Abs(yAdjustment) < 0.001f)
            {
                return;
            }

            var pos = target.transform.position;
            pos.y += yAdjustment;
            target.transform.position = pos;
        }

        private static void InitializeLogic(GameObject spawnedEnemyObject, EnemyVirusSpawnContext spawnContext, uint hpBonus, uint attackPowerBonus, float dropRateMultiplier)
        {
            var enemyEntity = spawnContext.EnemyEntity;

            // プレハブにコンポーネントがなければ自動追加する。
            // モデルプレハブだけ用意すれば動作するようにするための設計。
            var healthComponent = GetOrAdd<EnemyVirusHealthComponent>(spawnedEnemyObject);

            // ダンジョン生成回数に応じたHP強化を適用
            var scaledHp = enemyEntity.Hp + hpBonus;
            healthComponent.Initialize(scaledHp, spawnContext.EnemyViewData);

            // プレイヤー攻撃（IDamageable）を受け取る窓口を保証する
            GetOrAdd<EnemyVirusDamageHandler>(spawnedEnemyObject);

            var actionComponent = GetOrAdd<EnemyVirusActionComponent>(spawnedEnemyObject);
            actionComponent.Initialize(enemyEntity.ActionPatternEntity);
            actionComponent.SetAttackCoolTime(enemyEntity.WeaponEntity.AttackCoolTime);

            var weaponComponent = GetOrAdd<EnemyVirusWeaponComponent>(spawnedEnemyObject);
            weaponComponent.Initialize(enemyEntity.WeaponEntity, spawnContext.EnemyViewData);

            var deathComponent = GetOrAdd<EnemyVirusDeathComponent>(spawnedEnemyObject);
            deathComponent.Initialize(spawnContext.EnemyViewData);

            var dropComponent = GetOrAdd<EnemyDropComponent>(spawnedEnemyObject);
            dropComponent.Initialize(healthComponent, spawnContext.EnemyViewData.DropTable, dropRateMultiplier);

            var aiComponent = GetOrAdd<EnemyVirusAIComponent>(spawnedEnemyObject);
            aiComponent.Initialize(enemyEntity, attackPowerBonus);
        }

        /// <summary>
        /// コンポーネントが存在すればそれを返し、なければ AddComponent して返す。
        /// プレハブ側にコンポーネントが未アタッチでも動作させるための補助メソッド。
        /// </summary>
        private static T GetOrAdd<T>(GameObject target) where T : Component
        {
            if (target.TryGetComponent<T>(out var component))
            {
                return component;
            }

            return target.AddComponent<T>();
        }
    }
}
