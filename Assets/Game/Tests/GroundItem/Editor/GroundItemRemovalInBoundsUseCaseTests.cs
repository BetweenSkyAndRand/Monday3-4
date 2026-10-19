using System.Collections.Generic;
using Game.Item;
using NUnit.Framework;
using UnityEngine;

namespace Game.GroundItem.Tests
{
    public class GroundItemRemovalInBoundsUseCaseTests
    {
        [Test]
        public void Execute_ReturnsOnlyItemsInsideBounds()
        {
            var entities = new List<GroundItemEntity>
            {
                new GroundItemEntity("inside", new ItemStack("item", 1, ItemRarity.N), new Vector3(1f, 0f, 1f), 0f),
                new GroundItemEntity("outside", new ItemStack("item", 1, ItemRarity.N), new Vector3(50f, 0f, 50f), 0f),
            };

            var removalInstanceIds = new GroundItemRemovalInBoundsUseCase().Execute(entities, position => position.x < 10f);

            Assert.AreEqual(1, removalInstanceIds.Count);
            Assert.AreEqual("inside", removalInstanceIds[0]);
        }
    }
}
