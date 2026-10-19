using NUnit.Framework;
using UnityEngine;
using Game.Grid;

namespace Game.Tests.Grid.Editor
{
    /// <summary>
    /// GridMovementStrategiesの単体テスト。
    /// プレイヤーと敵の移動戦略の動作を検証する。
    /// </summary>
    public class GridMovementStrategiesTests
    {
        private GameObject testObject1;
        private GameObject testObject2;

        [SetUp]
        public void SetUp()
        {
            testObject1 = new GameObject("TestObject1");
            testObject2 = new GameObject("TestObject2");
        }

        [TearDown]
        public void TearDown()
        {
            if (testObject1 != null)
            {
                Object.DestroyImmediate(testObject1);
            }
            if (testObject2 != null)
            {
                Object.DestroyImmediate(testObject2);
            }
        }

        [Test]
        public void PlayerMovementStrategy_CanMoveToOccupiedGrid_常にtrueを返す()
        {
            // Arrange
            var strategy = new PlayerMovementStrategy();

            // Act
            var canMove = strategy.CanMoveToOccupiedGrid(testObject2, testObject1);

            // Assert
            Assert.IsTrue(canMove);
        }

        [Test]
        public void PlayerMovementStrategy_ShouldNotifyOccupant_trueを返す()
        {
            // Arrange
            var strategy = new PlayerMovementStrategy();

            // Act
            var shouldNotify = strategy.ShouldNotifyOccupant();

            // Assert
            Assert.IsTrue(shouldNotify, "プレイヤーは占有者に通知する");
        }

        [Test]
        public void EnemyMovementStrategy_CanMoveToOccupiedGrid_falseを返す()
        {
            // Arrange
            var strategy = new EnemyMovementStrategy();

            // Act
            var canMove = strategy.CanMoveToOccupiedGrid(testObject2, testObject1);

            // Assert
            Assert.IsFalse(canMove, "敵は占有グリッドに移動できない");
        }

        [Test]
        public void EnemyMovementStrategy_ShouldNotifyOccupant_falseを返す()
        {
            // Arrange
            var strategy = new EnemyMovementStrategy();

            // Act
            var shouldNotify = strategy.ShouldNotifyOccupant();

            // Assert
            Assert.IsFalse(shouldNotify);
        }

        [Test]
        public void PlayerMovementStrategy_占有者がnullでも例外を投げない()
        {
            // Arrange
            var strategy = new PlayerMovementStrategy();

            // Act & Assert
            Assert.DoesNotThrow(() => strategy.CanMoveToOccupiedGrid(null, testObject1));
        }

        [Test]
        public void EnemyMovementStrategy_占有者がnullでも例外を投げない()
        {
            // Arrange
            var strategy = new EnemyMovementStrategy();

            // Act & Assert
            Assert.DoesNotThrow(() => strategy.CanMoveToOccupiedGrid(null, testObject1));
        }
    }
}

