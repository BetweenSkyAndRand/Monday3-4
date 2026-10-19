using System.Collections.Generic;
using Game.Item;
using UnityEngine;

namespace Game.Drop
{
    /// <summary>
    /// ドロップテーブルから2段階抽選（レア度 → アイテム）を行う。
    /// dropRateMultiplier は「なし」以外のレア度プールの重みにだけ掛け、「なし」の重みは変えない。
    /// </summary>
    public class DropTableRollUseCase
    {
        public bool TryRoll(EnemyDropTableParameter table, float dropRateMultiplier, IDropRandom random, out ItemStack itemStack)
        {
            itemStack = default;
            if (table == null || random == null)
            {
                return false;
            }

            var safeMultiplier = Mathf.Max(0f, dropRateMultiplier);
            var poolWeights = new List<float>();
            var totalWeight = Mathf.Max(0f, table.NoneWeight);
            foreach (var rarityPool in table.RarityPools)
            {
                var poolWeight = rarityPool != null && HasEligibleItem(rarityPool)
                    ? Mathf.Max(0f, rarityPool.Weight) * safeMultiplier
                    : 0f;
                poolWeights.Add(poolWeight);
                totalWeight += poolWeight;
            }

            if (totalWeight <= 0f)
            {
                return false;
            }

            var rarityRoll = random.Range(0f, totalWeight);
            if (rarityRoll < table.NoneWeight)
            {
                return false;
            }

            var selectedPool = SelectPool(table, poolWeights, rarityRoll - table.NoneWeight);
            if (selectedPool == null)
            {
                return false;
            }

            var selectedEntry = SelectItem(selectedPool, random);
            if (selectedEntry == null)
            {
                return false;
            }

            var quantity = random.RangeInt(selectedEntry.MinQuantity, selectedEntry.MaxQuantity + 1);
            itemStack = new ItemStack(selectedEntry.Item.ItemId, (uint)quantity, selectedPool.Rarity);
            return itemStack.IsValid;
        }

        private static bool HasEligibleItem(DropRarityPool rarityPool)
        {
            foreach (var entry in rarityPool.Items)
            {
                if (IsEligible(entry, rarityPool.Rarity))
                {
                    return true;
                }
            }

            return false;
        }

        // アイテムが未設定、重み0、またはそのレア度が無効（生成しない設定）のものは抽選対象にしない
        private static bool IsEligible(DropItemEntry entry, ItemRarity rarity)
        {
            return entry != null && entry.Item != null && entry.Weight > 0f && entry.Item.IsRarityEnabled(rarity);
        }

        private static DropRarityPool SelectPool(EnemyDropTableParameter table, List<float> poolWeights, float remainingRoll)
        {
            DropRarityPool lastEligiblePool = null;
            for (var poolIndex = 0; poolIndex < poolWeights.Count; poolIndex++)
            {
                if (poolWeights[poolIndex] <= 0f)
                {
                    continue;
                }

                lastEligiblePool = table.RarityPools[poolIndex];
                if (remainingRoll < poolWeights[poolIndex])
                {
                    return lastEligiblePool;
                }

                remainingRoll -= poolWeights[poolIndex];
            }

            return lastEligiblePool;
        }

        private static DropItemEntry SelectItem(DropRarityPool rarityPool, IDropRandom random)
        {
            var eligibleEntries = new List<DropItemEntry>();
            var totalWeight = 0f;
            foreach (var entry in rarityPool.Items)
            {
                if (!IsEligible(entry, rarityPool.Rarity))
                {
                    continue;
                }

                eligibleEntries.Add(entry);
                totalWeight += entry.Weight;
            }

            if (eligibleEntries.Count == 0)
            {
                return null;
            }

            var itemRoll = random.Range(0f, totalWeight);
            foreach (var entry in eligibleEntries)
            {
                if (itemRoll < entry.Weight)
                {
                    return entry;
                }

                itemRoll -= entry.Weight;
            }

            return eligibleEntries[eligibleEntries.Count - 1];
        }
    }
}