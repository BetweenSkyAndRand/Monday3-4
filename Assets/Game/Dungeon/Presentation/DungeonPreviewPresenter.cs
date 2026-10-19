using Game.Player.Script;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// ビルドモード中のプレビュー表示を専任で担うプレゼンター。
    /// マウス位置の Raycast からグリッド座標を計算し、配置可否に応じてプレビュー View の色を更新する。
    /// MonoBehaviour に依存しない純粋 C# クラス。DungeonController がライフサイクルを管理する。
    ///
    /// 責務:
    ///   - マウス位置の取得・Raycast によるグリッド座標の解決
    ///   - プレビュー View の生成（初回のみ）・表示/非表示切り替え
    ///   - DungeonRoomEntity の生成と PlacementUseCase への配置可否問い合わせ
    ///   - 配置可否に応じたプレビュー色の更新
    /// </summary>
    public class DungeonPreviewPresenter
    {
        private readonly DungeonRoomPlacementUseCase placementUseCase;
        private readonly DungeonRoomParameter roomParameter;
        private readonly DungeonCellParameter cellParameter;
        private readonly Camera mainCamera;
        private readonly LayerMask groundMask;
        private readonly GameInputReader gameInputReader;
        private readonly GameObject previewPrefab;

        private DungeonPreviewRoomView previewView;
        private Vector2Int currentGridPos = new Vector2Int(int.MinValue, int.MinValue);

        /// <summary>現在のプレビュー位置に部屋を配置できるか。</summary>
        public bool CanPlace { get; private set; }

        /// <summary>現在のプレビュー位置の判定結果（配置不可の理由つき）。</summary>
        public DungeonPlacementResult PlacementResult { get; private set; } = DungeonPlacementResult.NoNeighbor;

        /// <summary>現在のプレビューが指しているグリッド座標。</summary>
        public Vector2Int CurrentGridPos => currentGridPos;

        public DungeonPreviewPresenter(
            DungeonRoomPlacementUseCase placementUseCase,
            DungeonRoomParameter roomParameter,
            DungeonCellParameter cellParameter,
            Camera mainCamera,
            LayerMask groundMask,
            GameInputReader gameInputReader,
            GameObject previewPrefab)
        {
            this.placementUseCase = placementUseCase;
            this.roomParameter    = roomParameter;
            this.cellParameter    = cellParameter;
            this.mainCamera       = mainCamera;
            this.groundMask       = groundMask;
            this.gameInputReader  = gameInputReader;
            this.previewPrefab    = previewPrefab;
        }

        /// <summary>
        /// プレビュー View の表示/非表示を切り替える。
        /// ビルドモード終了時に非表示にするために使用する。
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (previewView == null)
            {
                return;
            }

            previewView.gameObject.SetActive(visible);
        }

        /// <summary>
        /// グリッド座標のキャッシュをリセットする。
        /// ビルドモード終了時に呼ぶことで、次回入室時に同座標でも canPlace を必ず再評価させる。
        /// </summary>
        public void ResetGridPos()
        {
            currentGridPos = new Vector2Int(int.MinValue, int.MinValue);
        }

        /// <summary>
        /// マウス位置をもとにプレビュー内容を更新する。
        /// グリッド座標が変化していない場合はスキップしてアロケーションを削減する。
        /// </summary>
        public void Refresh()
        {
            if (gameInputReader == null || mainCamera == null)
            {
                return;
            }

            var mousePos = gameInputReader.MouseScreenPosition;
            var ray = mainCamera.ScreenPointToRay(mousePos);

            // Raycast がヒットしない場合（マウスが地面外）はプレビューを更新しない
            if (!Physics.Raycast(ray, out RaycastHit hit, 1000f, groundMask))
            {
                return;
            }

            var newGridPos = DungeonRoomEntityFactory.WorldToRoomGrid(
                hit.point, roomParameter, cellParameter);

            // グリッド座標が変化したときだけエンティティを再生成してアロケーションを削減する
            if (newGridPos == currentGridPos)
            {
                return;
            }

            currentGridPos = newGridPos;

            // プレビュー View の生成（初回のみ Instantiate）
            var currentEntity = DungeonRoomEntityFactory.Create(
                currentGridPos, roomParameter, cellParameter,
                cellParameter.ColorBlocked // 初期は赤（下で上書き）
            );

            if (previewView == null)
            {
                var go = Object.Instantiate(previewPrefab);
                previewView = go.GetComponent<DungeonPreviewRoomView>();
            }

            previewView.ApplyDungeonRoomEntity(currentEntity);

            // 配置可否を問い合わせて色を反映する
            var newPlacementResult = placementUseCase.Evaluate(currentEntity);
            CanPlace = newPlacementResult == DungeonPlacementResult.Placeable;
            PlacementResult = newPlacementResult;

            var color = CanPlace ? cellParameter.ColorPlaceable : cellParameter.ColorBlocked;
            previewView.SetCellColor(new DungeonCell.ViewModel { cellColor = color });
        }
    }
}



