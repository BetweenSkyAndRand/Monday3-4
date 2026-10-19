using System.Collections;
using System.Collections.Generic;
using Game.Debug;
using Game.Inventory;
using Game.Inventory.Parameter;
using Game.Inventory.UseCase;
using Game.UI;
using Game.Weapon;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace Game.InteractObj
{
    /// <summary>
    /// 拠点ストレージのインベントリUI・ロジックコントローラー。
    /// Plan.md の「拠点のストレージ」に対応する（2スロット）。
    ///
    /// 操作方法:
    ///   - StorageBox にインタラクト → ストレージUI が開く
    ///   - ストレージのスロット（ボタン）をクリック → そのスロットを選択状態にする（再クリックで選択解除）
    ///   - 選択後に以下のボタンで操作:
    ///       「取り出す」 → 選択スロットのアイテムをプレイヤーへ移動
    ///       「預ける」   → プレイヤーのアクティブスロット（1〜4キー）のアイテムを選択スロットへ移動
    ///       「捨てる」   → 選択スロットのアイテムを削除
       ///   - 「閉じる」ボタンまたは再インタラクトで UI を閉じる
    ///
    /// StorageBoxController からインタラクト時に OpenStorage() を呼ばれて開かれる。
    /// 装備スロットはなく、武器をアイテムスロット（2枠）に保管する。
    /// </summary>
    public class StorageInventoryController : MonoBehaviour, IForceCloseableUi
    {
        [Header("ストレージ設定")]
        [Tooltip("ストレージ専用の InventoryDisplayParameter。slotCount=2 に設定すること。")]
        [SerializeField] private InventoryDisplayParameter storageDisplayParameter;

        [Header("ストレージUI")]
        [Tooltip("ストレージ専用の InventoryView。StorageCanvas 配下の Panel にアタッチする。")]
        [SerializeField] private InventoryView storageInventoryView;

        [Header("操作ボタン")]
        [Tooltip("ストレージUIを閉じるボタン。")]
        [SerializeField] private Button closeButton;

        [Tooltip("選択中のストレージスロットのアイテムをプレイヤーへ取り出すボタン。")]
        [SerializeField] private Button withdrawButton;

        [Tooltip("プレイヤーのアクティブスロットのアイテムを選択中のストレージスロットへ預けるボタン。")]
        [SerializeField] private Button depositButton;

        [Tooltip("選択中のストレージスロットのアイテムを捨てるボタン。")]
        [SerializeField] private Button discardButton;

        [Header("プレイヤーインベントリ参照")]
        [Tooltip("プレイヤーの InventoryController。Inspector で直接アサインする。")]
        [SerializeField] private InventoryController playerInventoryController;

        private IInventoryRepository storageRepository;
        private InventoryMutationUseCase mutationUseCase;
        private bool isOpen;

        /// <summary>ストレージUIで最後にクリックされたスロットのインデックス（-1=未選択）。捨てる操作の対象スロット。</summary>
        private int selectedStorageSlotIndex = -1;

        /// <summary>現在ストレージUIが開いているか。</summary>
        public bool IsOpen => isOpen;

        private void Awake()
        {
            if (storageDisplayParameter == null)
            {
                GameLogger.LogError(LogCategory.Object, "storageDisplayParameter が未設定です。Inspector でアサインしてください。", this);
                enabled = false;
                return;
            }

            if (playerInventoryController == null)
            {
                GameLogger.LogError(LogCategory.Object, "playerInventoryController が未設定です。Inspector でアサインしてください。", this);
                enabled = false;
                return;
            }

            // 2スロットのリポジトリを初期化（装備スロットは使わない）
            storageRepository = new InventoryRepository((int)storageDisplayParameter.SlotCount);
            mutationUseCase = new InventoryMutationUseCase(storageRepository, storageDisplayParameter);

            if (storageInventoryView != null)
            {
                // 宝箱と同じ初期化順序：先に Hide してパネルを非アクティブにしてからスロットを生成する。
                // この順序を逆にすると（先に Initialize して後で Hide）、スロットがアクティブ状態で生成され
                // 即座に非アクティブ化されることで Canvas/Image の内部状態が中途半端になり、
                // 次回 Show 時にアイコンが描画されず、スロットをクリックするまで見えない不具合が発生する。
                storageInventoryView.Hide();
                storageInventoryView.Initialize(storageDisplayParameter, (int)storageDisplayParameter.SlotCount);
                storageInventoryView.SetSlotClickListener(OnClickStorageSlot);
            }

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseStorage);
            }

            if (withdrawButton != null)
            {
                withdrawButton.onClick.AddListener(OnWithdrawFromSelectedStorageSlot);
                withdrawButton.gameObject.SetActive(false);
            }

            if (depositButton != null)
            {
                depositButton.onClick.AddListener(OnDepositToSelectedStorageSlot);
                depositButton.gameObject.SetActive(false);
            }

            if (discardButton != null)
            {
                discardButton.onClick.AddListener(OnDiscardSelectedStorageSlot);
                discardButton.gameObject.SetActive(false);
            }

        }

        // ─── 公開API ──────────────────────────────────────────────────

        /// <summary>
        /// ストレージUIを開く。すでに開いている場合は閉じる（トグル動作）。
        /// StorageBoxController の Interact() から呼ばれる。
        /// </summary>
        public void OpenStorage()
        {
            if (isOpen)
            {
                CloseStorage();
                return;
            }

            isOpen = true;
            // パネルを先にアクティブ化してから ViewModel を適用する。
            // Hide() で非アクティブになっていた Image / Canvas は、
            // 非アクティブのまま sprite を更新しても次回 SetActive(true) 時に
            // 1 フレーム描画が遅れることがある（アイコン欠落バグ）。
            // 先に Show() してから RefreshAllViews() で sprite を流し込むと、
            // 即座に正しいアイコンが描画される。
            storageInventoryView?.Show();
            RefreshAllViews();
            UpdateActionButtonsVisibility();

            // 一部の環境では SetActive(true) と同フレーム内の sprite 設定が
            // Canvas に取りこぼされ、UI を一度クリックするまでアイコンが見えない問題が発生する。
            // 次フレームに再度 ApplyViewModel を呼ぶことで、確実に描画を反映させる保険を入れる。
            StartCoroutine(ReapplyViewNextFrame());
        }

        /// <summary>
        /// Show() の次フレームで RefreshAllViews() を呼び直すコルーチン。
        /// 初回 SetActive(true) 直後の Canvas 描画タイミングずれによる
        /// アイコン未表示問題への保険。
        /// </summary>
        private IEnumerator ReapplyViewNextFrame()
        {
            yield return null;
            if (!isOpen)
            {
                yield break;
            }

            RefreshAllViews();
            UpdateActionButtonsVisibility();
        }

        /// <summary>ストレージUIを閉じる。</summary>
        public void CloseStorage()
        {
            isOpen = false;
            selectedStorageSlotIndex = -1;
            UpdateActionButtonsVisibility();
            storageInventoryView?.Hide();
        }

        // ─── IForceCloseableUi ────────────────────────────────────────

        /// <summary>
        /// UIが開いていれば閉じる（IForceCloseableUi の実装）。
        /// プレイヤー死亡時のリスポーン処理から、開きっぱなしのストレージUIを強制的に閉じるために呼ばれる。
        /// </summary>
        public void CloseIfOpen()
        {
            if (isOpen)
            {
                CloseStorage();
            }
        }

        // ─── 初期武器の取り出し（リスポーン用） ─────────────────────────

        /// <summary>
        /// ストレージ内から指定した初期武器データに一致する武器を検索し、
        /// 見つかればスロットから取り出して返す。
        /// プレイヤー死亡時のリスポーン処理（PlayerRespawnController）から呼ばれる。
        /// </summary>
        /// <returns>取り出せた場合は true。</returns>
        public bool TryTakeInitialWeapon(WeaponData initialWeaponData, out InventoryItemEntity? weaponItem)
        {
            weaponItem = null;
            if (initialWeaponData == null || storageRepository == null)
            {
                return false;
            }

            var entity = storageRepository.GetCurrent();
            for (var i = 0; i < entity.ItemSlots.Count; i++)
            {
                var slotItem = entity.ItemSlots[i].Item;
                if (!slotItem.HasValue)
                {
                    continue;
                }

                var item = slotItem.Value;
                if (item.ItemType != InventoryItemType.Weapon || item.WeaponData != initialWeaponData)
                {
                    continue;
                }

                weaponItem = item;
                storageRepository.TryClearItemSlot(i);
                storageRepository.TryIncrementVersion();

                if (selectedStorageSlotIndex == i)
                {
                    selectedStorageSlotIndex = -1;
                }

                RefreshAllViews();
                UpdateActionButtonsVisibility();

                GameLogger.Log(LogCategory.Object, $"[{item.DisplayName}] をリスポーン処理のためストレージから取り出しました。");
                return true;
            }

            return false;
        }

        // ─── スロットクリック（選択のみ） ────────────────────────────

        /// <summary>
        /// ストレージのスロット（ボタン）がクリックされたときに呼ばれる。
        ///
        /// 動作:
        ///   同じスロットを再クリック → 選択解除
        ///   別のスロットをクリック   → そのスロットを選択状態にする
        ///
        /// 預ける・取り出す操作は選択後に対応するボタンから行う。
        /// 誤操作（意図しない変換・破棄）を防ぐため、クリック即実行を廃止した。
        /// </summary>
        private void OnClickStorageSlot(int storageSlotViewIndex)
        {
            // 同じスロットを再クリックで選択解除、別スロットで選択切替
            selectedStorageSlotIndex = selectedStorageSlotIndex == storageSlotViewIndex ? -1 : storageSlotViewIndex;

            RefreshView();
            UpdateActionButtonsVisibility();
        }

        // ─── 預ける・引き出すロジック ────────────────────────────────

        /// <summary>
        /// プレイヤーのアクティブスロットにあるアイテムを全部ストレージの指定スロットへ預ける。
        /// ストレージ側のスロットが使用中の場合はキャンセルする。
        /// </summary>
        private void DepositFromPlayerSlotToStorage(
            IInventory playerInventory,
            int playerSlotIndex,
            int storageSlotIndex,
            InventoryItemEntity itemEntity)
        {
            var storageEntity = storageRepository.GetCurrent();
            if (storageSlotIndex >= storageEntity.ItemSlots.Count)
            {
                return;
            }

            // ストレージの対象スロットが使用中ならキャンセル
            if (storageEntity.ItemSlots[storageSlotIndex].Item.HasValue)
            {
                GameLogger.Log(LogCategory.Object, "ストレージのそのスロットは使用中です。別のスロットか別のストレージスロットを選択してください。");
                return;
            }

            // ストレージスロットにアイテムをセット
            var setResult = storageRepository.TrySetItemSlot(storageSlotIndex, itemEntity);
            if (!setResult.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Object, $"ストレージへの書き込みに失敗しました: {setResult.Error}");
                return;
            }

            storageRepository.TryIncrementVersion();

            // プレイヤーのスロットを消費（失敗した場合はロールバック）
            var consumeSucceeded = TryConsumeFromPlayerSlot(playerInventory, playerSlotIndex, itemEntity);
            if (!consumeSucceeded)
            {
                storageRepository.TryClearItemSlot(storageSlotIndex);
                storageRepository.TryIncrementVersion();
                return;
            }

            GameLogger.Log(LogCategory.Object, $"[{itemEntity.DisplayName}] をストレージ スロット{storageSlotIndex}へ預けました。");
        }

        /// <summary>
        /// ストレージの指定スロットにあるアイテムをプレイヤーのインベントリへ引き出す。
        /// プレイヤーのインベントリに空きがない場合はキャンセルする。
        /// </summary>
        private void WithdrawFromStorageSlotToPlayer(IInventory playerInventory, int storageSlotIndex)
        {
            var storageEntity = storageRepository.GetCurrent();
            if (storageSlotIndex >= storageEntity.ItemSlots.Count)
            {
                return;
            }

            var storageSlot = storageEntity.ItemSlots[storageSlotIndex];
            if (!storageSlot.Item.HasValue)
            {
                GameLogger.Log(LogCategory.Object, "そのストレージスロットは空です。");
                return;
            }

            var itemEntity = storageSlot.Item.Value;

            // プレイヤーのインベントリへ追加
            var addResult = TryAddToPlayerInventory(playerInventory, itemEntity);
            if (!addResult.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Object, $"引き出し失敗（プレイヤーのインベントリが満杯の可能性）: {addResult.Error}");
                return;
            }

            // ストレージスロットを空にする
            storageRepository.TryClearItemSlot(storageSlotIndex);
            storageRepository.TryIncrementVersion();

            GameLogger.Log(LogCategory.Object, $"[{itemEntity.DisplayName}] をプレイヤーへ引き出しました。");
        }

        // ─── ユーティリティ ──────────────────────────────────────────

        /// <summary>プレイヤーのスロットからアイテムを消費する。</summary>
        private static bool TryConsumeFromPlayerSlot(IInventory playerInventory, int playerSlotIndex, InventoryItemEntity itemEntity)
        {

            if (itemEntity.ItemType == InventoryItemType.Weapon)
            {
                var result = playerInventory.TryConsumeWeapon(playerSlotIndex);
                if (!result.IsSuccess)
                {
                    GameLogger.LogWarning(LogCategory.Object, $"プレイヤーの武器消費に失敗（ロールバック）: {result.Error}");
                    return false;
                }

                return true;
            }

            return false;
        }

        /// <summary>プレイヤーのインベントリにアイテムを追加する。</summary>
        private static InventoryUseCaseResult TryAddToPlayerInventory(IInventory playerInventory, InventoryItemEntity itemEntity)
        {

            if (itemEntity.ItemType == InventoryItemType.Weapon && itemEntity.WeaponData != null)
            {
                return playerInventory.TryAddItem(InventoryItemSpecFactory.FromWeaponEntity(itemEntity), 1);
            }

            return InventoryUseCaseResult.Failed(InventoryUseCaseError.ItemNotFound, "storage.error.unknown_item_type");
        }

        // ─── 取り出す・預けるボタン操作 ─────────────────────────────

        /// <summary>
        /// 「取り出す」ボタンが押されたときに呼ばれる。
        /// 選択中ストレージスロットのアイテムをプレイヤーインベントリへ移動する。
        /// withdrawButton のクリックリスナーとして登録される。
        /// </summary>
        private void OnWithdrawFromSelectedStorageSlot()
        {
            if (playerInventoryController == null)
            {
                GameLogger.LogWarning(LogCategory.Object, "playerInventoryController が null です。");
                return;
            }

            if (selectedStorageSlotIndex < 0)
            {
                GameLogger.Log(LogCategory.Object, "取り出す対象スロットが選択されていません。ストレージのスロットをクリックして選択してください。");
                return;
            }

            WithdrawFromStorageSlotToPlayer(playerInventoryController, selectedStorageSlotIndex);

            selectedStorageSlotIndex = -1;
            RefreshAllViews();
            UpdateActionButtonsVisibility();
        }

        /// <summary>
        /// 「預ける」ボタンが押されたときに呼ばれる。
        /// プレイヤーのアクティブスロットにあるアイテムを選択中ストレージスロットへ移動する。
        /// depositButton のクリックリスナーとして登録される。
        /// </summary>
        private void OnDepositToSelectedStorageSlot()
        {
            if (playerInventoryController == null)
            {
                GameLogger.LogWarning(LogCategory.Object, "playerInventoryController が null です。");
                return;
            }

            if (selectedStorageSlotIndex < 0)
            {
                GameLogger.Log(LogCategory.Object, "預け先スロットが選択されていません。ストレージのスロットをクリックして選択してください。");
                return;
            }

            var playerActiveSlotIndex = playerInventoryController.GetActiveItemSlotIndex();
            var playerEntity = playerInventoryController.GetCurrentInventory();

            if (playerActiveSlotIndex < 0 || playerActiveSlotIndex >= playerEntity.ItemSlots.Count)
            {
                GameLogger.Log(LogCategory.Object, "プレイヤーのアクティブスロットが選択されていません。1〜4キーでスロットを選択してください。");
                return;
            }

            var playerSlot = playerEntity.ItemSlots[playerActiveSlotIndex];
            if (!playerSlot.Item.HasValue)
            {
                GameLogger.Log(LogCategory.Object, "プレイヤーのアクティブスロットが空です。");
                return;
            }

            DepositFromPlayerSlotToStorage(playerInventoryController, playerActiveSlotIndex, selectedStorageSlotIndex, playerSlot.Item.Value);

            selectedStorageSlotIndex = -1;
            RefreshAllViews();
            UpdateActionButtonsVisibility();
        }

        // ─── 捨てる/変換操作 ────────────────────────────────────────

        /// <summary>
        /// 選択中のストレージスロットのアイテムを捨てる（削除する）。
        /// discardButton のクリックリスナーとして登録される。
        /// </summary>
        private void OnDiscardSelectedStorageSlot()
        {
            if (selectedStorageSlotIndex < 0)
            {
                GameLogger.Log(LogCategory.Object, "捨てる対象スロットが選択されていません。ストレージのスロットをクリックしてください。");
                return;
            }

            var storageEntity = storageRepository.GetCurrent();
            if (selectedStorageSlotIndex >= storageEntity.ItemSlots.Count)
            {
                return;
            }

            var targetSlot = storageEntity.ItemSlots[selectedStorageSlotIndex];
            if (!targetSlot.Item.HasValue)
            {
                GameLogger.Log(LogCategory.Object, "選択中のストレージスロットは空です。");
                selectedStorageSlotIndex = -1;
                UpdateActionButtonsVisibility();
                return;
            }

            var itemName = targetSlot.Item.Value.DisplayName;
            var clearResult = storageRepository.TryClearItemSlot(selectedStorageSlotIndex);
            if (!clearResult.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Object, $"ストレージスロットのクリアに失敗しました: {clearResult.Error}");
                return;
            }

            storageRepository.TryIncrementVersion();
            GameLogger.Log(LogCategory.Object, $"[{itemName}] をストレージから捨てました。");

            selectedStorageSlotIndex = -1;
            RefreshAllViews();
            UpdateActionButtonsVisibility();
        }

        /// <summary>
        /// selectedStorageSlotIndex の状態に基づいて各アクションボタンの表示を更新する。
        /// ・選択スロットにアイテムがある    → 取り出す・捨てるボタン表示
        /// ・選択スロットに武器がある        → 変換ボタンも表示
        /// ・選択スロットが空 かつ プレイヤーにアクティブアイテムがある → 預けるボタン表示
        /// </summary>
        private void UpdateActionButtonsVisibility()
        {
            var hasItemInSelectedSlot = false;
            var isWeaponInSelectedSlot = false;
            var selectedSlotIsEmpty = false;

            if (selectedStorageSlotIndex >= 0 && storageRepository != null)
            {
                var storageEntity = storageRepository.GetCurrent();
                if (selectedStorageSlotIndex < storageEntity.ItemSlots.Count)
                {
                    var slotItem = storageEntity.ItemSlots[selectedStorageSlotIndex].Item;
                    hasItemInSelectedSlot = slotItem.HasValue;
                    selectedSlotIsEmpty = !slotItem.HasValue;
                    if (slotItem.HasValue)
                    {
                        isWeaponInSelectedSlot = slotItem.Value.ItemType == InventoryItemType.Weapon;
                    }
                }
            }

            // 「預ける」ボタン: 選択スロットが空 かつ プレイヤーにアクティブアイテムあり
            var playerHasActiveItem = false;
            if (playerInventoryController != null && selectedStorageSlotIndex >= 0)
            {
                var playerActiveSlotIndex = playerInventoryController.GetActiveItemSlotIndex();
                var playerEntity = playerInventoryController.GetCurrentInventory();
                if (playerActiveSlotIndex >= 0 && playerActiveSlotIndex < playerEntity.ItemSlots.Count)
                {
                    playerHasActiveItem = playerEntity.ItemSlots[playerActiveSlotIndex].Item.HasValue;
                }
            }

            if (withdrawButton != null)
            {
                withdrawButton.gameObject.SetActive(hasItemInSelectedSlot);
            }

            if (depositButton != null)
            {
                depositButton.gameObject.SetActive(selectedSlotIsEmpty && playerHasActiveItem);
            }

            if (discardButton != null)
            {
                discardButton.gameObject.SetActive(hasItemInSelectedSlot);
            }

        }

        // ─── View 更新 ────────────────────────────────────────────────

        /// <summary>ストレージとプレイヤーインベントリの両方のビューを更新する。</summary>
        private void RefreshAllViews()
        {
            RefreshView();
            RefreshPlayerInventoryView();
        }

        /// <summary>ストレージビューのみを更新する。</summary>
        private void RefreshView()
        {
            if (storageInventoryView == null)
            {
                return;
            }

            var entity = storageRepository.GetCurrent();
            var viewModels = BuildStorageViewModels(entity);
            storageInventoryView.ApplyViewModel(viewModels);
        }

        /// <summary>プレイヤーインベントリのビューを強制的に更新する。</summary>
        private void RefreshPlayerInventoryView()
        {
            if (playerInventoryController == null)
            {
                return;
            }

            // InventoryController の公開メソッド ForceRefreshView() を呼び出して
            // プレイヤーインベントリの表示を更新する
            playerInventoryController.ForceRefreshView();
        }

        /// <summary>
        /// ストレージ専用の ViewModel リストを構築する。
        /// 装備スロットは含まず、アイテムスロット（2枠）のみを対象とする。
        /// </summary>
        private IReadOnlyList<InventoryViewModel> BuildStorageViewModels(InventoryEntity entity)
        {
            var viewModels = new List<InventoryViewModel>();

            foreach (var slot in entity.ItemSlots)
            {
                InventoryViewModel viewModel;

                if (!slot.Item.HasValue)
                {
                    viewModel = new InventoryViewModel(
                        slot.SlotIndex,
                        false,
                        true,
                        InventoryItemType.None,
                        string.Empty,
                        "Empty",
                        storageDisplayParameter.EmptySlotSprite,
                        0,
                        slot.SlotIndex == selectedStorageSlotIndex);
                }
                else
                {
                    var item = slot.Item.Value;

                    // 武器の場合は WeaponData に直接アサインされたアイコンを優先する。
                    // InventoryBuildViewModelUseCase と同じロジック（プレイヤーインベントリと一貫させる）。
                    // ストレージ専用 InventoryDisplayParameter には武器アイコンが登録されていないことが多いため、
                    // WeaponData.InventoryIcon を優先しないと収納時にアイコンが表示されなくなる。
                    var resolvedIcon = item.ItemType == InventoryItemType.Weapon && item.WeaponData != null && item.WeaponData.InventoryIcon != null
                        ? item.WeaponData.InventoryIcon
                        : storageDisplayParameter.ResolveIcon(item.ItemId);

                    viewModel = new InventoryViewModel(
                        slot.SlotIndex,
                        false,
                        false,
                        item.ItemType,
                        item.ItemId,
                        item.DisplayName,
                        resolvedIcon,
                        item.Quantity,
                        slot.SlotIndex == selectedStorageSlotIndex,
                        item.EffectiveAttackPower,
                        item.Rarity);
                }

                viewModels.Add(viewModel);
            }

            return viewModels;
        }
    }
}
