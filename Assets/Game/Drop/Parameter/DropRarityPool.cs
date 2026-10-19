using System;
using System.Collections.Generic;
using Game.Item;
using UnityEngine;

namespace Game.Drop
{
    /// <summary>1つのレア度の抽選枠。weight はレア度プール同士の比較に使う。</summary>
    [Serializable]
    public class DropRarityPool
    {
        [SerializeField] private ItemRarity rarity = ItemRarity.N;
        [Min(0f)]
        [SerializeField] private float weight = 10f;
        [Tooltip("このレア度が当たったときに抽選されるアイテム。ItemParameter 側でそのレア度が有効なものだけが対象になる。")]
        [SerializeField] private List<DropItemEntry> items = new List<DropItemEntry>();

        public ItemRarity Rarity => rarity;
        public float Weight => weight;
        public IReadOnlyList<DropItemEntry> Items => items;
    }
}