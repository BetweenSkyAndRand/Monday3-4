using Game.Drop;
using Game.GroundItem;
using UnityEngine;

namespace Game.Enemy
{
    /// <summary>
    /// 敵の死亡時にドロップテーブルを抽選し、敵の位置へ地面アイテムを生成する。
    /// EnemyVirusSpawner がスポーン時に自動で追加するため、プレハブへの設定は不要。
    /// </summary>
    public sealed class EnemyDropComponent : MonoBehaviour
    {
        private readonly DropTableRollUseCase dropTableRollUseCase = new DropTableRollUseCase();
        private readonly IDropRandom dropRandom = new UnityDropRandom();

        private EnemyVirusHealthComponent healthComponent;
        private EnemyDropTableParameter dropTable;
        private float dropRateMultiplier = 1f;
        private bool isSuppressed;

        public void Initialize(EnemyVirusHealthComponent targetHealthComponent, EnemyDropTableParameter targetDropTable, float targetDropRateMultiplier)
        {
            Unsubscribe();
            healthComponent = targetHealthComponent;
            dropTable = targetDropTable;
            dropRateMultiplier = targetDropRateMultiplier;
            isSuppressed = false;

            if (healthComponent != null)
            {
                healthComponent.OnDeath += HandleDeath;
            }
        }

        /// <summary>ダンジョン崩壊など、倒された扱いでもドロップさせたくないときに呼ぶ。</summary>
        public void SuppressDrop()
        {
            isSuppressed = true;
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Unsubscribe()
        {
            if (healthComponent != null)
            {
                healthComponent.OnDeath -= HandleDeath;
            }
        }

        private void HandleDeath()
        {
            if (isSuppressed || dropTable == null)
            {
                return;
            }

            if (!GroundItemRuntime.TryGetController(out var spawnController))
            {
                UnityEngine.Debug.LogWarning("[EnemyDropComponent] GroundItemSpawnController がシーンにないためドロップできません。", this);
                return;
            }

            if (!dropTableRollUseCase.TryRoll(dropTable, dropRateMultiplier, dropRandom, out var droppedItemStack))
            {
                return;
            }

            spawnController.TrySpawn(droppedItemStack, transform.position, out _);
        }
    }
}