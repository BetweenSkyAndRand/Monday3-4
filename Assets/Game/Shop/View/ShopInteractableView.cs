using Game.Debug;
using Game.Interaction;
using Game.Search;
using UnityEngine;

namespace Game.Shop
{
    /// <summary>
    /// 拠点のショップNPC（話しかけてショップを開く）。
    /// 探索中は話しかけられない。プレイヤーが近づけるよう Collider が必要。
    /// </summary>
    public class ShopInteractableView : MonoBehaviour, IInteractableObject
    {
        [SerializeField] private ShopController shopController;
        [SerializeField] private SearchController searchController;

        [Header("UI表示設定")]
        [SerializeField] private InteractTextSettings textSettings;

        [Header("グリッド占有")]
        [Tooltip("true の場合、このオブジェクトの位置のセルを占有してプレイヤーが重ならないようにする。")]
        [SerializeField] private bool occupyGridCell = true;
        [SerializeField] private float cellSize = 1f;

        private void Start()
        {
            if (!occupyGridCell)
            {
                return;
            }

            var gridPosition = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / cellSize),
                Mathf.RoundToInt(transform.position.z / cellSize));
            Game.Grid.GridOccupationManager.Occupy(gridPosition, gameObject);
        }

        private void OnDestroy()
        {
            if (occupyGridCell)
            {
                Game.Grid.GridOccupationManager.ReleaseByOccupant(gameObject);
            }
        }

        public string GetInteractText()
        {
            if (shopController == null || shopController.IsOpen)
            {
                return string.Empty;
            }

            // 探索中はショップを利用できない（searchController 未設定の場合は制限しない）
            if (searchController != null && searchController.IsSearchInProgress)
            {
                return string.Empty;
            }

            return textSettings != null ? textSettings.shopOpenText : "Open Shop";
        }

        public void Interact()
        {
            if (shopController == null)
            {
                GameLogger.LogError(LogCategory.Object, "shopController が設定されていません。", this);
                return;
            }

            if (searchController != null && searchController.IsSearchInProgress)
            {
                return;
            }

            shopController.OpenShop();
        }
    }
}
