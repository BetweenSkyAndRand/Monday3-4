using Game.Debug;
using Game.Interaction;
using Game.Player.Script;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.InteractObj
{
    /// <summary>
    /// 拠点ストレージボックスのインタラクタブル。
    /// Plan.md の「拠点のストレージ」に対応する（青色モデルを使用）。
    ///
    /// 動作:
    ///   - インタラクト → StorageInventoryController の UI を開く（トグル動作）
    ///   - アイテムの保管・引き出しは StorageInventoryController の UI ボタンで行う
    ///   - プレイヤーが一定距離（cellSize × autoCloseCellDistance）離れたら UI を自動で閉じる
    ///
    /// ストレージのロジックはすべて StorageInventoryController に委譲している。
    /// </summary>
    public class StorageBoxController : MonoBehaviour, IInteractableObject
    {
    [Header("ストレージ連携")]
    [Tooltip("同じ GameObject か子 GameObject にアタッチされた StorageInventoryController を設定する。")]
    [SerializeField] private StorageInventoryController storageInventoryController;

    [Header("UI表示設定")]
    [Tooltip("インタラクトテキスト設定 ScriptableObject")]
    [SerializeField] private InteractTextSettings textSettings;

    [Header("自動クローズ設定")]
    [Tooltip("1セルのワールドサイズ。RogueMover の cellSize と合わせる。")]
    [SerializeField] private float cellSize = 1f;

    [Tooltip("プレイヤーがこの距離（セル単位）以上離れたら UI を自動で閉じる。")]
    [SerializeField] private float autoCloseCellDistance = 2f;

    /// <summary>プレイヤー Transform のキャッシュ。初回 Update で取得する。</summary>
    private Transform playerTransform;

        private void Awake()
        {
            if (storageInventoryController == null)
            {
                // 同オブジェクト内を自動検索
                storageInventoryController = GetComponentInChildren<StorageInventoryController>();
            }

            if (storageInventoryController == null)
            {
                GameLogger.LogWarning(LogCategory.Object, "StorageInventoryController が見つかりません。Inspector で設定してください。", this);
            }
        }


        private void Update()
        {
            // UI が開いていないときは距離判定不要
            if (storageInventoryController == null || !storageInventoryController.IsOpen)
            {
                return;
            }

            if (playerTransform == null)
            {
                var playerInteract = FindFirstObjectByType<PlayerInteract>();
                if (playerInteract == null)
                {
                    return;
                }

                playerTransform = playerInteract.transform;
            }

            // XZ 平面距離で判定する（高さ違いを無視する）
            var dx = playerTransform.position.x - transform.position.x;
            var dz = playerTransform.position.z - transform.position.z;
            var sqDistance = dx * dx + dz * dz;
            var threshold = cellSize * autoCloseCellDistance;
            if (sqDistance > threshold * threshold)
            {
                storageInventoryController.CloseStorage();
            }
        }

        // ─── IInteractableObject ──────────────────────────────────────

    public string GetInteractText()
    {
        if (storageInventoryController == null)
        {
            return string.Empty;
        }

        if (textSettings == null)
        {
            return storageInventoryController.IsOpen ? "Close Storage" : "Open Storage";
        }

        return storageInventoryController.IsOpen ? textSettings.storageCloseText : textSettings.storageOpenText;
    }

        public void Interact()
        {
            if (storageInventoryController == null)
            {
                GameLogger.LogWarning(LogCategory.Object, "StorageInventoryController が未設定です。", this);
                return;
            }

            storageInventoryController.OpenStorage();
        }
    }
}
