using System.Collections.Generic;
using UnityEngine;

namespace Game.Dungeon.Spawn
{
    /// <summary>
    /// 部屋ごとに、スポーンした宝箱・雑貨を記録し、部屋の崩壊時にまとめて破棄する。
    /// 敵は EnemyVirusController 側で管理されるため、ここでは扱わない。
    /// </summary>
    public class RoomSpawnedObjectRegistry
    {
        private readonly Dictionary<Vector2Int, List<GameObject>> spawnedObjectsByRoom =
            new Dictionary<Vector2Int, List<GameObject>>();

        public void Register(Vector2Int roomGridPosition, GameObject spawnedObject)
        {
            if (spawnedObject == null)
            {
                return;
            }

            if (!spawnedObjectsByRoom.TryGetValue(roomGridPosition, out var spawnedObjects))
            {
                spawnedObjects = new List<GameObject>();
                spawnedObjectsByRoom[roomGridPosition] = spawnedObjects;
            }

            spawnedObjects.Add(spawnedObject);
        }

        /// <summary>指定した部屋で記録した生成物をすべて破棄して、記録を消す。</summary>
        public void DestroyRoomObjects(Vector2Int roomGridPosition)
        {
            if (!spawnedObjectsByRoom.TryGetValue(roomGridPosition, out var spawnedObjects))
            {
                return;
            }

            foreach (var spawnedObject in spawnedObjects)
            {
                // 拾われるなどして既に破棄された要素は無視する
                if (spawnedObject != null)
                {
                    Object.Destroy(spawnedObject);
                }
            }

            spawnedObjectsByRoom.Remove(roomGridPosition);
        }
    }
}
