using System;
using System.Collections.Generic;
using Game.Debug;
using Game.Inventory.Parameter;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Inventory
{
    public class InventoryView : MonoBehaviour, IInventoryView
    {
        [SerializeField] private GameObject itemSlotPrefab;
        [SerializeField] private Transform itemSlotParent;
        [SerializeField] private GameObject inventoryRoot;

        [Header("装備ボタン（武器スロット選択時のみ表示）")]
        [Tooltip("選択したアイテムスロットの武器を装備するボタン。InspectorでCanvasのButtonをアサイン")]
        [SerializeField] private Button equipButton;

        [Header("捨てるボタン（アイテム選択時のみ表示）")]
        [Tooltip("選択したスロットのアイテムを捨てるボタン。InspectorでCanvasのButtonをアサイン")]
        [SerializeField] private Button discardButton;

        private readonly List<IItemSlotView> itemSlots = new();

        private void Awake()
        {
            // すべてのアクションボタンをゲーム開始時に強制的に非表示にする。
            // Prefab や Scene でボタンが Active=true になっていても、
            // コードが制御するまで表示されないようにするため。
            // ボタンが未アサインの場合はエラーログで通知する。
            HideEquipButton();
            HideDiscardButton();
        }

        public void Initialize(InventoryDisplayParameter parameter, int totalSlotCount)
        {
            ClearAllSlots();

            if (itemSlotPrefab == null)
            {
                GameLogger.LogError(LogCategory.Inventory,
                    "itemSlotPrefab が未設定です。Inspector で ItemSlotPrefab を割り当ててください。" +
                    $" (GameObject: {gameObject.name})", this);
                return;
            }

            if (itemSlotParent == null)
            {
                GameLogger.LogError(LogCategory.Inventory,
                    "itemSlotParent が未設定です。Inspector でスロットを並べる Transform を割り当ててください。" +
                    $" (GameObject: {gameObject.name})", this);
                return;
            }

            for (var i = 0; i < totalSlotCount; i++)
            {
                var slotObject = Instantiate(itemSlotPrefab, itemSlotParent);
                var slotView = slotObject.GetComponent<IItemSlotView>();
                if (slotView == null)
                {
                    GameLogger.LogError(LogCategory.Inventory, "itemSlotPrefab must implement IItemSlotView.");
                    Destroy(slotObject);
                    continue;
                }

                slotView.Initialize(i);
                itemSlots.Add(slotView);
            }

            if (inventoryRoot == null)
            {
                inventoryRoot = gameObject;
            }
        }

        public void Show()
        {
            if (inventoryRoot == null)
            {
                inventoryRoot = gameObject;
            }

            inventoryRoot.SetActive(true);

            // SetActive(true) と同フレームの sprite 設定が Canvas に取りこぼされる問題への対策。
            // レイアウトと Canvas の強制再構築を行うことで、即座にアイコンが描画されるようにする。
            ForceCanvasRebuild();
        }

        public void Hide()
        {
            if (inventoryRoot == null)
            {
                inventoryRoot = gameObject;
            }

            inventoryRoot.SetActive(false);
        }

        public void Toggle()
        {
            if (inventoryRoot == null)
            {
                inventoryRoot = gameObject;
            }

            inventoryRoot.SetActive(!inventoryRoot.activeSelf);
        }

        public void ApplyViewModel(IReadOnlyList<InventoryViewModel> viewModels)
        {
            foreach (var viewModel in viewModels)
            {
                if (viewModel.SlotIndex < 0 || viewModel.SlotIndex >= itemSlots.Count)
                {
                    continue;
                }

                itemSlots[viewModel.SlotIndex].ApplyViewModel(viewModel);
            }

            // sprite 設定が CanvasRenderer に反映されない取りこぼし対策。
            // ApplyViewModel が呼ばれたら強制的に Canvas / レイアウトを更新する。
            ForceCanvasRebuild();
        }

        /// <summary>
        /// Canvas / レイアウトを強制的に再構築する。
        /// SetActive(true) と同フレームの sprite 設定が描画に反映されない問題への対策として呼ぶ。
        /// </summary>
        private void ForceCanvasRebuild()
        {
            if (itemSlotParent != null)
            {
                var rect = itemSlotParent as RectTransform;
                if (rect == null)
                {
                    rect = itemSlotParent.GetComponent<RectTransform>();
                }

                if (rect != null)
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(rect);
                }
            }

            Canvas.ForceUpdateCanvases();
        }

        public void SetSlotClickListener(Action<int> onClick)
        {
            foreach (var itemSlot in itemSlots)
            {
                itemSlot.SetButtonListener(onClick);
            }
        }

        public void SetEquipButtonListener(Action onClick)
        {
            if (equipButton == null)
            {
                return;
            }

            equipButton.onClick.RemoveAllListeners();
            equipButton.onClick.AddListener(() => onClick?.Invoke());
            HideEquipButton();
        }

        public void ShowEquipButton()
        {
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(true);
            }
        }

        public void HideEquipButton()
        {
            if (equipButton != null)
            {
                equipButton.gameObject.SetActive(false);
            }
        }

        public void SetDiscardButtonListener(Action onDiscard)
        {
            if (discardButton == null)
            {
                // Awake() でも通知済みのため、ここでは重複ログを出さない
                return;
            }

            discardButton.onClick.RemoveAllListeners();
            discardButton.onClick.AddListener(() => onDiscard?.Invoke());
            HideDiscardButton();
        }

        public void ShowDiscardButton()
        {
            if (discardButton != null)
            {
                discardButton.gameObject.SetActive(true);
            }
        }

        public void HideDiscardButton()
        {
            if (discardButton != null)
            {
                discardButton.gameObject.SetActive(false);
            }
        }

        private void ClearAllSlots()
        {
            foreach (Transform child in itemSlotParent)
            {
                Destroy(child.gameObject);
            }

            itemSlots.Clear();
        }
    }
}
