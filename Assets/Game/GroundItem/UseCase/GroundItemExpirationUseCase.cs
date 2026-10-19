using System.Collections.Generic;

namespace Game.GroundItem
{
    public class GroundItemExpirationUseCase
    {
        /// <summary>消滅時間を過ぎた地面アイテムの InstanceId を返す。despawnSeconds が 0 以下なら何も返さない。</summary>
        public List<string> Execute(IEnumerable<GroundItemEntity> entities, float currentTime, float despawnSeconds)
        {
            var expiredInstanceIds = new List<string>();
            if (despawnSeconds <= 0f)
            {
                return expiredInstanceIds;
            }

            foreach (var entity in entities)
            {
                if (currentTime - entity.DroppedTime >= despawnSeconds)
                {
                    expiredInstanceIds.Add(entity.InstanceId);
                }
            }

            return expiredInstanceIds;
        }
    }
}