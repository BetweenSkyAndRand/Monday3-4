using System.Collections.Generic;
using System.Reflection;
using Game.Drop;
using Game.Item;
using Game.Item.Parameter;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Drop.Editor
{
    public class DropTableRollUseCaseTests
    {
        private sealed class FixedRandom : IDropRandom
        {
            private readonly Queue<float> values;

            public FixedRandom(params float[] fixedValues)
            {
                values = new Queue<float>(fixedValues);
            }

            public float Range(float minInclusive, float maxExclusive)
            {
                return values.Dequeue();
            }

            public int RangeInt(int minInclusive, int maxExclusive)
            {
                return minInclusive;
            }
        }

        private readonly List<Object> createdObjects = new List<Object>();

        [TearDown]
        public void TearDown()
        {
            foreach (var createdObject in createdObjects)
            {
                Object.DestroyImmediate(createdObject);
            }

            createdObjects.Clear();
        }

        private static void SetField(object target, string fieldName, object value)
        {
            var field = target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(field, fieldName);
            field.SetValue(target, value);
        }

        private ItemParameter CreateItem(string itemId)
        {
            var item = ScriptableObject.CreateInstance<ItemParameter>();
            SetField(item, "itemId", itemId);
            createdObjects.Add(item);
            return item;
        }

        private static DropItemEntry CreateEntry(ItemParameter item, float weight)
        {
            var entry = new DropItemEntry();
            SetField(entry, "item", item);
            SetField(entry, "weight", weight);
            return entry;
        }

        private static DropRarityPool CreatePool(ItemRarity rarity, float weight, params DropItemEntry[] entries)
        {
            var pool = new DropRarityPool();
            SetField(pool, "rarity", rarity);
            SetField(pool, "weight", weight);
            SetField(pool, "items", new List<DropItemEntry>(entries));
            return pool;
        }

        private EnemyDropTableParameter CreateTable(float noneWeight, params DropRarityPool[] pools)
        {
            var table = ScriptableObject.CreateInstance<EnemyDropTableParameter>();
            SetField(table, "noneWeight", noneWeight);
            SetField(table, "rarityPools", new List<DropRarityPool>(pools));
            createdObjects.Add(table);
            return table;
        }

        [Test]
        public void TryRoll_NoneRoll_ReturnsFalse()
        {
            var table = CreateTable(100f, CreatePool(ItemRarity.N, 100f, CreateEntry(CreateItem("a"), 1f)));

            var rolled = new DropTableRollUseCase().TryRoll(table, 1f, new FixedRandom(50f), out _);

            Assert.IsFalse(rolled);
        }

        [Test]
        public void TryRoll_RarityPoolRoll_ReturnsItemWithPoolRarity()
        {
            var table = CreateTable(100f, CreatePool(ItemRarity.N, 100f, CreateEntry(CreateItem("a"), 1f)));

            var rolled = new DropTableRollUseCase().TryRoll(table, 1f, new FixedRandom(150f, 0.5f), out var itemStack);

            Assert.IsTrue(rolled);
            Assert.AreEqual("a", itemStack.ItemId);
            Assert.AreEqual(ItemRarity.N, itemStack.Rarity);
        }

        [Test]
        public void TryRoll_Multiplier_OnlyScalesNonNoneWeights()
        {
            var table = CreateTable(100f, CreatePool(ItemRarity.N, 100f, CreateEntry(CreateItem("a"), 1f)));

            // 倍率2 → 合計 100 + 200。150 は「なし」(0〜100) を外れてプールに入る
            var rolled = new DropTableRollUseCase().TryRoll(table, 2f, new FixedRandom(299f, 0.5f), out _);
            var noneRolled = new DropTableRollUseCase().TryRoll(table, 2f, new FixedRandom(99f), out _);

            Assert.IsTrue(rolled);
            Assert.IsFalse(noneRolled);
        }

        [Test]
        public void TryRoll_DisabledRarityItem_IsNotEligible()
        {
            // 既定では N のみ有効なので、SR プールのアイテムは抽選対象にならない
            var table = CreateTable(0f, CreatePool(ItemRarity.SR, 100f, CreateEntry(CreateItem("a"), 1f)));

            var rolled = new DropTableRollUseCase().TryRoll(table, 1f, new FixedRandom(0f), out _);

            Assert.IsFalse(rolled);
        }

        [Test]
        public void TryRoll_NullTable_ReturnsFalse()
        {
            Assert.IsFalse(new DropTableRollUseCase().TryRoll(null, 1f, new FixedRandom(), out _));
        }
    }
}