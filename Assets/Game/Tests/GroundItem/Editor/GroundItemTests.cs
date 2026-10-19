using System.Collections.Generic;
using Game.GroundItem;
using Game.Item;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.GroundItem
{
    public class GroundItemTests
    {
        private static GroundItemEntity CreateEntity(string instanceId, float droppedTime)
        {
            return new GroundItemEntity(instanceId, new ItemStack("potion", 1), Vector3.zero, droppedTime);
        }

        [Test]
        public void Repository_SaveGetRemove()
        {
            var repository = new GroundItemRepository();
            repository.Save(CreateEntity("a", 0f));

            Assert.IsTrue(repository.TryGet("a", out var entity));
            Assert.AreEqual("potion", entity.ItemStack.ItemId);
            Assert.IsTrue(repository.Remove("a"));
            Assert.IsFalse(repository.Remove("a"));
            Assert.AreEqual(0, repository.Count);
        }

        [Test]
        public void Expiration_ReturnsOnlyItemsPastDespawnTime()
        {
            var entities = new List<GroundItemEntity> { CreateEntity("old", 0f), CreateEntity("new", 500f) };

            var expired = new GroundItemExpirationUseCase().Execute(entities, 600f, 600f);

            Assert.AreEqual(1, expired.Count);
            Assert.AreEqual("old", expired[0]);
        }

        [Test]
        public void Expiration_ZeroOrNegativeDespawnSecondsNeverExpires()
        {
            var entities = new List<GroundItemEntity> { CreateEntity("old", 0f) };

            Assert.AreEqual(0, new GroundItemExpirationUseCase().Execute(entities, 99999f, 0f).Count);
            Assert.AreEqual(0, new GroundItemExpirationUseCase().Execute(entities, 99999f, -1f).Count);
        }

        [Test]
        public void ItemStack_IsValidRequiresIdAndQuantity()
        {
            Assert.IsTrue(new ItemStack("a", 1).IsValid);
            Assert.IsFalse(new ItemStack("", 1).IsValid);
            Assert.IsFalse(new ItemStack("a", 0).IsValid);
        }
    }
}