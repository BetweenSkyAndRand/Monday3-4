using Game.Dungeon;
using Game.Interaction;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace Game.BuildMode
{
    /// <summary>
    /// ビルドモード中だけ表示される HUD。配置済みの部屋数と、プレビュー位置の配置可否（理由つき）を表示する。
    /// 文字の背後には半透明の背景を置き、背景色でも配置可否を判別できるようにする。
    /// </summary>
    public class BuildModeHudView : MonoBehaviour
    {
        [Header("参照")]
        [SerializeField] private DungeonController dungeonController;
        [SerializeField] private InteractTextSettings textSettings;

        [Header("表示要素")]
        [Tooltip("ビルドモード中だけ有効にするルート。このコンポーネントとは別のオブジェクトにすること。")]
        [SerializeField] private GameObject root;
        [SerializeField] private TextMeshProUGUI roomCountText;
        [SerializeField] private GameObject messageRoot;
        [SerializeField] private TextMeshProUGUI messageText;
        [SerializeField] private Image messageBackground;

        [Header("メッセージ背景色（半透明）")]
        [SerializeField] private Color placeableBackgroundColor = new Color(0.1f, 0.5f, 0.2f, 0.7f);
        [SerializeField] private Color blockedBackgroundColor = new Color(0.6f, 0.1f, 0.1f, 0.7f);

        private bool isVisible;

        private void Awake()
        {
            SetVisible(false);
        }

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

        private void Update()
        {
            var isBuilding = BuildModeRepository.GetBuildMode() == BuildModeType.Building;
            if (isBuilding != isVisible)
            {
                SetVisible(isBuilding);
                if (isBuilding)
                {
                    HandlePlacedRoomCountChanged(dungeonController != null ? dungeonController.PlacedRoomCount : 0);
                }
            }

            if (isBuilding)
            {
                RefreshPlacementMessage();
            }
        }

        private void SetVisible(bool visible)
        {
            isVisible = visible;
            if (root != null)
            {
                root.SetActive(visible);
            }
        }

        private void HandlePlacedRoomCountChanged(int placedRoomCount)
        {
            if (roomCountText == null || textSettings == null)
            {
                return;
            }

            roomCountText.text = string.Format(textSettings.buildRoomCountFormat, placedRoomCount, dungeonController.MaxPlacedRoomCount);
        }

        private void RefreshPlacementMessage()
        {
            if (dungeonController == null || textSettings == null)
            {
                return;
            }

            var placementResult = dungeonController.PreviewPlacementResult;
            var message = placementResult switch
            {
                DungeonPlacementResult.Placeable => textSettings.buildPlaceableText,
                DungeonPlacementResult.Overlapped => textSettings.buildOverlappedText,
                DungeonPlacementResult.RoomLimitReached => textSettings.buildRoomLimitText,
                _ => textSettings.buildNoNeighborText
            };

            // メッセージが空のときは背景だけが残らないよう、背景ごと隠す
            var hasMessage = !string.IsNullOrEmpty(message);
            if (messageRoot != null)
            {
                messageRoot.SetActive(hasMessage);
            }

            if (!hasMessage)
            {
                return;
            }

            if (messageText != null)
            {
                messageText.text = message;
            }

            if (messageBackground != null)
            {
                messageBackground.color = placementResult == DungeonPlacementResult.Placeable
                    ? placeableBackgroundColor
                    : blockedBackgroundColor;
            }
        }
    }
}
