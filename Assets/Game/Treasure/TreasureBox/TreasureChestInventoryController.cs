using System.Collections.Generic;
using Game.Debug;
using Game.Inventory;
using Game.Inventory.Parameter;
using UnityEngine;
using UnityEngine.UI;

// ReSharper disable once CheckNamespace
namespace Game.Treasure
{
    /// <summary>
    /// 宝箱 UI を管理するシーン常駐シングルトン。StorageInventoryController と同じ構造。
    ///
    /// シーンに 1 つだけ配置する。宝箱プレハブ側に UI 参照は持たせない。
    ///
    /// セットアップ手順（Unity Editor）:
    ///   1. シーン上の UI GameObject（Canvas 配下など）にこのスクリプトをアタッチ
    ///   2. chestInventoryView   → 宝箱用 InventoryView パネル（スロット1つ）
    ///   3. chestDisplayParameter → 1スロット設定の InventoryDisplayParameter アセット
    ///   4. takeButton / takeAllButton / discardButton / closeButton
    ///      → Canvas 上のボタンを Inspector でアサイン
    ///   5. playerInventoryController → シーン上の InventoryController をアサイン
    /// </summary>
    public class TreasureChestInventoryController : MonoBehaviour
    {
        /// <summary>シーン内で唯一のインスタンス。TreasureBoxController から参照される。</summary>
        public static TreasureChestInventoryController Instance { get; private set; }

        [Header("宝箱UI")]
        [Tooltip("宝箱の中身を表示する InventoryView（1スロット）。")]
        [SerializeField] private InventoryView chestInventoryView;

        [Tooltip("宝箱UI用の InventoryDisplayParameter（1スロット設定）。Project アセットをアサイン。")]
        [SerializeField] private InventoryDisplayParameter chestDisplayParameter;

        [Header("操作ボタン")]
        [Tooltip("選択アイテムをプレイヤーへ受け取るボタン。")]
        [SerializeField] private Button takeButton;

        [Tooltip("宝箱内の全アイテムをプレイヤーへ受け取るボタン。")]
        [SerializeField] private Button takeAllButton;

        [Tooltip("選択アイテムを捨てるボタン。")]
        [SerializeField] private Button discardButton;

        [Tooltip("宝箱UIを閉じるボタン。")]
        [SerializeField] private Button closeButton;

        [Header("プレイヤーインベントリ参照")]
        [Tooltip("シーン上の InventoryController をアサイン。")]
        [SerializeField] private InventoryController playerInventoryController;

        /// <summary>現在 UI が開いている宝箱。null なら UI は閉じている。</summary>
        public TreasureBoxController CurrentChest { get; private set; }

        /// <summary>UI が開いているか。</summary>
        public bool IsOpen => CurrentChest != null;

        /// <summary>UI 内で選択中のスロットインデックス（-1 = 未選択）。</summary>
        private int selectedSlotIndex = -1;

        private void Awake()
        {
            Instance = this;

            // ─── 必須フィールドの検証 ───────────────────────────────
            if (chestInventoryView == null)
            {
                GameLogger.LogError(LogCategory.Object,
                    "chestInventoryView が未設定です。" +
                    "シーン上の InventoryView コンポーネントを Inspector でアサインしてください。", this);
            }

            if (chestDisplayParameter == null)
            {
                GameLogger.LogError(LogCategory.Object,
                    "chestDisplayParameter が未設定です。" +
                    "1スロット設定の InventoryDisplayParameter アセットを Inspector でアサインしてください。", this);
            }

            if (playerInventoryController == null)
            {
                GameLogger.LogWarning(LogCategory.Object,
                    "playerInventoryController が未設定です。" +
                    "シーン上の InventoryController を Inspector でアサインしてください。", this);
            }

            // 起動時は非表示
            chestInventoryView?.Hide();
            SetButtonsActive(false, false, false, false);

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(Close);
            }

            if (takeButton != null)
            {
                takeButton.onClick.AddListener(OnTakeSelected);
            }

            if (takeAllButton != null)
            {
                takeAllButton.onClick.AddListener(OnTakeAll);
            }

