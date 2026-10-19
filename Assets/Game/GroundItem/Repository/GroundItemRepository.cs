using System.Collections.Generic;

namespace Game.GroundItem
{
    /// <summary>地面アイテムの保存・取得・削除のみを行う。</summary>
    public class GroundItemRepository
    {
        private readonly Dictionary<string, GroundItemEntity> entities = new Dictionary<string, GroundItemEntity>();

        public int Count => entities.Count;

        public void Save(GroundItemEntity entity)
        {
            entities[entity.InstanceId] = entity;
        }

        public bool TryGet(string instanceId, out GroundItemEntity entity)
        {
            return entities.TryGetValue(instanceId, out entity);
        }

        public bool Remove(string instanceId)
        {
            return entities.Remove(instanceId);
        }

        public IReadOnlyCollection<GroundItemEntity> GetAll()
        {
            return entities.Values;
        }
    }
}