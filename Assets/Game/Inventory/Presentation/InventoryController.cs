using Game.Debug;
using Game.Inventory.Parameter;
using Game.Inventory.UseCase;
using Game.Item;
using Game.GroundItem;
using Game.Item.Parameter;
using Game.Player.Script;
using Game.Weapon;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Game.Inventory
{
    /// <summary>
    /// Minecraft スタイルのホットバー型インベントリのコントローラー。
    /// N 個のスロットを持ち、武器を任意のスロットに格納できる。
    /// 1〜N キーまたはマウスクリックで「選択スロット（ActiveSlot）」を切り替え、
    /// 選択中スロットに武器があれば PlayerEquipment 経由で自動装備する。
    /// </summary>
    public class InventoryController : MonoBehaviour, IInventory
    {
        [SerializeField] private InventoryDisplayParameter displayParameter;
        [SerializeField] private InventoryView inventoryView;
        [SerializeField] private GameInputReader gameInputReader;
        [SerializeField] private PlayerEquipment playerEquipment;

        [Tooltip("武器以外のアイテムのアイコン解決に使用する。未設定の場合は displayParameter のアイコン設定のみを使う。")]
        [SerializeField] private ItemDatabase itemDatabase;

        private IInventoryRepository repository;
        private InventoryToggleUseCase toggleUseCase;
        private InventoryBuildViewModelUseCase buildViewModelUseCase;
        private InventoryMutationUseCase mutationUseCase;
        private InventoryEquipUseCase equipUseCase;

        private bool isOpen;
        private float lastToggleTime;
        private bool isSubscribedToInput;
        private bool isInitialWeaponSeeded;
        private readonly PlayerDeathDropUseCase deathDropUseCase = new PlayerDeathDropUseCase();

        /// <summary>
        /// 現在 UI 上で選択中のスロットインデックス（0始まり）。-1 = 未選択。
        /// ActiveSlotIndex（= 装備対象）と一致する。
        /// </summary>
        private int selectedSlotIndex = -1;

        private void Awake()
        {
            if (displayParameter == null)
            {
                GameLogger.LogError(LogCategory.Inventory, "displayParameter が未設定です。Inspector でアサインしてください。", this);
                enabled = false;
                return;
            }

            if (inventoryView == null)
            {
                GameLogger.LogError(LogCategory.Inventory, "inventoryView が未設定です。Inspector でアサインしてください。", this);
                enabled = false;
                return;
            }

            if (gameInputReader == null)
            {
                GameLogger.LogError(LogCategory.Inventory, "gameInputReader が未設定です。Inspector でアサインしてください。", this);
                enabled = false;
                return;
            }

            if (playerEquipment == null)
            {
                GameLogger.LogError(LogCategory.Inventory, "playerEquipment が未設定です。Inspector でアサインしてください。", this);
                enabled = false;
                return;
            }

            repository = new InventoryRepository((int)displayParameter.SlotCount);
            toggleUseCase = new InventoryToggleUseCase();
            buildViewModelUseCase = new InventoryBuildViewModelUseCase();
            mutationUseCase = new InventoryMutationUseCase(repository, displayParameter);
            equipUseCase = new InventoryEquipUseCase(repository);

            selectedSlotIndex = -1;
            InventoryRuntime.SetActiveInventory(this);

            // Minecraft スタイル: 装備専用スロットなしの N 枠で初期化
            inventoryView.Initialize(displayParameter, (int)displayParameter.SlotCount);
            inventoryView.SetSlotClickListener(OnClickSlot);
            inventoryView.SetEquipButtonListener(OnEquipSelectedSlot);
            inventoryView.HideEquipButton();
            inventoryView.SetDiscardButtonListener(OnDiscardSelectedSlot);
            inventoryView.HideDiscardButton();

            // ゲーム開始時はインベントリを開いた状態にする
            isOpen = true;
            RefreshView();
            UpdateDiscardButtonVisibility();
            inventoryView.Show();

            PublishSnapshot();
        }

        private void OnEnable()
        {
            SubscribeInputIfReady();
        }

        private void Start()
        {
            SubscribeInputIfReady();
            TrySeedInitialWeaponToInventory();

            // Start の実行順により PlayerEquipment の初期装備がまだ反映されていない場合へ対応する。
            if (!isInitialWeaponSeeded)
            {
                StartCoroutine(SeedInitialWeaponNextFrame());
            }
        }

        private void Update()
        {
            if (gameInputReader != null)
            {
                return;
            }

            if (Keyboard.current == null)
            {
                return;
            }

            if (!Keyboard.current.tabKey.wasPressedThisFrame)
            {
                return;
            }

            ToggleInventory();
        }

        private void OnDisable()
        {
            UnsubscribeInputIfReady();
        }

        private void OnDestroy()
        {
            InventoryRuntime.ClearActiveInventory(this);
            if (inventoryView != null)
            {
                inventoryView.SetSlotClickListener(null);
            }
        }

        public InventoryUseCaseResult TryAddWeapon(WeaponData weaponData)
        {
            var result = mutationUseCase.TryAddWeapon(weaponData);
            OnInventoryChanged(result);
            return result;
        }

        public InventoryUseCaseResult TryAddItem(InventoryItemSpec itemSpec, uint quantity)
        {
            var result = mutationUseCase.TryAddItem(itemSpec, quantity);
            OnInventoryChanged(result);
            return result;
        }

        public uint GetAddableQuantity(InventoryItemSpec itemSpec)
        {
            return mutationUseCase.GetAddableQuantity(itemSpec);
        }

        public InventoryUseCaseResult TryAddItem(ItemParameter itemParameter, ItemRarity rarity, uint quantity)
        {
            if (itemParameter == null)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "inventory.error.item.null");
            }

            if (!itemParameter.IsRarityEnabled(rarity))
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "inventory.error.rarity.disabled");
            }

            return TryAddItem(InventoryItemSpecFactory.FromItemParameter(itemParameter, rarity), quantity);
        }

        public InventoryUseCaseResult TryRemoveItemAt(int itemSlotIndex)
        {
            var result = mutationUseCase.TryRemoveItemAt(itemSlotIndex);
            OnInventoryChanged(result);
            return result;
        }

        public InventoryUseCaseResult TryRemoveItemAt(int itemSlotIndex, uint quantity)
        {
            var result = mutationUseCase.TryRemoveItemAt(itemSlotIndex, quantity);
            OnInventoryChanged(result);
            return result;
        }

        public InventoryUseCaseResult TryConsumeWeapon(int itemSlotIndex)
        {
            var result = mutationUseCase.TryConsumeWeapon(itemSlotIndex);
            OnInventoryChanged(result);
            return result;
        }

        public InventoryEntity GetCurrentInventory()
        {
            return repository.GetCurrent();
        }

        /// <summary>
        /// 初期武器データ（PlayerEquipment 経由）。
        /// StorageInventoryController が「ストレージ内の武器が初期武器か」を判定する際や、
        /// リスポーン処理が初期武器を解決する際に参照する。
        /// </summary>
        public WeaponData InitialWeapon => playerEquipment != null ? playerEquipment.InitialWeapon : null;

        /// <summary>
        /// 外部からビューの更新を強制する。
        /// ストレージなどの外部システムがインベントリを変更したときに呼び出す。
        /// </summary>
        public void ForceRefreshView()
        {
            RefreshView();
            UpdateDiscardButtonVisibility();
        }

        /// <summary>
        /// プレイヤー死亡時のリスポーン処理から呼ばれる。
        /// 全スロットをクリアし、初期武器を再投入・選択状態にする。
        /// 初期武器の復元優先順位の解決自体は呼び出し元（PlayerRespawnController）の責務とし、
        /// このメソッドは受け取った値をそのまま適用するのみ。
        /// </summary>
        public void ResetToInitialLoadout(WeaponData initialWeaponData)
        {
            if (initialWeaponData == null)
            {
                GameLogger.LogWarning(LogCategory.Inventory, "ResetToInitialLoadout: initialWeaponData が null です。");
                return;
            }

            ClearAllSlots();
            selectedSlotIndex = -1;

            var addResult = TryAddWeapon(initialWeaponData);
            if (!addResult.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Inventory, $"ResetToInitialLoadout: 初期武器の再投入に失敗しました: {addResult.Error}");
                return;
            }

            var weaponSlotIndex = FindWeaponSlotIndex(initialWeaponData);
            if (weaponSlotIndex >= 0)
            {
                SelectSlot(weaponSlotIndex);
            }

            isInitialWeaponSeeded = true;
        }

        /// <summary>全スロットを空にする。ResetToInitialLoadout() から使用する。</summary>
        private void ClearAllSlots()
        {
            var entity = repository.GetCurrent();
            for (var i = 0; i < entity.ItemSlots.Count; i++)
            {
                if (entity.ItemSlots[i].Item.HasValue)
                {
                    repository.TryClearItemSlot(i);
                }
            }

            repository.TryIncrementVersion();
            RefreshView();
            PublishSnapshot();
        }

        /// <inheritdoc/>
        public int GetActiveItemSlotIndex()
        {
            return selectedSlotIndex;
        }

        /// <summary>
        /// 選択中スロットのアイテムをドロップ（スタックごと削除）する。
        /// </summary>
        public InventoryUseCaseResult DropSelectedSlot()
        {
            if (selectedSlotIndex < 0)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "inventory.error.selection.none");
            }

            var entity = repository.GetCurrent();
            if (selectedSlotIndex >= entity.ItemSlots.Count)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.InvalidSlotIndex, "inventory.error.slot.invalid");
            }

            if (!entity.ItemSlots[selectedSlotIndex].Item.HasValue)
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "inventory.error.slot.empty");
            }

            var droppedItem = entity.ItemSlots[selectedSlotIndex].Item.Value;
            var droppedStack = new ItemStack(droppedItem.ItemId, droppedItem.Quantity, droppedItem.Rarity);
            if (!GroundItemRuntime.TryGetController(out var spawnController) || !spawnController.TryDropFromPlayer(droppedStack))
            {
                return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "inventory.error.drop.failed");
            }

            return TryRemoveItemAt(selectedSlotIndex);
        }

        private void OnDropInput()
        {
            var result = DropSelectedSlot();
            if (!result.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Inventory, $"アイテム投棄失敗（ItemDatabase に登録されていない、GroundItemSpawnController がない等）: {result.Error}");
            }
        }

        /// <summary>死亡時ドロップ。初期武器を含むすべてのアイテムを position へ落とす。落とせなかったものは持ったままにする。</summary>
        public int DropAllToGround(Vector3 position)
        {
            if (!GroundItemRuntime.TryGetController(out var spawnController))
            {
                GameLogger.LogWarning(LogCategory.Inventory, "死亡時ドロップ失敗: GroundItemSpawnController がシーンにありません。", this);
                return 0;
            }

            var droppedCount = 0;
            foreach (var deathDropEntry in deathDropUseCase.Execute(repository.GetCurrent()))
            {
                if (!spawnController.TrySpawn(deathDropEntry.ItemStack, position, out _))
                {
                    GameLogger.LogWarning(LogCategory.Inventory, $"死亡時ドロップ失敗（ItemDatabase に未登録等）: {deathDropEntry.ItemStack.ItemId}", this);
                    continue;
                }

                TryRemoveItemAt(deathDropEntry.SlotIndex);
                droppedCount++;
            }

            return droppedCount;
        }

        public bool HasWeapon(WeaponData weaponData)
        {
            return weaponData != null && ContainsWeapon(weaponData);
        }

        /// <summary>初期武器を持っていない場合のみ再投入し、選択状態にする。他のアイテムは変更しない。</summary>
        public void EnsureInitialWeapon(WeaponData initialWeaponData)
        {
            if (initialWeaponData == null)
            {
                return;
            }

            if (!ContainsWeapon(initialWeaponData))
            {
                var addResult = TryAddWeapon(initialWeaponData);
                if (!addResult.IsSuccess)
                {
                    GameLogger.LogWarning(LogCategory.Inventory, $"初期武器の再配布に失敗しました: {addResult.Error}");
                    return;
                }
            }

            var weaponSlotIndex = FindWeaponSlotIndex(initialWeaponData);
            if (weaponSlotIndex >= 0)
            {
                SelectSlot(weaponSlotIndex);
            }

            isInitialWeaponSeeded = true;
        }

        /// <summary>
        /// 「捨てる」ボタンが押されたときに呼ばれる。
        /// 選択中スロットのアイテムを捨てる（インベントリから削除する）。
        /// </summary>
        private void OnDiscardSelectedSlot()
        {
            var result = DropSelectedSlot();
            if (!result.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Inventory, $"アイテム捨て失敗: {result.Error}");
            }
        }

        private void ToggleInventory()
        {            if (Time.unscaledTime - lastToggleTime < displayParameter.OpenCloseCooldown)
            {
                return;
            }

            lastToggleTime = Time.unscaledTime;
            isOpen = toggleUseCase.Execute(isOpen);
            if (isOpen)
            {
                RefreshView();
                UpdateDiscardButtonVisibility();
                inventoryView?.Show();
                return;
            }

            inventoryView?.Hide();
        }

        /// <summary>
        /// 選択中スロットにアイテムがある場合のみ「捨てる」ボタンを表示する。
        /// </summary>
        private void UpdateDiscardButtonVisibility()
        {
            if (inventoryView == null)
            {
                return;
            }
            if (selectedSlotIndex < 0)
            {
                inventoryView.HideDiscardButton();
                return;
            }
            var entity = repository.GetCurrent();
            if (selectedSlotIndex >= entity.ItemSlots.Count)
            {
                inventoryView.HideDiscardButton();
                return;
            }
            if (entity.ItemSlots[selectedSlotIndex].Item.HasValue)
            {
                inventoryView.ShowDiscardButton();
                return;
            }
            inventoryView.HideDiscardButton();
        }
        private void RefreshView()
        {
            if (inventoryView == null)
            {
                return;
            }

            var entity = repository.GetCurrent();
            var viewModels = buildViewModelUseCase.Execute(entity, selectedSlotIndex, displayParameter, ResolveItemIcon);
            inventoryView.ApplyViewModel(viewModels);
        }

        /// <summary>
        /// スロットがクリックされたときに呼ばれる。
        /// Minecraft スタイル: クリック = そのスロットを選択（= 装備）。
        /// </summary>
        private void OnClickSlot(int viewSlotIndex)
        {
            SelectSlot(viewSlotIndex);
        }

        /// <summary>
        /// 1〜N キー入力で対応するスロットを選択する。
        /// slotNumber は 1〜N で、内部インデックスは slotNumber - 1。
        /// </summary>
        private void OnEquipSlotByIndex(int slotNumber)
        {
            SelectSlot(slotNumber - 1);
        }

        /// <summary>
        /// 指定スロットを選択状態にし、武器なら装備、それ以外なら装備解除する。
        /// </summary>
        private void SelectSlot(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= (int)displayParameter.SlotCount)
            {
                return;
            }

            var result = equipUseCase.SelectSlot(slotIndex);
            if (!result.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Inventory, $"スロット選択失敗: {result.Error}");
                return;
            }

            selectedSlotIndex = slotIndex;

            // 選択中スロットのアイテムを装備として反映
            SyncEquippedWeaponToPlayer();
            PublishSnapshot();

            if (isOpen)
            {
                RefreshView();
                UpdateDiscardButtonVisibility();
            }
        }

        /// <summary>
        /// 装備ボタンは Minecraft スタイルでは不要だが、旧 UI 互換のため残してある。
        /// クリックされた場合は何もしない（選択がすでに装備を意味するため）。
        /// </summary>
        private void OnEquipSelectedSlot()
        {
            // 旧 UI 互換: 何もしない（選択 = 装備のため）
        }

        private void OnInventoryChanged(InventoryUseCaseResult result)
        {
            if (!result.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Inventory, $"Inventory op failed: {result.Error} ({result.MessageKey})");
            }

            // 失敗時も同期・更新を行う（パッチ追加の一部成功など、部分的な変更が発生している可能性があるため）
            SyncEquippedWeaponToPlayer();
            PublishSnapshot();

            if (isOpen)
            {
                RefreshView();
                UpdateDiscardButtonVisibility();
            }
        }

        /// <summary>
        /// 選択中スロットのアイテムを PlayerEquipment に反映する。
        /// 武器なら装備、武器以外・空・未選択なら装備解除（null）。
        /// </summary>
        private void SyncEquippedWeaponToPlayer()
        {
            if (playerEquipment == null)
            {
                return;
            }

            var entity = repository.GetCurrent();

            if (selectedSlotIndex < 0 || selectedSlotIndex >= entity.ItemSlots.Count)
            {
                playerEquipment.Equip(null);
                return;
            }

            var selectedSlot = entity.ItemSlots[selectedSlotIndex];
            if (!selectedSlot.Item.HasValue || selectedSlot.Item.Value.ItemType != InventoryItemType.Weapon)
            {
                playerEquipment.Equip(null);
                return;
            }

            var weaponItem = selectedSlot.Item.Value;
            playerEquipment.Equip(weaponItem.WeaponData, weaponItem.EffectiveAttackPower);
        }

        private Sprite ResolveItemIcon(string itemId)
        {
            if (itemDatabase == null)
            {
                return null;
            }

            var itemParameter = itemDatabase.GetItem(itemId);
            return itemParameter != null ? itemParameter.Icon : null;
        }

        private void PublishSnapshot()
        {
            InventoryRuntime.UpdateSnapshot(repository.GetCurrent());
        }

        private void SubscribeInputIfReady()
        {
            if (isSubscribedToInput)
            {
                return;
            }

            if (gameInputReader == null)
            {
                return;
            }

            gameInputReader.OnInventoryToggle += ToggleInventory;
            gameInputReader.OnEquipSlot += OnEquipSlotByIndex;
            gameInputReader.OnDrop += OnDropInput;
            isSubscribedToInput = true;
        }

        private void UnsubscribeInputIfReady()
        {
            if (!isSubscribedToInput)
            {
                return;
            }

            if (gameInputReader == null)
            {
                return;
            }

            gameInputReader.OnInventoryToggle -= ToggleInventory;
            gameInputReader.OnEquipSlot -= OnEquipSlotByIndex;
            gameInputReader.OnDrop -= OnDropInput;
            isSubscribedToInput = false;
        }

        private IEnumerator SeedInitialWeaponNextFrame()
        {
            yield return null;
            TrySeedInitialWeaponToInventory();
        }

        private void TrySeedInitialWeaponToInventory()
        {
            if (isInitialWeaponSeeded)
            {
                return;
            }

            if (playerEquipment == null || playerEquipment.CurrentWeapon == null)
            {
                return;
            }

            var initialWeapon = playerEquipment.CurrentWeapon;
            if (!ContainsWeapon(initialWeapon))
            {
            var addResult = TryAddWeapon(initialWeapon);
            if (!addResult.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Inventory, $"初期武器のインベントリ投入に失敗: {addResult.Error}");
                return;
            }
            }

            var weaponSlotIndex = FindWeaponSlotIndex(initialWeapon);
            if (weaponSlotIndex >= 0)
            {
                SelectSlot(weaponSlotIndex);
            }

            isInitialWeaponSeeded = true;
        }

        private bool ContainsWeapon(WeaponData weaponData)
        {
            var entity = repository.GetCurrent();
            foreach (var slot in entity.ItemSlots)
            {
                if (!slot.Item.HasValue)
                {
                    continue;
                }

                var item = slot.Item.Value;
                if (item.ItemType == InventoryItemType.Weapon && item.WeaponData == weaponData)
                {
                    return true;
                }
            }

            return false;
        }

        private int FindWeaponSlotIndex(WeaponData weaponData)
        {
            var entity = repository.GetCurrent();
            foreach (var slot in entity.ItemSlots)
            {
                if (!slot.Item.HasValue)
                {
                    continue;
                }

                var item = slot.Item.Value;
                if (item.ItemType == InventoryItemType.Weapon && item.WeaponData == weaponData)
                {
                    return slot.SlotIndex;
                }
            }

            return -1;
        }
    }
}
