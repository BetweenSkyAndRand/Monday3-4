using Game.Interaction;
using Game.Inventory;
using Game.Inventory.Parameter;
using Game.Weapon;
using System;
using UnityEngine;
// ReSharper disable once CheckNamespace
namespace Game.Treasure
{
    /// <summary>
    /// スポーン済み宝箱の振る舞いを管理する。
    ///
    /// UI はシーンに配置された TreasureChestInventoryController（シングルトン）に委譲する。
    /// このコンポーネント自体は UI フィールドを持たないため、プレハブのままシーン参照なしで動作する。
    ///
    /// 操作フロー:
    ///   1. インタラクト → TreasureChestInventoryController.Instance.OpenWith(this) を呼ぶ
    ///   2. UIから TakeFromChestSlot / DiscardChestSlot を呼んでもらう
    ///   3. 全スロットが空になると Destroy される
    /// </summary>
    public class TreasureBoxController : MonoBehaviour, IInteractableObject
    {
        [SerializeField] private float cellSize = 1f;
        [Header("UI表示設定")]
        [Tooltip("インタラクトテキスト設定 ScriptableObject（Project アセット）")]
        [SerializeField] private InteractTextSettings textSettings;
        [Header("開封演出")]
        [SerializeField] private GameObject openVfxPrefab;
        [SerializeField] private AudioClip openSe;
        [SerializeField] private string openSeId = "";
        [SerializeField] private Animator animator;
        [SerializeField] private string openTriggerName = "Open";
        [SerializeField] private bool enableOpenEffect = true;
        [Header("自動クローズ設定")]
        [Tooltip("プレイヤーがこの距離（セル単位）以上離れたら UI を自動で閉じる。")]
        [SerializeField] private float autoCloseCellDistance = 2f;
        /// <summary>プレイヤー Transform のキャッシュ。初回 Update で取得する。</summary>
        private Transform playerTransform;
        /// <summary>スポーン時に確定した中身。Initialize() が呼ばれるまで null。</summary>
        private TreasureContents contents;
        /// <summary>宝箱の中身を管理する内部リポジトリ（1スロット）。</summary>
        private IInventoryRepository chestRepository;
        /// <summary>UIが開いているか（GetInteractText のテキスト切り替えに使用）。</summary>
        private bool isChestOpen;
        private void Start()
        {
            var gridPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / cellSize),
                Mathf.RoundToInt(transform.position.z / cellSize)
            );
            Grid.GridOccupationManager.Occupy(gridPos, gameObject);
        }
        private void OnDestroy()
        {
            Grid.GridOccupationManager.ReleaseByOccupant(gameObject);
        }
        private void Update()
        {
            // この宝箱が UI に開かれているときのみ距離判定を行う
            var uiController = TreasureChestInventoryController.Instance;
            if (uiController == null || !isChestOpen || uiController.CurrentChest != this)
            {
                return;
            }
            if (playerTransform == null)
            {
                var playerInteract = FindFirstObjectByType<Player.Script.PlayerInteract>();
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
                uiController.Close();
            }
        }
        // ─── 初期化 ───────────────────────────────────────────────────
        /// <summary>
        /// スポーン直後に RoomObjectSpawner から呼ばれる。
        /// 中身を内部リポジトリに格納する。
        /// </summary>
        public void Initialize(TreasureContents treasureContents)
        {
            contents = treasureContents;
            chestRepository = new InventoryRepository(1);
            var initialItem = BuildChestItem(contents);
            if (initialItem.HasValue)
            {
                chestRepository.TrySetItemSlot(0, initialItem.Value);
                chestRepository.TryIncrementVersion();
            }
        }
        // ─── IInteractableObject ──────────────────────────────────────
        public string GetInteractText()
        {
            if (contents == null)
            {
                return string.Empty;
            }
            if (isChestOpen)
            {
                return textSettings != null ? textSettings.treasureBoxCloseText : "Close Chest";
            }
            return textSettings != null ? textSettings.treasureBoxGeneric : "Open Chest";
        }
        /// <summary>
        /// プレイヤーのインタラクト。
        /// TreasureChestInventoryController がシーンに存在すれば UIを開く。
        /// 存在しない場合は直接ピックアップ（フォールバック）。
        /// </summary>
        public void Interact()
        {
            if (contents == null)
            {
                UnityEngine.Debug.LogWarning("[TreasureBoxController] Not initialized. Call Initialize() first.");
                return;
            }
            var uiController = TreasureChestInventoryController.Instance;
            if (uiController == null)
            {
                // TreasureChestInventoryController がシーンにない場合のフォールバック
                HandleLegacyDirectPickup();
                return;
            }
            // 同じ宝箱が開いていればトグルで閉じる、別の or 未開放なら開く
            if (isChestOpen && uiController.CurrentChest == this)
            {
                uiController.Close();
            }
            else
            {
                if (enableOpenEffect && !isChestOpen)
                {
                    PlayOpenEffect();
                }
                uiController.OpenWith(this);
            }
        }
        // ─── UI コントローラーから呼ばれる公開メソッド ───────────────
        /// <summary>現在の宝箱内容のスナップショットを返す。リポジトリ未初期化なら null。</summary>
        public InventoryEntity? GetChestContents()
        {
            if (chestRepository == null)
            {
                return null;
            }
            return chestRepository.GetCurrent();
        }
        /// <summary>指定スロットを空にする。</summary>
        public bool TryClearChestSlot(int slotIndex)
        {
            if (chestRepository == null)
            {
                return false;
            }
            var result = chestRepository.TryClearItemSlot(slotIndex);
            if (result.IsSuccess)
            {
                chestRepository.TryIncrementVersion();
            }
            return result.IsSuccess;
        }
        /// <summary>宝箱が空かどうかを返す。</summary>
        public bool IsEmpty()
        {
            if (chestRepository == null)
            {
                return true;
            }
            var entity = chestRepository.GetCurrent();
            foreach (var slot in entity.ItemSlots)
            {
                if (slot.Item.HasValue)
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>空になった宝箱を Destroy する。UIコントローラーから呼ばれる。</summary>
        public void DestroyIfEmpty()
        {
            if (IsEmpty())
            {
                Destroy(gameObject);
            }
        }
        /// <summary>UIが開かれたことを通知する（テキスト切り替え用）。</summary>
        public void NotifyOpened() => isChestOpen = true;
        /// <summary>UIが閉じられたことを通知する（テキスト切り替え用）。</summary>
        public void NotifyClosed() => isChestOpen = false;
        // ─── 内部ユーティリティ ───────────────────────────────────────
        private static InventoryItemEntity? BuildChestItem(TreasureContents treasureContents)
        {
            if (treasureContents == null)
            {
                return null;
            }
            if (treasureContents.ContentsType == TreasureContentsType.Weapon)
            {
                if (treasureContents.WeaponData == null)
                {
                    return null;
                }
                var weaponItemId = treasureContents.WeaponData.ItemId;
                return new InventoryItemEntity(
                    Guid.NewGuid().ToString("N"),
                    weaponItemId, treasureContents.WeaponData.WeaponName, weaponItemId,
                    InventoryItemType.Weapon, 1,
                    treasureContents.WeaponData,
                    Game.Item.ItemRarity.N,
                    treasureContents.WeaponData.DefaultAttackPower);
            }
            return null;
        }
        /// <summary>
        /// TreasureChestInventoryController がシーンにない場合の直接ピックアップ動作。
        /// </summary>
        private void HandleLegacyDirectPickup()
        {
            if (chestRepository == null)
            {
                return;
            }
            if (!InventoryRuntime.TryGetInventory(out var inventory))
            {
                UnityEngine.Debug.LogWarning("[TreasureBoxController] IInventory not found.");
                return;
            }
            var entity = chestRepository.GetCurrent();
            for (var i = 0; i < entity.ItemSlots.Count; i++)
            {
                if (!entity.ItemSlots[i].Item.HasValue)
                {
                    continue;
                }
                var item = entity.ItemSlots[i].Item.Value;
                if (item.ItemType != InventoryItemType.Weapon || item.WeaponData == null)
                {
                    continue;
                }
                var addResult = inventory.TryAddItem(InventoryItemSpecFactory.FromWeaponEntity(item), 1);
                if (!addResult.IsSuccess)
                {
                    UnityEngine.Debug.LogWarning($"[TreasureBoxController] Pickup failed: {addResult.Error}");
                    return;
                }
                chestRepository.TryClearItemSlot(i);
                chestRepository.TryIncrementVersion();
            }
            DestroyIfEmpty();
        }
        private void PlayOpenEffect()
        {
            if (animator != null && !string.IsNullOrEmpty(openTriggerName))
            {
                animator.SetTrigger(openTriggerName);
            }
            if (openVfxPrefab != null)
            {
                var vfxManager = Effect.VFXManager.Instance;
                if (vfxManager != null)
                {
                    vfxManager.PlayEffect(openVfxPrefab, transform.position, Quaternion.identity);
                }
                else
                {
                    Instantiate(openVfxPrefab, transform.position, Quaternion.identity);
                }
            }
            if (!string.IsNullOrEmpty(openSeId) || openSe != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    if (!string.IsNullOrEmpty(openSeId))
                    {
                        audioManager.PlayConfiguredSe(openSeId);
                    }
                    else
                    {
                        audioManager.PlaySe(openSe);
                    }
                }
                else if (openSe != null)
                {
                    AudioSource.PlayClipAtPoint(openSe, transform.position);
                }
            }
        }
    }
}