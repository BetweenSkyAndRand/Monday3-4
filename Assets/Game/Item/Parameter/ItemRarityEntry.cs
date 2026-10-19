using System;
using UnityEngine;

namespace Game.Item.Parameter
{
    /// <summary>
    /// アイテム1つが持つレア度ごとの設定。チェック（isEnabled）のないレア度は抽選・生成の対象にならない。
    /// </summary>
    [Serializable]
    public class ItemRarityEntry
    {
        [SerializeField] private ItemRarity rarity;
        [Tooltip("チェックのないレア度は、ドロップ・宝箱の抽選対象にならず、生成もされない。")]
        [SerializeField] private bool isEnabled;
        [Tooltip("分類が Weapon のときに使用する攻撃力。")]
        [Min(0)]
        [SerializeField] private int attackPower;
        [Tooltip("分類が Recovery のときに使用する回復量。")]
        [Min(0)]
        [SerializeField] private int healAmount;

        public ItemRarity Rarity => rarity;
        public bool IsEnabled => isEnabled;
        public int AttackPower => attackPower;
        public int HealAmount => healAmount;

        public ItemRarityEntry(ItemRarity rarity, bool isEnabled, int attackPower = 0, int healAmount = 0)
        {
            this.rarity = rarity;
            this.isEnabled = isEnabled;
            this.attackPower = attackPower;
            this.healAmount = healAmount;
        }
    }
}