using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Dungeon
{
    /// <summary>
    /// 配置済みの部屋とプレイヤー位置だけを表示するミニマップ。
    /// プレイヤーのいる部屋を常に中心にし、タイルの大きさは固定。表示領域の外は親の Mask で切る。
    /// 敵・宝箱の情報は参照しないため、それらが映ることはない。
    /// タイルの位置はプレイヤーの部屋移動時・部屋数の変化時のみ更新する。
    /// </summary>
    public class DungeonMinimapView : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private DungeonController dungeonController;
        [SerializeField] private Transform playerTransform;

        [Header("表示要素")]
        [Tooltip("タイルの親。中心を原点として配置する（Mask のかかった領域の中央に置く）。")]
        [SerializeField] private RectTransform tileContainer;
        [Tooltip("部屋1つ分のタイル。Image を持つプレハブ（アンカーとピボットは中央）。")]
        [SerializeField] private Image roomTilePrefab;
        [Tooltip("プレイヤーを表すマーカー。常に中心に固定される。")]
        [SerializeField] private RectTransform playerMarker;

        [Header("見た目")]
        [SerializeField] private float tileSize = 24f;
        [SerializeField] private float tileGap = 2f;
        [SerializeField] private Color roomColor = new Color(1f, 1f, 1f, 0.6f);
        [SerializeField] private Color centralRoomColor = new Color(1f, 0.85f, 0.3f, 0.8f);

        private readonly DungeonMinimapPresenter presenter = new DungeonMinimapPresenter();
        private readonly List<Image> spawnedTiles = new List<Image>();
        private readonly List<Vector2Int> roomGridPositions = new List<Vector2Int>();
        private Vector2Int lastPlayerRoomGrid = new Vector2Int(int.MinValue, int.MinValue);

        private void OnEnable()
        {
            if (dungeonController != null)
            {
                dungeonController.OnPlacedRoomCountChanged += HandlePlacedRoomCountChanged;
            }
        }

        private void OnDisable()
        {
            if (dungeonController != null)
            {
                dungeonController.OnPlacedRoomCountChanged -= HandlePlacedRoomCountChanged;
            }
        }

        private void Start()
        {
            if (playerMarker != null)
            {
                playerMarker.anchoredPosition = Vector2.zero;
            }

            RebuildTiles();
        }

        private void Update()
        {
            if (TryGetPlayerRoomGrid(out var playerRoomGrid) && playerRoomGrid != lastPlayerRoomGrid)
            {
                RefreshTilePositions(playerRoomGrid);
            }
        }

        private void HandlePlacedRoomCountChanged(int placedRoomCount)
        {
            RebuildTiles();
        }

        private void RebuildTiles()
        {
            if (dungeonController == null || tileContainer == null || roomTilePrefab == null)
            {
                return;
            }

            // DungeonController の Start 前は未初期化。初期化完了時のイベントで再構築される
            var roomRepository = dungeonController.RoomRepository;
            if (roomRepository == null)
            {
                return;
            }

            roomGridPositions.Clear();
            foreach (var room in roomRepository.GetAll())
            {
                roomGridPositions.Add(room.RoomGridPosition);
            }

            // 部屋数に対して不足したタイルだけ生成する
            while (spawnedTiles.Count < roomGridPositions.Count)
            {
                var newTile = Instantiate(roomTilePrefab, tileContainer);
                newTile.rectTransform.sizeDelta = Vector2.one * Mathf.Max(0f, tileSize - tileGap);
                spawnedTiles.Add(newTile);
            }

            if (playerMarker != null)
            {
                playerMarker.SetAsLastSibling();
            }

            // 部屋が除去された直後などで基準の部屋を取得できない場合も、最後の基準でタイルを更新する
            TryGetPlayerRoomGrid(out var playerRoomGrid);
            if (playerRoomGrid.x == int.MinValue)
            {
                playerRoomGrid = Vector2Int.zero;
            }

            RefreshTilePositions(playerRoomGrid);
        }

        private void RefreshTilePositions(Vector2Int playerRoomGrid)
        {
            lastPlayerRoomGrid = playerRoomGrid;

            for (var tileIndex = 0; tileIndex < spawnedTiles.Count; tileIndex++)
            {
                var tile = spawnedTiles[tileIndex];
                if (tileIndex >= roomGridPositions.Count)
                {
                    tile.gameObject.SetActive(false);
                    continue;
                }

                var roomGridPosition = roomGridPositions[tileIndex];
                var anchoredPosition = presenter.ToAnchoredPosition(roomGridPosition, playerRoomGrid, tileSize);
                tile.rectTransform.anchoredPosition = anchoredPosition;

                // 背景枠から飛び出す部屋は非表示にし、枠内に入ったら再び表示する
                var isInsideView = presenter.IsInsideView(anchoredPosition, tileSize, tileContainer.rect.size);
                tile.gameObject.SetActive(isInsideView);
                tile.color = roomGridPosition == Vector2Int.zero ? centralRoomColor : roomColor;
            }
        }

        // プレイヤーが部屋の外にいる間は、最後にいた部屋を基準にし続ける
        private bool TryGetPlayerRoomGrid(out Vector2Int playerRoomGrid)
        {
            playerRoomGrid = lastPlayerRoomGrid;
            if (dungeonController == null || playerTransform == null)
            {
                return false;
            }

            var roomParameter = dungeonController.RoomParameter;
            var cellParameter = dungeonController.CellParameter;
            if (roomParameter == null || cellParameter == null)
            {
                return false;
            }

            var roomWorldSize = roomParameter.RoomWidth * cellParameter.CellSize;
            var candidateRoomGrid = presenter.WorldToRoomGrid(playerTransform.position, roomWorldSize);
            var roomRepository = dungeonController.RoomRepository;
            if (roomRepository != null && roomRepository.TryGetRoomByGridPos(candidateRoomGrid, out _))
            {
                playerRoomGrid = candidateRoomGrid;
                return true;
            }

            // 最後にいた部屋が未確定（初回）の場合は拠点部屋を基準にする
            if (lastPlayerRoomGrid.x == int.MinValue)
            {
                playerRoomGrid = Vector2Int.zero;
                return true;
            }

            return false;
        }
    }
}