            if (discardButton != null)
            {
                discardButton.onClick.AddListener(OnDiscardSelected);
            }

            if (chestInventoryView != null && chestDisplayParameter != null)
            {
                chestInventoryView.Initialize(chestDisplayParameter, 1);
                chestInventoryView.SetSlotClickListener(OnClickSlot);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }
        }

        // ─── 公開API（TreasureBoxController から呼ばれる） ───────────

        /// <summary>
        /// 指定した宝箱の UI を開く。
        /// すでに別の宝箱が開いている場合は先に閉じる。
        /// </summary>
        public void OpenWith(TreasureBoxController chest)
        {
            if (chest == null)
            {
                return;
            }

            // 別の宝箱が開いていれば先に閉じる（通知のみ、UI操作はこちらで行う）
            if (CurrentChest != null && CurrentChest != chest)
            {
                CurrentChest.NotifyClosed();
            }

            CurrentChest = chest;
            selectedSlotIndex = -1;

            CurrentChest.NotifyOpened();
            // パネルを先にアクティブ化してから ViewModel を適用する。
            // 非アクティブのまま sprite を更新するとアイコン描画が遅延することがあるため、
            // Show() → RefreshView() の順で呼ぶ。
            chestInventoryView?.Show();
            RefreshView();
            UpdateActionButtonsVisibility();
        }

        /// <summary>UI を閉じる。</summary>
        public void Close()
        {
            if (CurrentChest != null)
            {
                CurrentChest.NotifyClosed();
                CurrentChest = null;
            }

            selectedSlotIndex = -1;
            chestInventoryView?.Hide();
            SetButtonsActive(false, false, false, false);
        }

        // ─── スロットクリック ─────────────────────────────────────────

        private void OnClickSlot(int slotIndex)
        {
            // 同じスロットを再クリックで選択解除
            selectedSlotIndex = selectedSlotIndex == slotIndex ? -1 : slotIndex;
            UpdateActionButtonsVisibility();
            RefreshView();
        }

        // ─── 受け取る ────────────────────────────────────────────────

        private void OnTakeSelected()
        {
            if (CurrentChest == null || selectedSlotIndex < 0)
            {
                return;
            }

            TakeFromSlot(selectedSlotIndex);
        }

        private void OnTakeAll()
        {
            if (CurrentChest == null)
            {
                return;
            }

            var entity = CurrentChest.GetChestContents();
            if (!entity.HasValue)
            {
                return;
            }

            for (var i = 0; i < entity.Value.ItemSlots.Count; i++)
            {
                if (entity.Value.ItemSlots[i].Item.HasValue)
                {
                    TakeFromSlot(i);
                }
            }
        }

        private void TakeFromSlot(int slotIndex)
        {
            if (CurrentChest == null)
            {
                return;
            }

            var entity = CurrentChest.GetChestContents();
            if (!entity.HasValue || slotIndex >= entity.Value.ItemSlots.Count)
            {
                return;
            }

            var slot = entity.Value.ItemSlots[slotIndex];
            if (!slot.Item.HasValue)
            {
                return;
            }

            if (!InventoryRuntime.TryGetInventory(out var inventory))
            {
                GameLogger.LogWarning(LogCategory.Object, "IInventory が見つかりません。");
                return;
            }

            var item = slot.Item.Value;
            InventoryUseCaseResult addResult;

            if (item.ItemType == InventoryItemType.Weapon && item.WeaponData != null)
            {
                addResult = inventory.TryAddItem(InventoryItemSpecFactory.FromWeaponEntity(item), 1);
            }
            else
            {
                return;
            }

            if (!addResult.IsSuccess)
            {
                GameLogger.LogWarning(LogCategory.Object, $"受け取り失敗（インベントリ満杯の可能性）: {addResult.Error}");
                return;
            }

            CurrentChest.TryClearChestSlot(slotIndex);
            GameLogger.Log(LogCategory.Object, $"[{item.DisplayName}] を宝箱から受け取りました。");

            selectedSlotIndex = -1;
            AfterItemRemoved();
        }

