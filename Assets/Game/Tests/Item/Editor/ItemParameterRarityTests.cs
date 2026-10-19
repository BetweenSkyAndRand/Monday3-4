using System.Collections.Generic;
using System.Reflection;
using Game.Item;
using Game.Item.Parameter;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Item
{
    public class ItemParameterRarityTests
    {
        private ItemParameter itemParameter;

        [TearDown]
        public void TearDown()
        {
            if (itemParameter != null)
            {
                Object.DestroyImmediate(itemParameter);
            }
        }

        private ItemParameter CreateItemParameter(List<ItemRarityEntry> rarityEntries)
        {
            itemParameter = ScriptableObject.CreateInstance<ItemParameter>();
            var rarityEntriesField = typeof(ItemParameter).GetField("rarityEntries", BindingFlags.NonPublic | BindingFlags.Instance);
            rarityEntriesField.SetValue(itemParameter, rarityEntries);
            return itemParameter;
        }

        [Test]
        public void DefaultEntries_OnlyNormalIsEnabled()
        {
            itemParameter = ScriptableObject.CreateInstance<ItemParameter>();

            Assert.IsTrue(itemParameter.IsRarityEnabled(ItemRarity.N));
            Assert.IsFalse(itemParameter.IsRarityEnabled(ItemRarity.R));
            Assert.IsFalse(itemParameter.IsRarityEnabled(ItemRarity.SR));
            Assert.IsFalse(itemParameter.IsRarityEnabled(ItemRarity.UR));
        }

        [Test]
        public void GetAttackPower_ReturnsValuePerRarity()
        {
            var parameter = CreateItemParameter(new List<ItemRarityEntry>
            {
                new ItemRarityEntry(ItemRarity.N, true, 20),
                new ItemRarityEntry(ItemRarity.R, true, 35),
                new ItemRarityEntry(ItemRarity.SR, false, 60)
            });

            Assert.AreEqual(20, parameter.GetAttackPower(ItemRarity.N));
            Assert.AreEqual(35, parameter.GetAttackPower(ItemRarity.R));
            Assert.AreEqual(0, parameter.GetAttackPower(ItemRarity.UR));
        }

        [Test]
        public void DisabledOrMissingRarity_IsNotEnabled()
        {
            var parameter = CreateItemParameter(new List<ItemRarityEntry>
            {
                new ItemRarityEntry(ItemRarity.N, false),
                new ItemRarityEntry(ItemRarity.R, true)
            });

            Assert.IsFalse(parameter.IsRarityEnabled(ItemRarity.N));
            Assert.IsTrue(parameter.IsRarityEnabled(ItemRarity.R));
            Assert.IsFalse(parameter.IsRarityEnabled(ItemRarity.UR));
            Assert.IsTrue(parameter.HasAnyEnabledRarity());
        }

        [Test]
        public void HasAnyEnabledRarity_FalseWhenAllDisabled()
        {
            var parameter = CreateItemParameter(new List<ItemRarityEntry>
            {
                new ItemRarityEntry(ItemRarity.N, false)
            });

            Assert.IsFalse(parameter.HasAnyEnabledRarity());
        }
    }
}