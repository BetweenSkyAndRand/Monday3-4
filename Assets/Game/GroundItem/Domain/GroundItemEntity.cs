using Game.Item;
using UnityEngine;

namespace Game.GroundItem
{
    public readonly struct GroundItemEntity
    {
        public string InstanceId { get; }
        public ItemStack ItemStack { get; }
        public Vector3 WorldPosition { get; }

        /// <summary>落ちた時刻（Time.time）。消滅判定の基準。</summary>
        public float DroppedTime { get; }

        public GroundItemEntity(string instanceId, ItemStack itemStack, Vector3 worldPosition, float droppedTime)
        {
            InstanceId = instanceId;
            ItemStack = itemStack;
            WorldPosition = worldPosition;
            DroppedTime = droppedTime;
        }
    }
}