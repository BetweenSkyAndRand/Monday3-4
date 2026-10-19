using Game.Debug;
using Game.GroundItem;
using Game.InteractObj;
using Game.Inventory;
using Game.Player.Presentation;
using Game.Search;
using Game.UI;
using Game.Utility;
using Game.Weapon;
using Game.Player;
using UnityEngine;
namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤー死亡時のリスポーン処理を統括する。
    /// 即死イベント/通常ダメージ死亡のどちらから呼ばれても、以下を順番に実行する：
    ///   ① 開いているUIを強制的に閉じる（IForceCloseableUi 実装をすべて）
    ///   ② 探索終了処理をそのまま流用する（SearchController.RequestSearchEnd()）
    ///   ③ 初期武器を優先順位に従って解決し、インベントリへ再投入する
    ///   ④ プレイヤーを初期位置へワープさせる
    ///   ⑤ ダウン状態を解除する
    /// </summary>
    public class PlayerRespawnController : MonoBehaviour, IPlayerRespawnHandler
    {
        [Header("プレイヤー参照")]
        [Tooltip("未設定の場合は自身から自動取得する。")]
        [SerializeField] private MonoBehaviour playerMoverObject;
        [Header("探索終了処理（そのまま流用する）")]
        [Tooltip("未設定の場合はシーンから自動検索する。")]
        [SerializeField] private SearchController searchController;
        [Header("インベントリ・装備参照")]
        [SerializeField] private InventoryController inventoryController;
        [SerializeField] private PlayerEquipment playerEquipment;
        [Header("拠点ストレージ参照（初期武器の優先取得元 兼 強制クローズ対象）")]
        [Tooltip("未設定でも動作するが、その場合ストレージ優先ルールとUI強制クローズが機能しない。")]
        [SerializeField] private StorageInventoryController storageInventoryController;
        [Header("ダウン状態管理")]
        [SerializeField] private PlayerDownStateBlocker playerDownStateBlocker;
        [Header("ステータス復旧")]
        [Tooltip("未設定の場合は自身から自動検索する。")]
        [SerializeField] private PlayerStatusController playerStatusController;
        [Header("死亡時に強制的に閉じるUI一覧（IForceCloseableUi実装、storageInventoryController以外の将来拡張用）")]
        [SerializeField] private MonoBehaviour[] closeableUiObjects;
        private IForceCloseableUi[] closeableUis;
        private IPlayerMover playerMover;
        private Vector3 initialPosition;
        private Quaternion initialRotation;
        private bool isRespawning;
        private void Awake()
        {
            if (playerMoverObject == null)
            {
                var playerFreeMover = GetComponent<PlayerFreeMover>();
                if (playerFreeMover != null)
                {
                    playerMoverObject = playerFreeMover;
                }
                else
                {
                    playerMoverObject = GetComponent<RogueMover>();
                }
            }
            playerMover = MonoBehaviourCaster.CastSingle<IPlayerMover>(playerMoverObject, nameof(PlayerRespawnController));
            if (searchController == null)
            {
                searchController = FindFirstObjectByType<SearchController>();
            }
            if (inventoryController == null)
            {
                inventoryController = GetComponentInChildren<InventoryController>();
            }
            if (playerEquipment == null)
            {
                playerEquipment = GetComponentInChildren<PlayerEquipment>();
            }
            if (playerDownStateBlocker == null)
            {
                playerDownStateBlocker = GetComponent<PlayerDownStateBlocker>();
            }
            if (playerStatusController == null)
            {
                playerStatusController = GetComponent<PlayerStatusController>();
            }
            initialPosition = transform.position;
            initialRotation = transform.rotation;
            closeableUis = MonoBehaviourCaster.CastArray<IForceCloseableUi>(closeableUiObjects, nameof(PlayerRespawnController));
        }
        public void HandleDeath()
        {
            if (isRespawning)
            {
                return;
            }

            isRespawning = true;
            try
            {
                RunDeathPreparation();
                Respawn();
            }
            finally
            {
                isRespawning = false;
            }
        }

        public void RunDeathPreparation()
        {
            DropInventoryOnDeath();
            CloseAllOpenUi();
            RunSearchEndProcess();
        }

        public void Respawn()
        {
            ResolveAndApplyInitialWeapon();
            TeleportToInitialPosition();
            RecoverPlayerStatus();
            RecoverFromDownState();
        }
        // 探索終了（崩壊）の前に落とすことで、拠点で死んだときだけ地面に残り、ダンジョン内で落としたものは崩壊で失われる
        private void DropInventoryOnDeath()
        {
            if (!GroundItemRuntime.DropItemsOnDeath || inventoryController == null || playerEquipment == null)
            {
                return;
            }

            var droppedCount = inventoryController.DropAllToGround(transform.position);
            GameLogger.Log(LogCategory.Player, $"[PlayerRespawnController] 死亡時ドロップ: {droppedCount} 件");
        }

        private void CloseAllOpenUi()
        {
            storageInventoryController?.CloseIfOpen();
            if (closeableUis == null)
            {
                return;
            }
            foreach (var closeableUi in closeableUis)
            {
                closeableUi?.CloseIfOpen();
            }
        }
        private void RunSearchEndProcess()
        {
            if (searchController == null)
            {
                GameLogger.LogWarning(LogCategory.Player, "[PlayerRespawnController] searchController が未設定です。探索終了処理をスキップします。", this);
                return;
            }
            searchController.RequestSearchEnd();
        }
        private void ResolveAndApplyInitialWeapon()
        {
            if (inventoryController == null || playerEquipment == null)
            {
                GameLogger.LogWarning(LogCategory.Player, "[PlayerRespawnController] inventoryController または playerEquipment が未設定です。初期武器の復元をスキップします。", this);
                return;
            }
            var initialWeaponData = playerEquipment.InitialWeapon;
            if (initialWeaponData == null)
            {
                GameLogger.LogWarning(LogCategory.Player, "[PlayerRespawnController] PlayerEquipment.InitialWeapon が未設定です。初期武器の復元をスキップします。", this);
                return;
            }
            // 死亡時ドロップが有効なときは、初期武器を持っていない場合のみ再配布する
            var isDropEnabled = GroundItemRuntime.DropItemsOnDeath;
            if (isDropEnabled && inventoryController.HasWeapon(initialWeaponData))
            {
                inventoryController.EnsureInitialWeapon(initialWeaponData);
                return;
            }

            if (storageInventoryController != null &&
                storageInventoryController.TryTakeInitialWeapon(initialWeaponData, out var storedWeaponItem) &&
                storedWeaponItem.HasValue)
            {
                GameLogger.Log(LogCategory.Player, "[PlayerRespawnController] 拠点ストレージから初期武器を回収しました。");
            }
            if (isDropEnabled)
            {
                inventoryController.EnsureInitialWeapon(initialWeaponData);
                return;
            }

            inventoryController.ResetToInitialLoadout(initialWeaponData);
        }
        private void TeleportToInitialPosition()
        {
            if (playerMover != null)
            {
                playerMover.Warp(initialPosition, initialRotation);
                return;
            }
            transform.SetPositionAndRotation(initialPosition, initialRotation);
            GameLogger.LogWarning(LogCategory.Player, "[PlayerRespawnController] playerMover が未設定のため Transform を直接書き換えました。", this);
        }
        private void RecoverFromDownState()
        {
            if (playerDownStateBlocker == null)
            {
                GameLogger.LogWarning(LogCategory.Player, "[PlayerRespawnController] playerDownStateBlocker が未設定です。ダウン状態が解除されません。", this);
                return;
            }
            playerDownStateBlocker.Recover();
        }

        private void RecoverPlayerStatus()
        {
            if (playerStatusController == null)
            {
                GameLogger.LogWarning(LogCategory.Player, "[PlayerRespawnController] playerStatusController が未設定です。HP状態が復旧されません。", this);
                return;
            }

            playerStatusController.RestoreToFullHealth();
        }
    }
}

