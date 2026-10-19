using NUnit.Framework;
using UnityEngine;
using Game.Grid;

namespace Game.Tests.Grid.Editor
{
    /// <summary>
    /// GridOccupationManagerの単体テスト。
    /// グリッド占有管理の動作を検証する。
    /// </summary>
    public class GridOccupationManagerTests
    {
        private GameObject testObject1;
        private GameObject testObject2;

        [SetUp]
        public void SetUp()
        {
            testObject1 = new GameObject("TestObject1");
            testObject2 = new GameObject("TestObject2");
            GridOccupationManager.Clear();
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
            GridOccupationManager.Clear();
        }

        [Test]
        public void Occupy_グリッドを占有できる()
        {
            // Arrange
            var gridPos = new Vector2Int(0, 0);

            // Act
            GridOccupationManager.Occupy(gridPos, testObject1);

            // Assert
            Assert.IsTrue(GridOccupationManager.IsOccupied(gridPos, out var occupant));
            Assert.AreEqual(testObject1, occupant);
        }

        [Test]
        public void Occupy_nullを渡しても例外を投げない()
        {
            // Arrange
            var gridPos = new Vector2Int(0, 0);

            // Act & Assert
            Assert.DoesNotThrow(() => GridOccupationManager.Occupy(gridPos, null));
            Assert.IsFalse(GridOccupationManager.IsOccupied(gridPos, out _));
        }

        [Test]
        public void IsOccupied_占有されていないグリッドはfalseを返す()
        {
            // Arrange
            var gridPos = new Vector2Int(0, 0);

            // Act
            var isOccupied = GridOccupationManager.IsOccupied(gridPos, out var occupant);

            // Assert
            Assert.IsFalse(isOccupied);
            Assert.IsNull(occupant);
        }

        [Test]
        public void Release_占有を解除できる()
        {
            // Arrange
            var gridPos = new Vector2Int(0, 0);
            GridOccupationManager.Occupy(gridPos, testObject1);

            // Act
            GridOccupationManager.Release(gridPos);

            // Assert
            Assert.IsFalse(GridOccupationManager.IsOccupied(gridPos, out _));
        }

        [Test]
        public void Release_存在しないグリッドを解除しても例外を投げない()
        {
            // Arrange
            var gridPos = new Vector2Int(999, 999);

            // Act & Assert
            Assert.DoesNotThrow(() => GridOccupationManager.Release(gridPos));
        }

        [Test]
        public void ReleaseByOccupant_GameObjectが占有している全てのグリッドを解放する()
        {
            // Arrange
            var gridPos1 = new Vector2Int(0, 0);
            var gridPos2 = new Vector2Int(1, 0);
            var gridPos3 = new Vector2Int(2, 0);

            GridOccupationManager.Occupy(gridPos1, testObject1);
            GridOccupationManager.Occupy(gridPos2, testObject1);
            GridOccupationManager.Occupy(gridPos3, testObject2);

            // Act
            GridOccupationManager.ReleaseByOccupant(testObject1);

            // Assert
            Assert.IsFalse(GridOccupationManager.IsOccupied(gridPos1, out _));
            Assert.IsFalse(GridOccupationManager.IsOccupied(gridPos2, out _));
            Assert.IsTrue(GridOccupationManager.IsOccupied(gridPos3, out var occupant));
            Assert.AreEqual(testObject2, occupant);
        }

        [Test]
        public void ReleaseByOccupant_nullを渡しても例外を投げない()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => GridOccupationManager.ReleaseByOccupant(null));
        }

        [Test]
        public void Clear_全ての占有情報をクリアする()
        {
            // Arrange
            var gridPos1 = new Vector2Int(0, 0);
            var gridPos2 = new Vector2Int(1, 0);

            GridOccupationManager.Occupy(gridPos1, testObject1);
            GridOccupationManager.Occupy(gridPos2, testObject2);

            // Act
            GridOccupationManager.Clear();

            // Assert
            Assert.IsFalse(GridOccupationManager.IsOccupied(gridPos1, out _));
            Assert.IsFalse(GridOccupationManager.IsOccupied(gridPos2, out _));
        }

        [Test]
        public void Occupy_同じグリッドを上書きできる()
        {
            // Arrange
            var gridPos = new Vector2Int(0, 0);
            GridOccupationManager.Occupy(gridPos, testObject1);

            // Act
            GridOccupationManager.Occupy(gridPos, testObject2);

            // Assert
            Assert.IsTrue(GridOccupationManager.IsOccupied(gridPos, out var occupant));
            Assert.AreEqual(testObject2, occupant);
        }

        [Test]
        public void DebugLogOccupation_例外を投げない()
        {
            // Arrange
            GridOccupationManager.Occupy(new Vector2Int(0, 0), testObject1);
            GridOccupationManager.Occupy(new Vector2Int(1, 1), testObject2);

            // Act & Assert
            Assert.DoesNotThrow(() => GridOccupationManager.DebugLogOccupation());
        }
    }
}

