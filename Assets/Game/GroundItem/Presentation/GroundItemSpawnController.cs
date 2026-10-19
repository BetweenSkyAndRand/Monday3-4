using System;
using System.Collections.Generic;
using Game.Debug;
using Game.Inventory;
using Game.Item;
using Game.Item.Parameter;
using Game.Player.Script;
using UnityEngine;

namespace Game.GroundItem
{
    /// <summary>
    /// 地面アイテムの生成・登録・消滅を管理する。
    /// 位置は自由配置で、グリッド占有には登録しない。
    /// </summary>
    public class GroundItemSpawnController : MonoBehaviour
    {
        private const float FloorRaycastStartHeight = 0.5f;
        private const float FloorRaycastDistance = 3f;

        [SerializeField] private GroundItemParameter groundItemParameter;
        [SerializeField] private ItemDatabase itemDatabase;

        private readonly GroundItemRepository repository = new GroundItemRepository();
        private readonly GroundItemExpirationUseCase expirationUseCase = new GroundItemExpirationUseCase();
        private readonly GroundItemRemovalInBoundsUseCase removalInBoundsUseCase = new GroundItemRemovalInBoundsUseCase();
        private readonly Dictionary<string, GroundItemView> viewsByInstanceId = new Dictionary<string, GroundItemView>();

        private InventoryController inventoryController;
        private PlayerModelRotator playerModelRotator;

        public GroundItemRepository Repository => repository;
        public ItemDatabase ItemDatabase => itemDatabase;

        private void Awake()
        {
            if (groundItemParameter == null || itemDatabase == null)
            {
                GameLogger.LogError(LogCategory.Inventory, "groundItemParameter / itemDatabase が未設定です。Inspector でアサインしてください。", this);
                enabled = false;
                return;
            }

            GroundItemRuntime.Register(this);
            GroundItemRuntime.DropItemsOnDeath = groundItemParameter.DropItemsOnDeath;
        }

        private void OnDestroy()
        {
            GroundItemRuntime.Unregister(this);
        }

        private void Update()
        {
            var expiredInstanceIds = expirationUseCase.Execute(repository.GetAll(), Time.time, groundItemParameter.DespawnSeconds);
            foreach (var expiredInstanceId in expiredInstanceIds)
            {
                Remove(expiredInstanceId);
            }
        }

        /// <summary>プレイヤーの向いている方向の前方（壁があれば手前）へ投棄する。</summary>
        public bool TryDropFromPlayer(ItemStack itemStack)
        {
            if (playerModelRotator == null)
            {
                playerModelRotator = FindFirstObjectByType<PlayerModelRotator>();
            }

            if (playerModelRotator == null)
            {
                GameLogger.LogWarning(LogCategory.Inventory, "投棄失敗: PlayerModelRotator がシーンにありません。", this);
                return false;
            }

            var playerPosition = playerModelRotator.transform.position;
            var facingDirection = playerModelRotator.VisualModel != null ? playerModelRotator.VisualModel.forward : playerModelRotator.transform.forward;
            facingDirection.y = 0f;
            facingDirection = facingDirection.sqrMagnitude < 0.0001f ? playerModelRotator.transform.forward : facingDirection.normalized;

            var dropPosition = ClampToReachable(playerPosition, playerPosition + facingDirection * groundItemParameter.PlayerDropDistance);
            return TrySpawn(itemStack, dropPosition, out _);
        }

        public bool TrySpawn(ItemStack itemStack, Vector3 dropOrigin, out string instanceId)
        {
            instanceId = null;
            if (!itemStack.IsValid)
            {
                return false;
            }

            var itemParameter = itemDatabase.GetItem(itemStack.ItemId);
            if (itemParameter == null)
            {
                GameLogger.LogWarning(LogCategory.Inventory, $"地面ドロップ失敗: ItemDatabase に存在しない ItemId です: {itemStack.ItemId}", this);
                return false;
            }

            if (!itemParameter.IsRarityEnabled(itemStack.Rarity))
            {
                GameLogger.LogWarning(LogCategory.Inventory, $"地面ドロップ失敗: {itemStack.ItemId} のレア度 {itemStack.Rarity} は無効です。", this);
                return false;
            }

            var worldPosition = ResolveDropPosition(dropOrigin);
            instanceId = Guid.NewGuid().ToString("N");

            var viewObject = new GameObject($"GroundItem_{itemParameter.ItemName}");
            viewObject.transform.SetParent(transform, false);
            viewObject.transform.position = worldPosition;

            if (itemParameter.WorldPrefab != null)
            {
                Instantiate(itemParameter.WorldPrefab, viewObject.transform);
            }
            else
            {
                GameLogger.LogWarning(LogCategory.Inventory, $"{itemParameter.ItemName} の worldPrefab が未設定です。見た目なしで生成します。", this);
            }

            var view = viewObject.AddComponent<GroundItemView>();
            view.Initialize(instanceId, itemStack.ItemId, itemStack.Quantity, itemStack.Rarity, itemParameter.ItemName, itemParameter.Icon);

            // PlayerInteract が OverlapSphere で拾えるよう、判定用のトリガーを付ける（移動は妨げない）
            var pickupCollider = viewObject.AddComponent<SphereCollider>();
            pickupCollider.isTrigger = true;
            pickupCollider.radius = groundItemParameter.PickupRadius;

            repository.Save(new GroundItemEntity(instanceId, itemStack, worldPosition, Time.time));
            viewsByInstanceId[instanceId] = view;
            return true;
        }

