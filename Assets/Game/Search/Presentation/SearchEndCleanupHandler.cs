using Game.Enemy;
using Game.Treasure;
using UnityEngine;

// ReSharper disable once CheckNamespace
// プロジェクト規約でフォルダ階層より短い名前空間を使用している (例: Game.Search)
namespace Game.Search
{
    /// <summary>
    /// サーチエンド時の残オブジェクトを一括クリーンアップする。
    /// IEnemyCleanupHandler を実装し、SearchController の enemyCleanupHandlerObject にアサインして使用する。
    ///
    /// 責務：
    ///   - アクティブな全敵（EnemyVirusHealthComponent を持つオブジェクト）のカウントと消去
    ///   - アクティブな全宝箱（TreasureBoxController を持つオブジェクト）の消去
    ///   - このクラスは残敵のカウント・消去のみ担当
    /// </summary>
    public class SearchEndCleanupHandler : MonoBehaviour, IEnemyCleanupHandler
    {
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // IEnemyCleanupHandler 実装
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// シーン上に残っている生存敵（IsDead = false）の数を返す。
        /// 探索終了時の残敵数として使える（現在は加算に使っていない）。
        /// </summary>
        public int GetRemainingEnemyCount()
        {
            var allHealthComponents = FindObjectsByType<EnemyVirusHealthComponent>(FindObjectsSortMode.None);
            var count = 0;

            foreach (var healthComponent in allHealthComponents)
            {
                if (!healthComponent.IsDead)
                {
                    count++;
                }
            }

            return count;
        }

        /// <summary>
        /// シーン上の全残敵と全宝箱を消去する。
        /// 死亡演出中（IsDead = true）の敵も含めてすべて即時消去する。
        /// </summary>
        public void DestroyAllRemainingEnemies()
        {
            DestroyAllEnemies();
            DestroyAllTreasureBoxes();
        }

        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━
        // private
        // ━━━━━━━━━━━━━━━━━━━━━━━━━━━━

        /// <summary>
        /// EnemyVirusHealthComponent を持つ全オブジェクトを即時消去する。
        /// </summary>
        private void DestroyAllEnemies()
        {
            var allHealthComponents = FindObjectsByType<EnemyVirusHealthComponent>(FindObjectsSortMode.None);

            foreach (var healthComponent in allHealthComponents)
            {
                if (healthComponent == null)
                {
                    continue;
                }

                UnityEngine.Debug.Log($"[SearchEndCleanupHandler] 敵を消去: {healthComponent.gameObject.name}");
                Destroy(healthComponent.gameObject);
            }

            UnityEngine.Debug.Log($"[SearchEndCleanupHandler] 敵を {allHealthComponents.Length} 体消去しました。");
        }

        /// <summary>
        /// TreasureBoxController を持つ全宝箱を即時消去する。
        /// </summary>
        private void DestroyAllTreasureBoxes()
        {
            var allTreasureBoxes = FindObjectsByType<TreasureBoxController>(FindObjectsSortMode.None);

            foreach (var treasureBox in allTreasureBoxes)
            {
                if (treasureBox == null)
                {
                    continue;
                }

                UnityEngine.Debug.Log($"[SearchEndCleanupHandler] 宝箱を消去: {treasureBox.gameObject.name}");
                Destroy(treasureBox.gameObject);
            }

            UnityEngine.Debug.Log($"[SearchEndCleanupHandler] 宝箱を {allTreasureBoxes.Length} 個消去しました。");
        }
    }
}

