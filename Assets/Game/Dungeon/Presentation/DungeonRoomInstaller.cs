using System.Collections.Generic;
using UnityEngine;
using Game.Dungeon.View;

// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// 部屋の確定配置を専任で担うインストーラー。
    /// Entity 生成・プレハブ Instantiate・リポジトリへのキャッシュ・ドア接続を一元管理する。
    /// MonoBehaviour に依存しない純粋 C# クラス。DungeonController がライフサイクルを管理する。
    ///
    /// 責務:
    ///   - 確定配置 Entity の生成と View の Instantiate
    ///   - DungeonPlacedRoomRepository へのキャッシュ登録
    ///   - ConnectionUseCase を通じた隣接ドア接続
    ///   - 部屋グリッド座標 → View のマッピング管理（ドア接続参照用）
    /// </summary>
    public class DungeonRoomInstaller
    {
        private readonly DungeonPlacedRoomRepository repository;
        private readonly DungeonRoomConnectionUseCase connectionUseCase;
        private readonly DungeonRoomDisconnectionUseCase disconnectionUseCase = new();
        private readonly DungeonRoomParameter roomParameter;
        private readonly DungeonCellParameter cellParameter;
        private readonly GameObject placedRoomPrefab;

        /// <summary>動的生成される部屋の親 Transform。null の場合はシーンルートに配置される。</summary>
        private Transform roomsParent;

        /// <summary>部屋グリッド座標 → 確定済み View のマッピング（ドア接続に使用）</summary>
        private readonly Dictionary<Vector2Int, DungeonPlacedRoomView> placedViews = new();

        /// <summary>外部が読み取り専用で部屋データにアクセスするためのプロパティ。</summary>
        public IReadOnlyDungeonRoomRepository RoomRepository => repository;

        /// <summary>
        /// 部屋生成時の親 Transform を設定する。
        /// この親配下に部屋が Instantiate される。null の場合はシーンルートに配置される。
        /// </summary>
        public void SetRoomsParent(Transform parent)
        {
            roomsParent = parent;
        }

        public DungeonRoomInstaller(
            DungeonPlacedRoomRepository repository,
            DungeonRoomConnectionUseCase connectionUseCase,
            DungeonRoomParameter roomParameter,
            DungeonCellParameter cellParameter,
            GameObject placedRoomPrefab,
            Transform roomsParent = null)
        {
            this.repository       = repository;
            this.connectionUseCase = connectionUseCase;
            this.roomParameter    = roomParameter;
            this.cellParameter    = cellParameter;
            this.placedRoomPrefab = placedRoomPrefab;
            this.roomsParent      = roomsParent;
        }

        /// <summary>
        /// 中央部屋（初期部屋）を生成してリポジトリとシーンに登録する。
        /// 配置判定は行わず直接キャッシュする。ドア接続も行わない（隣接部屋がないため）。
        /// </summary>
        /// <returns>生成に成功した場合は true。</returns>
        public bool TryInstallCentralRoom()
        {
            return TryInstallAtGridPos(Vector2Int.zero, Color.white, connectDoors: false);
        }

        /// <summary>
        /// 指定グリッド座標に部屋を確定配置し、隣接部屋とのドアを接続する。
        /// </summary>
        /// <returns>生成に成功した場合は true。</returns>
        public bool TryInstallRoom(Vector2Int gridPos)
        {
            return TryInstallAtGridPos(gridPos, Color.white, connectDoors: true);
        }

        /// <summary>
        /// 指定した部屋グリッド座標の確定済み View を取得する。
        /// ダンジョン崩壊システムなど、外部から特定の部屋の View を操作したい場合に使用する。
        /// </summary>
        /// <returns>View が存在する場合は true。</returns>
        public bool TryGetPlacedView(Vector2Int roomGridPos, out DungeonPlacedRoomView view)
        {
            return placedViews.TryGetValue(roomGridPos, out view);
        }

        /// <summary>
        /// 指定した部屋グリッド座標の部屋を完全に除去する（View の破棄 + リポジトリからの削除）。
        /// ダンジョン崩壊システムから、部屋の消滅処理として呼ばれる。
        /// 削除前に、接続していた隣接部屋（中央拠点含む）側の壁を閉じる。
        /// </summary>
        /// <returns>除去に成功した場合は true。</returns>
        public bool RemoveRoom(Vector2Int roomGridPos)
        {
            // 削除前に隣接部屋を判定する必要がある（削除後は repository から情報が失われるため）
            CloseNeighborWalls(roomGridPos);

            if (placedViews.TryGetValue(roomGridPos, out var view))
            {
                if (view != null)
                {
                    Object.Destroy(view.gameObject);
                }
                placedViews.Remove(roomGridPos);
            }

            return repository.Remove(roomGridPos);
        }

        /// <summary>
        /// 削除対象部屋に接続していた隣接部屋（中央拠点含む）の壁を通常壁へ戻す。
        /// </summary>
        private void CloseNeighborWalls(Vector2Int targetRoomGridPos)
        {
            var disconnections = disconnectionUseCase.Execute(targetRoomGridPos, repository);
            foreach (var disconnection in disconnections)
            {
                if (placedViews.TryGetValue(disconnection.NeighborRoomGridPosition, out var neighborView) && neighborView != null)
                {
                    neighborView.SetWall(disconnection.DirectionToClose);
                }
            }
        }

        // ──────────────────────────────────────────
        // private
        // ──────────────────────────────────────────

        private bool TryInstallAtGridPos(Vector2Int gridPos, Color color, bool connectDoors)
        {
            var entity = DungeonRoomEntityFactory.Create(
                gridPos, roomParameter, cellParameter, color);

            var worldCenter = DungeonRoomEntityFactory.GetRoomWorldCenter(
                gridPos, roomParameter, cellParameter);

            var go   = Object.Instantiate(placedRoomPrefab, worldCenter, Quaternion.identity, roomsParent);
            var view = go.GetComponent<DungeonPlacedRoomView>();

            if (view == null)
            {
                UnityEngine.Debug.LogError(
                    "[DungeonRoomInstaller] placedRoomPrefab に DungeonPlacedRoomView がアタッチされていません。配置を中断します。");
                Object.Destroy(go);
                return false;
            }

            view.Initialize(entity);
            placedViews[gridPos] = view;

            // ドア接続より先にリポジトリへ登録する（ConnectionUseCase が参照するため）
            repository.Cache(entity);

            if (connectDoors)
            {
                ConnectDoors(entity, view);
            }

            return true;
        }

        /// <summary>
        /// ConnectionUseCase で隣接部屋を検出し、新部屋と隣接部屋の両側にドアを開ける。
        /// </summary>
        private void ConnectDoors(DungeonRoomEntity newEntity, DungeonPlacedRoomView newView)
        {
            var connections = connectionUseCase.Execute(newEntity);
            foreach (var conn in connections)
            {
                newView.SetDoor(conn.NewRoomDirection);

                if (placedViews.TryGetValue(conn.NeighborRoomEntity.RoomGridPosition, out var neighborView))
                {
                    neighborView.SetDoor(conn.NeighborDirection);
                }
            }
        }
    }
}

