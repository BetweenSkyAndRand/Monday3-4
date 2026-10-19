using System.Collections.Generic;
using UnityEngine;
using Game.Weapon;

namespace Game.Item.Parameter
{
    /// <summary>
    /// アイテム1種類のマスターデータ（ScriptableObject）。アイテムのマスターデータはこのクラスに一本化している。
    /// レア度ごとの有効・攻撃力・回復量は rarityEntries で設定する。
    /// </summary>
    [CreateAssetMenu(fileName = "ItemParameter", menuName = "Game/Item/ItemParameter")]
    public class ItemParameter : ScriptableObject
    {
        [Header("基本情報")]
        [SerializeField] private string itemId;
        [SerializeField] private string itemName;
        [SerializeField] private ItemCategory category = ItemCategory.Other;
        [Min(1)]
        [SerializeField] private int maxStackSize = 1;

        [Header("説明")]
        [TextArea(3, 5)]
        [SerializeField] private string description;

        [Header("購入")]
        [Tooltip("ショップでの購入価格。ショップの並び順は ItemDatabase の登録順に従う。")]
        [SerializeField] private uint purchasePrice;

        [Header("売却")]
        [SerializeField] private bool isSellable = true;
        [Tooltip("レア度Nのときの売却額。実際の売却額は ItemSellPriceCalculatorUseCase で求める。")]
        [SerializeField] private uint baseSellPrice;

        [Header("ビジュアル")]
        [Tooltip("全レア度で共通のアイコン。レア度の枠は表示側でアイコンの上に重ねる。")]
        [SerializeField] private Sprite icon;
        [Tooltip("地面に置くときに生成するプレハブ。")]
        [SerializeField] private GameObject worldPrefab;

        [Header("レア度")]
        [Tooltip("N / R / SR / UR の4行を設定する。チェックのないレア度は生成されない。")]
        [SerializeField] private List<ItemRarityEntry> rarityEntries = CreateDefaultRarityEntries();

        [Header("分類ごとの設定")]
        [Tooltip("category が Weapon のときのみ使用する（モデル・HitBox・演出）。攻撃力はレア度の行で設定する。")]
        [SerializeField] private WeaponData weaponData;

        [Header("武器モデル（手に持つ見た目）")]
        [Tooltip("未設定の場合は WeaponData / JSON 側のモデル設定を使う。設定した場合は、下のスケール・オフセット・回転もこちらを使う。")]
        [SerializeField] private GameObject weaponModelPrefab;
        [SerializeField] private float weaponModelScale = 1f;
        [SerializeField] private Vector3 weaponModelOffset;
        [SerializeField] private Vector3 weaponModelRotation;

        public GameObject WeaponModelPrefab => weaponModelPrefab;
        public float WeaponModelScale => weaponModelScale;
        public Vector3 WeaponModelOffset => weaponModelOffset;
        public Vector3 WeaponModelRotation => weaponModelRotation;

        public string ItemId => itemId;
        public string ItemName => itemName;
        public ItemCategory Category => category;
        public int MaxStackSize => maxStackSize;
        public string Description => description;
        public uint PurchasePrice => purchasePrice;
        public bool IsSellable => isSellable;
        public uint BaseSellPrice => baseSellPrice;
        public Sprite Icon => icon;
        public GameObject WorldPrefab => worldPrefab;
        public WeaponData WeaponData => weaponData;
        public IReadOnlyList<ItemRarityEntry> RarityEntries => rarityEntries;

        public bool TryGetRarityEntry(ItemRarity rarity, out ItemRarityEntry rarityEntry)
        {
            if (rarityEntries != null)
            {
                foreach (var candidateEntry in rarityEntries)
                {
                    if (candidateEntry != null && candidateEntry.Rarity == rarity)
                    {
                        rarityEntry = candidateEntry;
                        return true;
                    }
                }
            }

            rarityEntry = null;
            return false;
        }

        public bool IsRarityEnabled(ItemRarity rarity)
        {
            return TryGetRarityEntry(rarity, out var rarityEntry) && rarityEntry.IsEnabled;
        }

        public bool HasAnyEnabledRarity()
        {
            foreach (var rarity in ItemRarityUtility.AllRarities)
            {
                if (IsRarityEnabled(rarity))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>レア度に対応する攻撃力。行がない場合は 0。</summary>
        public int GetAttackPower(ItemRarity rarity)
        {
            return TryGetRarityEntry(rarity, out var rarityEntry) ? rarityEntry.AttackPower : 0;
        }

        /// <summary>レア度に対応する回復量。行がない場合は 0。</summary>
        public int GetHealAmount(ItemRarity rarity)
        {
            return TryGetRarityEntry(rarity, out var rarityEntry) ? rarityEntry.HealAmount : 0;
        }

        private static List<ItemRarityEntry> CreateDefaultRarityEntries()
        {
            var defaultEntries = new List<ItemRarityEntry>();
            foreach (var rarity in ItemRarityUtility.AllRarities)
            {
                defaultEntries.Add(new ItemRarityEntry(rarity, rarity == ItemRarity.N));
            }

            return defaultEntries;
        }

        private void OnValidate()
        {
            if (!HasAnyEnabledRarity())
            {
                UnityEngine.Debug.LogWarning($"[ItemParameter] 有効なレア度が1つもありません。このアイテムは生成されません: {name}", this);
            }

            if (weaponData != null && weaponData.ItemParameter != this)
            {
                UnityEngine.Debug.LogWarning($"[ItemParameter] WeaponData '{weaponData.name}' の Item Parameter がこのアイテムに設定されていません。武器名・ItemId・攻撃力が解決できません: {name}", this);
            }

            var seenRarities = new HashSet<ItemRarity>();
            foreach (var rarityEntry in rarityEntries)
            {
                if (rarityEntry != null && !seenRarities.Add(rarityEntry.Rarity))
                {
                    UnityEngine.Debug.LogWarning($"[ItemParameter] レア度 {rarityEntry.Rarity} の行が重複しています: {name}", this);
                }
            }
        }
    }
}
