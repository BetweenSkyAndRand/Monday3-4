using NUnit.Framework;
using UnityEngine;
using Game.Player.Script;
using Game.Grid;
using GridOccupationManager = Game.Grid.GridOccupationManager;

namespace Game.Tests.Grid.Editor
{
    /// <summary>
    /// GridMoverの単体テスト。
    /// グリッド座標計算、移動判定、占有管理の動作を検証する。
    /// </summary>
    public class GridMoverTests
    {
        private GameObject testObject;
        private const float CellSize = 1f;

        [SetUp]
        public void SetUp()
        {
            // テスト用GameObjectを作成
            testObject = new GameObject("TestObject");

            // 占有状態をクリア
            GridOccupationManager.Clear();
        }

        [TearDown]
        public void TearDown()
        {
            // テスト用GameObjectを破棄
            if (testObject != null)
            {
                Object.DestroyImmediate(testObject);
            }

            // 占有状態をクリア
            GridOccupationManager.Clear();
        }

        [Test]
        public void GridMover_初期化時に初期グリッドを占有する()
        {
            // Arrange
            var initialGrid = new Vector2Int(0, 0);

            // Act
            var gridMover = new GridMover(CellSize, initialGrid, testObject, new PlayerMovementStrategy());

            // Assert
            Assert.IsTrue(GridOccupationManager.IsOccupied(initialGrid, out var occupant));
            Assert.AreEqual(testObject, occupant);
            Assert.AreEqual(initialGrid, gridMover.CurrentGridPos);
        }

        [Test]
        public void GetNextGrid_正しい次のグリッド座標を返す()
        {
            // Arrange
            var initialGrid = new Vector2Int(5, 5);
            var gridMover = new GridMover(CellSize, initialGrid, testObject, new PlayerMovementStrategy());

            // Act & Assert
            Assert.AreEqual(new Vector2Int(6, 5), gridMover.GetNextGrid(Vector2Int.right));
            Assert.AreEqual(new Vector2Int(4, 5), gridMover.GetNextGrid(Vector2Int.left));
            Assert.AreEqual(new Vector2Int(5, 6), gridMover.GetNextGrid(Vector2Int.up));
            Assert.AreEqual(new Vector2Int(5, 4), gridMover.GetNextGrid(Vector2Int.down));
        }

        [Test]
        public void TryGetNextPos_障害物がない場合はtrueを返す()
        {
            // Arrange
            var initialGrid = new Vector2Int(0, 0);
            testObject.transform.position = new Vector3(0, 0, 0);
            var gridMover = new GridMover(CellSize, initialGrid, testObject, new PlayerMovementStrategy());

            // Act
            var result = gridMover.TryGetNextPos(Vector2Int.right, 0f, out var worldPos);

            // Assert
            Assert.IsTrue(result);
            Assert.AreEqual(new Vector3(1f, 0f, 0f), worldPos);
        }

        [Test]
        public void ConfirmMove_グリッド座標を更新する()
        {
            // Arrange
            var initialGrid = new Vector2Int(0, 0);
            var gridMover = new GridMover(CellSize, initialGrid, testObject, new PlayerMovementStrategy());
            var nextGrid = new Vector2Int(1, 0);

            // Act
            gridMover.ConfirmMove(nextGrid);

            // Assert
            Assert.AreEqual(nextGrid, gridMover.CurrentGridPos);
        }

        [Test]
        public void ConfirmMove_占有状態を更新する()
        {
            // Arrange
            var initialGrid = new Vector2Int(0, 0);
            var gridMover = new GridMover(CellSize, initialGrid, testObject, new PlayerMovementStrategy());
            var nextGrid = new Vector2Int(1, 0);

            // Act
            gridMover.ConfirmMove(nextGrid);

            // Assert
            Assert.IsFalse(GridOccupationManager.IsOccupied(initialGrid, out _));
            Assert.IsTrue(GridOccupationManager.IsOccupied(nextGrid, out var occupant));
            Assert.AreEqual(testObject, occupant);
        }

        [Test]
        public void PlayerMovementStrategy_占有されているグリッドに移動できる()
        {
            // Arrange
            var testObject2 = new GameObject("TestObject2");
            var initialGrid1 = new Vector2Int(0, 0);
            var initialGrid2 = new Vector2Int(1, 0);

            testObject.transform.position = new Vector3(0, 0, 0);
            testObject2.transform.position = new Vector3(1, 0, 0);

            var gridMover1 = new GridMover(CellSize, initialGrid1, testObject, new PlayerMovementStrategy());
            var _ = new GridMover(CellSize, initialGrid2, testObject2, new EnemyMovementStrategy());

            // Act - プレイヤーが敵のいるグリッドに移動を試みる
            var canMove = gridMover1.TryGetNextPos(Vector2Int.right, 0f, out Vector3 _);

            // Assert
            Assert.IsTrue(canMove, "プレイヤーは占有されているグリッドに移動できる");

            // Cleanup
            GridOccupationManager.ReleaseByOccupant(testObject2);
            Object.DestroyImmediate(testObject2);
        }

        [Test]
        public void EnemyMovementStrategy_占有されているグリッドに移動できない()
        {
            // Arrange
            var testObject2 = new GameObject("TestObject2");
            var initialGrid1 = new Vector2Int(0, 0);
            var initialGrid2 = new Vector2Int(1, 0);

            testObject.transform.position = new Vector3(0, 0, 0);
            testObject2.transform.position = new Vector3(1, 0, 0);

            var gridMover1 = new GridMover(CellSize, initialGrid1, testObject, new EnemyMovementStrategy());
            var _ = new GridMover(CellSize, initialGrid2, testObject2, new EnemyMovementStrategy());

            // Act - 敵が別の敵のいるグリッドに移動を試みる
            var canMove = gridMover1.TryGetNextPos(Vector2Int.right, 0f, out Vector3 _);

            // Assert
            Assert.IsFalse(canMove);

            // Cleanup
            GridOccupationManager.ReleaseByOccupant(testObject2);
            Object.DestroyImmediate(testObject2);
        }

        [Test]
        public void ForceSetGridPosition_グリッド位置を強制設定できる()
        {
            // Arrange
            var initialGrid = new Vector2Int(0, 0);
            var gridMover = new GridMover(CellSize, initialGrid, testObject, new PlayerMovementStrategy());
            var forcedGrid = new Vector2Int(10, 10);

            // Act
            gridMover.ForceSetGridPosition(forcedGrid);

            // Assert
            Assert.AreEqual(forcedGrid, gridMover.CurrentGridPos);
        }
    }
}

