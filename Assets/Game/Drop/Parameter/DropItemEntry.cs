using System;
using Game.Item.Parameter;
using UnityEngine;

namespace Game.Drop
{
    /// <summary>ドロップテーブルの1アイテム分。同じレア度プールの中で weight に比例して選ばれる。</summary>
    [Serializable]
    public class DropItemEntry
    {
        [SerializeField] private ItemParameter item;
        [Min(0f)]
        [SerializeField] private float weight = 1f;
        [Min(1)]
        [SerializeField] private int minQuantity = 1;
        [Min(1)]
        [SerializeField] private int maxQuantity = 1;

        public ItemParameter Item => item;
        public float Weight => weight;
        public int MinQuantity => Mathf.Max(1, minQuantity);
        public int MaxQuantity => Mathf.Max(MinQuantity, maxQuantity);
    }
}