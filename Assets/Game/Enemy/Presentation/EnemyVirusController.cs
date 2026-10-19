using System;
using UnityEngine;
using Game.Enemy.UseCase;

namespace Game.Enemy.Presentation
{
    public sealed class EnemyVirusController : MonoBehaviour
    {
        [Header("Master Data")]
        [SerializeField] private EnemyVirusMasterDataList enemyVirusMasterDataList;
        [SerializeField] private EnemyVirusViewDataList enemyVirusViewDataList;

        [Header("スポーン設定")]
        [Tooltip("生成される敵の親Transform。未設定の場合は「LEVEL> Characters」を自動検索します。")]
        [SerializeField] private Transform enemiesParent;

        private EnemyVirusEntityRepository enemyVirusEntityRepository;
        private EnemyVirusViewRepository enemyVirusViewRepository;
        private IEnemyVirusResolver enemyVirusResolver;
        private IEnemyVirusSpawner enemyVirusSpawner;
        private bool isInitialized;

        private void Awake()
        {
            // enemiesParent が未設定の場合は自動検索する
            if (enemiesParent == null)
            {
                enemiesParent = FindEnemiesParent();
                if (enemiesParent == null)
                {
                    UnityEngine.Debug.LogWarning("[EnemyVirusController] enemiesParent が見つかりませんでした。自動的に作成します。");
                    enemiesParent = CreateEnemiesParent();
                }
                else
                {
                    UnityEngine.Debug.Log($"[EnemyVirusController] enemiesParent を自動検索しました: {GetGameObjectPath(enemiesParent)}", this);
                }
            }
            else
            {
                UnityEngine.Debug.Log($"[EnemyVirusController] enemiesParent が設定されています: {GetGameObjectPath(enemiesParent)}", this);
            }

            enemyVirusEntityRepository = new EnemyVirusEntityRepository();
            enemyVirusViewRepository = new EnemyVirusViewRepository();

            try
            {
                enemyVirusEntityRepository.Cache(enemyVirusMasterDataList);
                enemyVirusViewRepository.Cache(enemyVirusViewDataList);

                enemyVirusResolver = new EnemyVirusResolver(enemyVirusEntityRepository, enemyVirusViewRepository);
                enemyVirusSpawner = new EnemyVirusSpawner(enemyVirusResolver, enemiesParent);
                isInitialized = true;
                UnityEngine.Debug.Log("[EnemyVirusController] 初期化完了", this);
            }
            catch (Exception exception)
            {
                isInitialized = false;
                UnityEngine.Debug.LogError($"EnemyVirusController initialize failed: {exception}", this);
            }
        }

        public bool TryRequestSpawn(string targetUid, Vector3 spawnPosition, uint hpBonus = 0, uint attackPowerBonus = 0, float dropRateMultiplier = 1f)
        {
            if (!isInitialized)
            {
                UnityEngine.Debug.LogWarning("EnemyVirusController is not initialized.", this);
                return false;
            }

            var spawnCommand = new EnemyVirusSpawnCommand(targetUid, spawnPosition, hpBonus, attackPowerBonus, dropRateMultiplier);

            if (!enemyVirusSpawner.TrySpawn(
                    spawnCommand,
                    out _,
                    out var spawnErrorType,
                    out var resolveErrorType))
            {
                LogSpawnFailure(spawnCommand.Uid, spawnErrorType, resolveErrorType);
                return false;
            }

            return true;
        }

        private static void LogSpawnFailure(
            string uid,
            EnemyVirusSpawnErrorType spawnErrorType,
            EnemyVirusResolveErrorType resolveErrorType)
        {
            UnityEngine.Debug.LogWarning(
                $"Enemy spawn failed. uid={uid}, spawnError={spawnErrorType}, resolveError={resolveErrorType}");
        }

        /// <summary>
        /// 敵スポーン用の親 Transform を探す。
        /// 優先順位: "LEVEL" > "Characters" > null
        /// </summary>
        private Transform FindEnemiesParent()
        {
            // 1. "LEVEL" を探す
            var levelRoot = GameObject.Find("LEVEL");
            if (levelRoot != null)
            {
                var charactersGroup = levelRoot.transform.Find("Characters");
                if (charactersGroup != null)
                {
                    return charactersGroup;
                }
            }

            // 2. フォールバック: "Characters" を直接探す
            var charactersFallback = GameObject.Find("Characters");
            if (charactersFallback != null)
            {
                return charactersFallback.transform;
            }

            return null;
        }

        /// <summary>
        /// 敵スポーン用の親 Transform を自動作成する。
        /// "LEVEL" が存在すればその下に "Characters" を作成。
        /// 存在しなければシーンルートに "EnemiesParent" を作成。
        /// </summary>
        private Transform CreateEnemiesParent()
        {
            var levelRoot = GameObject.Find("LEVEL");
            if (levelRoot != null)
            {
                var charactersGroup = new GameObject("Characters");
                charactersGroup.transform.SetParent(levelRoot.transform);
                UnityEngine.Debug.Log($"[EnemyVirusController] 「LEVEL> Characters」を作成しました。", this);
                return charactersGroup.transform;
            }

            // フォールバック: シーンルートに作成
            var enemiesParentObject = new GameObject("EnemiesParent");
            UnityEngine.Debug.Log($"[EnemyVirusController] シーンルートに「EnemiesParent」を作成しました。", this);
            return enemiesParentObject.transform;
        }

        /// <summary>
        /// Transform の階層パスを取得する（デバッグ用）。
        /// </summary>
        private string GetGameObjectPath(Transform targetTransform)
        {
            if (targetTransform == null)
            {
                return "null";
            }

            var path = targetTransform.name;
            var parent = targetTransform.parent;
            while (parent != null)
            {
                path = parent.name + " > " + path;
                parent = parent.parent;
            }
            return path;
        }
    }
}
