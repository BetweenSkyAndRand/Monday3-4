using System;
using System.Collections.Generic;

namespace Game.GroundItem
{
    /// <summary>指定した範囲（崩壊する部屋など）の中にある地面アイテムの InstanceId を返す。範囲の判定は呼び出し側から渡す。</summary>
    public class GroundItemRemovalInBoundsUseCase
    {
        public List<string> Execute(IEnumerable<GroundItemEntity> entities, Func<UnityEngine.Vector3, bool> isInsideBounds)
        {
            var removalInstanceIds = new List<string>();
            foreach (var entity in entities)
            {
                if (isInsideBounds(entity.WorldPosition))
                {
                    removalInstanceIds.Add(entity.InstanceId);
                }
            }

            return removalInstanceIds;
        }
    }
}