        /// <summary>地面アイテムをインベントリへ移す。全量を入れられない場合は何も変えず false を返す。</summary>
        public bool TryPickup(string instanceId)
        {
            if (!repository.TryGet(instanceId, out var groundItem))
            {
                return false;
            }

            if (inventoryController == null)
            {
                inventoryController = FindFirstObjectByType<InventoryController>();
            }

            if (inventoryController == null)
            {
                GameLogger.LogWarning(LogCategory.Inventory, "拾得失敗: InventoryController がシーンにありません。", this);
                return false;
            }

            var itemParameter = itemDatabase.GetItem(groundItem.ItemStack.ItemId);
            var addResult = inventoryController.TryAddItem(itemParameter, groundItem.ItemStack.Rarity, groundItem.ItemStack.Quantity);
            if (!addResult.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Inventory, $"拾得失敗（インベントリに空きがない等）: {groundItem.ItemStack.ItemId}", this);
                return false;
            }

            return Remove(instanceId);
        }

        public bool Remove(string instanceId)
        {
            if (!repository.Remove(instanceId))
            {
                return false;
            }

            if (viewsByInstanceId.TryGetValue(instanceId, out var view))
            {
                viewsByInstanceId.Remove(instanceId);
                if (view != null)
                {
                    Destroy(view.gameObject);
                }
            }

            return true;
        }

        /// <summary>範囲内の地面アイテムをすべて削除する（崩壊用）。削除した件数を返す。</summary>
        public int RemoveInBounds(System.Func<Vector3, bool> isInsideBounds)
        {
            var removalInstanceIds = removalInBoundsUseCase.Execute(repository.GetAll(), isInsideBounds);
            foreach (var instanceId in removalInstanceIds)
            {
                Remove(instanceId);
            }

            return removalInstanceIds.Count;
        }

        private Vector3 ResolveDropPosition(Vector3 dropOrigin)
        {
            var scatterOffset = UnityEngine.Random.insideUnitCircle * groundItemParameter.ScatterRadius;
            var scatteredPosition = dropOrigin + new Vector3(scatterOffset.x, 0f, scatterOffset.y);

            // ばらつきで壁の向こう側へ出ないよう、遮るものがあれば散らさない
            if (IsObstructed(dropOrigin, scatteredPosition))
            {
                scatteredPosition = dropOrigin;
            }

            // 天井に当たらないよう、レイは指定位置の少し上（プレイヤーの足元付近）から真下へ飛ばす
            var rayOrigin = scatteredPosition + Vector3.up * FloorRaycastStartHeight;
            if (Physics.Raycast(rayOrigin, Vector3.down, out var hit, FloorRaycastDistance, groundItemParameter.GroundLayerMask, QueryTriggerInteraction.Ignore))
            {
                scatteredPosition.y = hit.point.y;
            }

            scatteredPosition.y += groundItemParameter.DropHeightOffset;
            return scatteredPosition;
        }

        private bool IsObstructed(Vector3 fromPosition, Vector3 toPosition)
        {
            var checkHeight = Vector3.up * FloorRaycastStartHeight;
            return Physics.Linecast(fromPosition + checkHeight, toPosition + checkHeight, groundItemParameter.ObstacleLayerMask, QueryTriggerInteraction.Ignore);
        }

        /// <summary>プレイヤー位置から目標位置までの間に壁などがあれば手前に補正し、さらに壁の内部に重なっていれば重ならなくなるまで引き戻して返す。</summary>
        public Vector3 ClampToReachable(Vector3 originPosition, Vector3 targetPosition)
        {
            var checkHeight = Vector3.up * FloorRaycastStartHeight;
            var obstacleMask = groundItemParameter.ObstacleLayerMask;
            var resolvedPosition = targetPosition;

            if (Physics.Linecast(originPosition + checkHeight, targetPosition + checkHeight, out var hit, obstacleMask, QueryTriggerInteraction.Ignore))
            {
                var toTarget = targetPosition - originPosition;
                toTarget.y = 0f;
                resolvedPosition = hit.point - toTarget.normalized * groundItemParameter.WallMargin;
                resolvedPosition.y = originPosition.y;
                UnityEngine.Debug.Log($"[GroundItem診断] 壁に当たったため手前へ補正: {hit.collider.name}", this);
            }

            // レイが壁に届かない角度や、壁の厚みの内側に入る場合に備え、球で重なりを確認して引き戻す
            const int pullBackStepCount = 15;
            var pullBackStep = (originPosition - resolvedPosition) / pullBackStepCount;
            for (var stepIndex = 0; stepIndex < pullBackStepCount; stepIndex++)
            {
                if (!Physics.CheckSphere(resolvedPosition + checkHeight, groundItemParameter.WallMargin, obstacleMask, QueryTriggerInteraction.Ignore))
                {
                    break;
                }

                resolvedPosition += pullBackStep;
            }

            LogOverlappedColliders(originPosition, resolvedPosition);
            return resolvedPosition;
        }

        // 原因調査用：最終位置の周囲にあるコライダーを全レイヤーで列挙する（壁に埋まる問題が解決したら削除する）
        private void LogOverlappedColliders(Vector3 originPosition, Vector3 resolvedPosition)
        {
            var overlappedColliders = Physics.OverlapSphere(resolvedPosition + Vector3.up * FloorRaycastStartHeight, 0.6f, ~0, QueryTriggerInteraction.Collide);
            var overlapNames = new List<string>();
            foreach (var overlappedCollider in overlappedColliders)
            {
                overlapNames.Add($"{overlappedCollider.name}(layer={overlappedCollider.gameObject.layer},trigger={overlappedCollider.isTrigger})");
            }

            UnityEngine.Debug.Log($"[GroundItem診断] origin={originPosition} resolved={resolvedPosition} 周囲のコライダー=[{string.Join(", ", overlapNames)}]", this);
        }
    }
}