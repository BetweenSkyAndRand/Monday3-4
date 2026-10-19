using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Game.Shop
{
    /// <summary>
    /// ショップ画面全体のView。表示と入力イベントの通知だけを行い、売買ロジックは持たない。
    /// 左: 商品一覧 / 下: 所持品スロット / 右: 詳細と購入・売却操作。
    /// </summary>
    public class ShopView : MonoBehaviour
    {
        [Header("画面ルート")]
        [SerializeField] private GameObject panelRoot;

        [Header("共通設定")]
        [SerializeField] private ShopUiParameter uiParameter;
        [SerializeField] private Game.Item.Parameter.ItemRarityParameter rarityParameter;

        [Header("見出し・所持金")]
        [SerializeField] private TextMeshProUGUI titleText;
        [SerializeField] private TextMeshProUGUI productHeaderText;
        [SerializeField] private TextMeshProUGUI inventoryHeaderText;
        [SerializeField] private TextMeshProUGUI moneyText;

        [Header("左カラム: 商品一覧")]
        [SerializeField] private Transform productContainer;
        [SerializeField] private ShopEntryView productEntryPrefab;

        [Header("下部: 所持品スロット")]
        [SerializeField] private Transform inventorySlotContainer;
        [SerializeField] private ShopEntryView inventorySlotPrefab;

        [Header("右カラム: 詳細")]
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Image detailIconImage;
        [SerializeField] private Image detailRarityFrameImage;
        [SerializeField] private TextMeshProUGUI detailNameText;
        [SerializeField] private TextMeshProUGUI detailDescriptionText;
        [SerializeField] private TextMeshProUGUI detailOwnedQuantityText;
        [SerializeField] private TextMeshProUGUI unitPriceText;
        [SerializeField] private TextMeshProUGUI quantityText;
        [SerializeField] private TextMeshProUGUI totalPriceText;
        [SerializeField] private TextMeshProUGUI reasonText;
        [SerializeField] private TextMeshProUGUI messageText;

        [Header("ボタン")]
        [SerializeField] private Button quantityDecreaseButton;
        [SerializeField] private Button quantityIncreaseButton;
        [SerializeField] private Button executeButton;
        [SerializeField] private TextMeshProUGUI executeButtonLabel;
        [Tooltip("画面下部などに置くキャンセルボタン。")]
        [SerializeField] private Button cancelButton;
        [Tooltip("右側に置く戻るボタン。")]
        [SerializeField] private Button backButton;
        [SerializeField] private TextMeshProUGUI cancelButtonLabel;
        [SerializeField] private TextMeshProUGUI backButtonLabel;

        private readonly List<ShopEntryView> productEntryViews = new List<ShopEntryView>();
        private readonly List<ShopEntryView> inventorySlotViews = new List<ShopEntryView>();
        private IReadOnlyList<ShopSellTargetEntity?> currentSlotTargets;
        private int selectedProductIndex = -1;
        private int selectedSlotIndex = -1;

        public event Action<int> ProductClicked;
        public event Action<int> InventorySlotClicked;
        public event Action QuantityDecreaseClicked;
        public event Action QuantityIncreaseClicked;
        public event Action ExecuteClicked;
        public event Action CloseClicked;

        private void Awake()
        {
            BindButton(quantityDecreaseButton, () => QuantityDecreaseClicked?.Invoke());
            BindButton(quantityIncreaseButton, () => QuantityIncreaseClicked?.Invoke());
            BindButton(executeButton, () => ExecuteClicked?.Invoke());
            BindButton(cancelButton, () => CloseClicked?.Invoke());
            BindButton(backButton, () => CloseClicked?.Invoke());
        }

        public void Show()
        {
            ApplyStaticTexts();
            SetPanelActive(true);
        }

        public void Hide()
        {
            SetPanelActive(false);
        }

        public void SetMoney(uint money)
        {
            if (moneyText != null && uiParameter != null)
            {
                moneyText.text = uiParameter.FormatMoney(money);
            }
        }

        public void SetProducts(IReadOnlyList<ShopProductEntity> products)
        {
            ClearEntries(productEntryViews);
            for (var productIndex = 0; productIndex < products.Count; productIndex++)
            {
                var product = products[productIndex];
                var entryView = Instantiate(productEntryPrefab, productContainer);
                entryView.Initialize(productIndex, index => ProductClicked?.Invoke(index));
                entryView.SetContent(
                    product.Icon,
                    GetRarityFrame(product.Rarity),
                    product.DisplayName,
                    uiParameter != null ? uiParameter.FormatPrice(product.UnitPrice) : product.UnitPrice.ToString(),
                    string.Empty,
                    false);
                productEntryViews.Add(entryView);
            }

            RefreshSelectionColors();
        }

        public void SetInventorySlots(IReadOnlyList<ShopSellTargetEntity?> slotTargets)
        {
            currentSlotTargets = slotTargets;

            // スロット数は固定なので、足りない分だけ生成して使い回す
            while (inventorySlotViews.Count < slotTargets.Count)
            {
                var entryView = Instantiate(inventorySlotPrefab, inventorySlotContainer);
                entryView.Initialize(inventorySlotViews.Count, index => InventorySlotClicked?.Invoke(index));
                inventorySlotViews.Add(entryView);
            }

            for (var slotIndex = 0; slotIndex < inventorySlotViews.Count; slotIndex++)
            {
                var entryView = inventorySlotViews[slotIndex];
                var isUsed = slotIndex < slotTargets.Count;
                entryView.gameObject.SetActive(isUsed);
                if (!isUsed)
                {
                    continue;
                }

                var target = slotTargets[slotIndex];
                if (!target.HasValue)
                {
                    entryView.SetEmpty();
                    continue;
                }

                var targetValue = target.Value;
                entryView.SetContent(
                    targetValue.Icon,
                    GetRarityFrame(targetValue.Rarity),
                    targetValue.DisplayName,
                    string.Empty,
                    targetValue.Quantity > 1 && uiParameter != null ? uiParameter.FormatSlotQuantity(targetValue.Quantity) : string.Empty,
                    targetValue.IsEquippedWeapon);
            }

            RefreshSelectionColors();
        }

        public void SetSelection(int productIndex, int slotIndex)
        {
            selectedProductIndex = productIndex;
            selectedSlotIndex = slotIndex;
            RefreshSelectionColors();
        }

        public void SetDetail(ShopDetailViewModel viewModel)
        {
            if (detailRoot != null)
            {
                detailRoot.SetActive(viewModel.HasSelection);
            }

            if (detailIconImage != null)
            {
                detailIconImage.sprite = viewModel.Icon;
                detailIconImage.enabled = viewModel.Icon != null;
                detailIconImage.SetAllDirty();
            }

            if (detailRarityFrameImage != null)
            {
                detailRarityFrameImage.sprite = viewModel.RarityFrame;
                detailRarityFrameImage.enabled = viewModel.RarityFrame != null;
                detailRarityFrameImage.SetAllDirty();
            }

            SetText(detailNameText, viewModel.DisplayName);
            SetText(detailDescriptionText, viewModel.DescriptionText);
            SetText(detailOwnedQuantityText, viewModel.OwnedQuantityText);
            SetText(unitPriceText, viewModel.UnitPriceText);
            SetText(quantityText, viewModel.QuantityText);
            SetText(totalPriceText, viewModel.TotalPriceText);
            SetText(executeButtonLabel, viewModel.ActionLabel);
            SetText(reasonText, viewModel.ReasonText);

            if (executeButton != null)
            {
                executeButton.interactable = viewModel.HasSelection && viewModel.CanExecute;
            }

            if (quantityDecreaseButton != null)
            {
                quantityDecreaseButton.interactable = viewModel.HasSelection;
            }

            if (quantityIncreaseButton != null)
            {
                quantityIncreaseButton.interactable = viewModel.HasSelection;
            }
        }

        public void SetMessage(string message, bool isError)
        {
            if (messageText == null)
            {
                return;
            }

            messageText.text = message;
            if (uiParameter != null)
            {
                messageText.color = isError ? uiParameter.ErrorMessageColor : uiParameter.SuccessMessageColor;
            }
        }

        private void SetPanelActive(bool isActive)
        {
            if (panelRoot != null)
            {
                panelRoot.SetActive(isActive);
            }
        }

        private void ApplyStaticTexts()
        {
            if (uiParameter == null)
            {
                return;
            }

            SetText(titleText, uiParameter.ShopTitle);
            SetText(productHeaderText, uiParameter.ProductHeaderLabel);
            SetText(inventoryHeaderText, uiParameter.InventoryHeaderLabel);
            SetText(cancelButtonLabel, uiParameter.CancelButtonLabel);
            SetText(backButtonLabel, uiParameter.BackButtonLabel);
        }

        private void RefreshSelectionColors()
        {
            if (uiParameter == null)
            {
                return;
            }

            for (var productIndex = 0; productIndex < productEntryViews.Count; productIndex++)
            {
                productEntryViews[productIndex].SetColor(
                    productIndex == selectedProductIndex ? uiParameter.SelectedSlotColor : uiParameter.NormalSlotColor);
            }

            if (currentSlotTargets == null)
            {
                return;
            }

            for (var slotIndex = 0; slotIndex < currentSlotTargets.Count && slotIndex < inventorySlotViews.Count; slotIndex++)
            {
                var target = currentSlotTargets[slotIndex];
                var color = uiParameter.NormalSlotColor;
                if (slotIndex == selectedSlotIndex)
                {
                    color = uiParameter.SelectedSlotColor;
                }
                else if (target.HasValue && !target.Value.CanSell)
                {
                    color = uiParameter.UnavailableSlotColor;
                }

                inventorySlotViews[slotIndex].SetColor(color);
            }
        }

        public Sprite GetRarityFrame(Game.Item.ItemRarity rarity)
        {
            return rarityParameter != null ? rarityParameter.GetFrameSprite(rarity) : null;
        }

        private static void ClearEntries(List<ShopEntryView> entryViews)
        {
            foreach (var entryView in entryViews)
            {
                if (entryView != null)
                {
                    Destroy(entryView.gameObject);
                }
            }

            entryViews.Clear();
        }

        private static void BindButton(Button targetButton, Action onClick)
        {
            if (targetButton != null)
            {
                targetButton.onClick.AddListener(() => onClick());
            }
        }

        private static void SetText(TextMeshProUGUI targetText, string text)
        {
            if (targetText != null)
            {
                targetText.text = text;
            }
        }
    }
}