        // ─── 捨てる ──────────────────────────────────────────────────

        private void OnDiscardSelected()
        {
            if (CurrentChest == null || selectedSlotIndex < 0)
            {
                return;
            }

            var entity = CurrentChest.GetChestContents();
            if (!entity.HasValue || selectedSlotIndex >= entity.Value.ItemSlots.Count)
            {
                return;
            }

            var slot = entity.Value.ItemSlots[selectedSlotIndex];
            if (!slot.Item.HasValue)
            {
                return;
            }

            var itemName = slot.Item.Value.DisplayName;
            CurrentChest.TryClearChestSlot(selectedSlotIndex);
            GameLogger.Log(LogCategory.Object, $"[{itemName}] を宝箱から捨てました。");

            selectedSlotIndex = -1;
            AfterItemRemoved();
        }

        /// <summary>アイテム除去後に、表示とボタン状態を更新する。</summary>
        private void AfterItemRemoved()
        {
            RefreshView();
            UpdateActionButtonsVisibility();
        }

        /// <summary>宝箱 UI のスロット表示を更新する。</summary>
        private void RefreshView()
        {
            if (chestInventoryView == null || chestDisplayParameter == null || CurrentChest == null)
            {
                return;
            }

            var entity = CurrentChest.GetChestContents();
            if (!entity.HasValue)
            {
                return;
            }

            var viewModels = new List<InventoryViewModel>();

            foreach (var slot in entity.Value.ItemSlots)
            {
                InventoryViewModel viewModel;

                if (!slot.Item.HasValue)
                {
                    viewModel = new InventoryViewModel(
                        slot.SlotIndex, false, true,
                        InventoryItemType.None, string.Empty, "Empty",
                        chestDisplayParameter.EmptySlotSprite, 0, false);
                }
                else
                {
                    var item = slot.Item.Value;
                    var icon = item.ItemType == InventoryItemType.Weapon && item.WeaponData?.InventoryIcon != null
                        ? item.WeaponData.InventoryIcon
                        : chestDisplayParameter.ResolveIcon(item.ItemId);

                    viewModel = new InventoryViewModel(
                        slot.SlotIndex,
                        slot.SlotIndex == selectedSlotIndex,
                        false,
                        item.ItemType, item.ItemId, item.DisplayName,
                        icon, item.Quantity, false,
                        item.EffectiveAttackPower,
                        item.Rarity);
                }

                viewModels.Add(viewModel);
            }

            chestInventoryView.ApplyViewModel(viewModels);
        }

        /// <summary>選択状態・宝箱の内容に応じてボタンの表示を更新する。</summary>
        private void UpdateActionButtonsVisibility()
        {
            var hasAnyItem = false;
            var selectedHasItem = false;

            if (CurrentChest != null)
            {
                var entity = CurrentChest.GetChestContents();
                if (entity.HasValue)
                {
                    foreach (var slot in entity.Value.ItemSlots)
                    {
                        if (slot.Item.HasValue)
                        {
                            hasAnyItem = true;
                        }
                    }

                    if (selectedSlotIndex >= 0 && selectedSlotIndex < entity.Value.ItemSlots.Count)
                    {
                        var selectedSlot = entity.Value.ItemSlots[selectedSlotIndex];
                        selectedHasItem = selectedSlot.Item.HasValue;
                    }
                }
            }

            SetButtonsActive(
                takeActive: selectedHasItem,
                takeAllActive: hasAnyItem,
                discardActive: selectedHasItem,
                closeActive: true);
        }

        private void SetButtonsActive(bool takeActive, bool takeAllActive, bool discardActive, bool closeActive)
        {
            if (takeButton != null)
            {
                takeButton.gameObject.SetActive(takeActive);
            }

            if (takeAllButton != null)
            {
                takeAllButton.gameObject.SetActive(takeAllActive);
            }

            if (discardButton != null)
            {
                discardButton.gameObject.SetActive(discardActive);
            }

            if (closeButton != null)
            {
                closeButton.gameObject.SetActive(closeActive);
            }
        }
    }
}
