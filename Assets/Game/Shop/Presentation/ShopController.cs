using System;
using System.Collections.Generic;
using Game.Debug;
using Game.Inventory;
using Game.Item.Parameter;
using Game.Item.UseCase;
using Game.Money.Presentation;
using Game.Pause;
using Game.Player.Script;
using Game.UI;
using UnityEngine;

namespace Game.Shop
{
    /// <summary>
    /// ショップの Presentation 層。画面の状態（選択・数量）を管理し、売買は UseCase に委譲する。
    /// ショップを開いている間はゲームプレイ入力とポーズ入力を止める。
    /// </summary>
    public class ShopController : MonoBehaviour, IPauseBlocker, IForceCloseableUi
    {
        [Header("データ")]
        [SerializeField] private ShopCatalogParameter catalogParameter;
        [SerializeField] private ShopUiParameter uiParameter;
        [SerializeField] private ItemDatabase itemDatabase;
        [SerializeField] private ItemRarityParameter rarityParameter;

        [Header("参照")]
        [SerializeField] private ShopView shopView;
        [SerializeField] private InventoryController playerInventoryController;
        [SerializeField] private MoneyWalletController moneyWalletController;
        [SerializeField] private GameInputReader gameInputReader;

        private ShopPurchaseUseCase purchaseUseCase;
        private ShopSellUseCase sellUseCase;
        private ShopSellTargetUseCase sellTargetUseCase;
        private IShopWallet wallet;
        private List<ShopProductEntity> products = new List<ShopProductEntity>();
        private List<ShopSellTargetEntity?> sellTargets = new List<ShopSellTargetEntity?>();

        private bool isInitialized;
        private bool isOpen;
        private ShopTransactionMode mode = ShopTransactionMode.None;
        private int selectedProductIndex = -1;
        private int selectedSlotIndex = -1;
        private ShopSellTargetEntity selectedTarget;
        private uint quantity = 1;

        public bool IsOpen => isOpen;
        public bool IsPauseBlocked => isOpen;

        private void Awake()
        {
            if (!ValidateReferences())
            {
                enabled = false;
                return;
            }

            wallet = new MoneyWalletShopAdapter(moneyWalletController);
            sellTargetUseCase = new ShopSellTargetUseCase(
                itemDatabase,
                new ItemSellPriceCalculatorUseCase(rarityParameter.SellPriceMultiplierPerRarity));
            purchaseUseCase = new ShopPurchaseUseCase(playerInventoryController, wallet);
            sellUseCase = new ShopSellUseCase(playerInventoryController, wallet, sellTargetUseCase);
            products = new ShopCatalogUseCase().BuildProducts(catalogParameter.ShopItems);

            shopView.Hide();
            isInitialized = true;
        }

        private void OnEnable()
        {
            if (shopView == null)
            {
                return;
            }

            shopView.ProductClicked += OnProductClicked;
            shopView.InventorySlotClicked += OnInventorySlotClicked;
            shopView.QuantityDecreaseClicked += OnQuantityDecreaseClicked;
            shopView.QuantityIncreaseClicked += OnQuantityIncreaseClicked;
            shopView.ExecuteClicked += OnExecuteClicked;
            shopView.CloseClicked += CloseShop;
        }

        private void OnDisable()
        {
            if (shopView != null)
            {
                shopView.ProductClicked -= OnProductClicked;
                shopView.InventorySlotClicked -= OnInventorySlotClicked;
                shopView.QuantityDecreaseClicked -= OnQuantityDecreaseClicked;
                shopView.QuantityIncreaseClicked -= OnQuantityIncreaseClicked;
                shopView.ExecuteClicked -= OnExecuteClicked;
                shopView.CloseClicked -= CloseShop;
            }

            CloseShop();
        }

        public void OpenShop()
        {
            if (!isInitialized || isOpen)
            {
                return;
            }

            isOpen = true;
            gameInputReader.DisableGameplayInputExceptPause();
            moneyWalletController.MoneyChanged += OnMoneyChanged;

            ResetSelection();
            shopView.Show();
            shopView.SetProducts(products);
            RefreshInventory();
            shopView.SetMoney(wallet.CurrentMoney);
            RefreshDetail();
            shopView.SetMessage(uiParameter.SelectHintMessage, false);
        }

        public void CloseShop()
        {
            if (!isOpen)
            {
                return;
            }

            isOpen = false;
            if (moneyWalletController != null)
            {
                moneyWalletController.MoneyChanged -= OnMoneyChanged;
            }

            shopView.Hide();
            if (gameInputReader != null)
            {
                gameInputReader.EnableGameplayInputExceptPause();
            }
        }

        public void CloseIfOpen()
        {
            CloseShop();
        }

        private bool ValidateReferences()
        {
            var isValid = true;
            isValid &= CheckReference(catalogParameter, nameof(catalogParameter));
            isValid &= CheckReference(uiParameter, nameof(uiParameter));
            isValid &= CheckReference(itemDatabase, nameof(itemDatabase));
            isValid &= CheckReference(rarityParameter, nameof(rarityParameter));
            isValid &= CheckReference(shopView, nameof(shopView));
            isValid &= CheckReference(playerInventoryController, nameof(playerInventoryController));
            isValid &= CheckReference(moneyWalletController, nameof(moneyWalletController));
            isValid &= CheckReference(gameInputReader, nameof(gameInputReader));
            return isValid;
        }

        private bool CheckReference(UnityEngine.Object reference, string referenceName)
        {
            if (reference != null)
            {
                return true;
            }

            GameLogger.LogError(LogCategory.UI, $"{referenceName} が未設定です。Inspector でアサインしてください。", this);
            return false;
        }

        private void ResetSelection()
        {
            mode = ShopTransactionMode.None;
            selectedProductIndex = -1;
            selectedSlotIndex = -1;
            quantity = 1;
            shopView.SetSelection(-1, -1);
        }

        private void OnProductClicked(int productIndex)
        {
            if (productIndex < 0 || productIndex >= products.Count)
            {
                return;
            }

            mode = ShopTransactionMode.Buy;
            selectedProductIndex = productIndex;
            selectedSlotIndex = -1;
            quantity = 1;
            shopView.SetSelection(selectedProductIndex, selectedSlotIndex);
            shopView.SetMessage(string.Empty, false);
            RefreshDetail();
        }

        private void OnInventorySlotClicked(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= sellTargets.Count || !sellTargets[slotIndex].HasValue)
            {
                return;
            }

            mode = ShopTransactionMode.Sell;
            selectedTarget = sellTargets[slotIndex].Value;
            selectedSlotIndex = slotIndex;
            selectedProductIndex = -1;
            quantity = 1;
            shopView.SetSelection(selectedProductIndex, selectedSlotIndex);
            shopView.SetMessage(string.Empty, false);
            RefreshDetail();
        }

        private void OnQuantityDecreaseClicked()
        {
            ChangeQuantity(-1);
        }

        private void OnQuantityIncreaseClicked()
        {
            ChangeQuantity(1);
        }

        private void ChangeQuantity(int delta)
        {
            if (mode == ShopTransactionMode.None)
            {
                return;
            }

            var maxQuantity = GetSelectableMaxQuantity();
            var nextQuantity = (long)quantity + delta;
            quantity = (uint)Math.Max(1L, Math.Min(nextQuantity, maxQuantity));
            RefreshDetail();
        }

        private uint GetSelectableMaxQuantity()
        {
            uint maxQuantity;
            if (mode == ShopTransactionMode.Buy)
            {
                maxQuantity = purchaseUseCase.GetMaxQuantity(products[selectedProductIndex], catalogParameter.MaxPurchaseQuantityPerTransaction);
            }
            else
            {
                maxQuantity = selectedTarget.Quantity;
            }

            // 買えない/売れない状態でも数量は 1 を下限として表示し、理由を表示する
            return Math.Max(1u, maxQuantity);
        }

        private void OnExecuteClicked()
        {
            if (mode == ShopTransactionMode.Buy)
            {
                ExecutePurchase();
            }
            else if (mode == ShopTransactionMode.Sell)
            {
                ExecuteSell();
            }
        }

        private void ExecutePurchase()
        {
            var product = products[selectedProductIndex];
            var result = purchaseUseCase.Execute(product, quantity);
            if (!result.IsSuccess)
            {
                ReportFailure(result.Error);
                return;
            }

            shopView.SetMessage(uiParameter.FormatPurchaseSuccess(product.DisplayName, quantity, result.TotalPrice), false);
            RefreshAfterTransaction();
        }

        private void ExecuteSell()
        {
            var result = sellUseCase.Execute(selectedTarget, quantity);
            if (!result.IsSuccess)
            {
                ReportFailure(result.Error);
                return;
            }

            shopView.SetMessage(uiParameter.FormatSellSuccess(selectedTarget.DisplayName, quantity, result.TotalPrice), false);
            RefreshAfterTransaction();
        }

        private void ReportFailure(ShopTransactionError error)
        {
            if (error == ShopTransactionError.RollbackFailed)
            {
                GameLogger.LogError(LogCategory.UI, "購入のロールバックに失敗しました。所持金とインベントリを確認してください。", this);
            }

            shopView.SetMessage(uiParameter.GetErrorMessage(error), true);
            RefreshAfterTransaction();
        }

        private void RefreshAfterTransaction()
        {
            RefreshInventory();
            shopView.SetMoney(wallet.CurrentMoney);

            if (mode == ShopTransactionMode.Sell)
            {
                // 売却後は同じスロットを引き続き選択できるが、空になった・別アイテムになった場合は選択を解除する
                var currentTarget = selectedSlotIndex < sellTargets.Count ? sellTargets[selectedSlotIndex] : null;
                if (!currentTarget.HasValue
                    || currentTarget.Value.InstanceId != selectedTarget.InstanceId)
                {
                    ResetSelection();
                }
                else
                {
                    selectedTarget = currentTarget.Value;
                }
            }

            if (mode != ShopTransactionMode.None)
            {
                quantity = Math.Min(quantity, GetSelectableMaxQuantity());
            }

            RefreshDetail();
        }

        private void OnMoneyChanged(uint currentMoney)
        {
            shopView.SetMoney(currentMoney);
        }

        private void RefreshInventory()
        {
            sellTargets = sellTargetUseCase.BuildAll(playerInventoryController.GetCurrentInventory());
            shopView.SetInventorySlots(sellTargets);
        }

        private void RefreshDetail()
        {
            switch (mode)
            {
                case ShopTransactionMode.Buy:
                    shopView.SetDetail(CreateBuyDetail());
                    break;
                case ShopTransactionMode.Sell:
                    shopView.SetDetail(CreateSellDetail());
                    break;
                default:
                    shopView.SetDetail(ShopDetailViewModel.Empty());
                    break;
            }
        }

        private ShopDetailViewModel CreateBuyDetail()
        {
            var product = products[selectedProductIndex];
            var validation = purchaseUseCase.Validate(product, quantity);
            var totalPrice = ClampToUint((ulong)product.UnitPrice * quantity);

            return new ShopDetailViewModel(
                true,
                product.Icon,
                shopView.GetRarityFrame(product.Rarity),
                uiParameter.FormatDetailName(product.DisplayName),
                product.ItemParameter.Description,
                uiParameter.FormatOwnedQuantity(CountOwnedQuantity(product)),
                uiParameter.FormatUnitPrice(product.UnitPrice),
                uiParameter.FormatQuantity(quantity),
                uiParameter.FormatTotalPrice(totalPrice),
                uiParameter.BuyButtonLabel,
                validation.IsSuccess,
                validation.IsSuccess ? string.Empty : uiParameter.GetErrorMessage(validation.Error));
        }

        private ShopDetailViewModel CreateSellDetail()
        {
            var validation = sellUseCase.Validate(selectedTarget, quantity);
            var totalPrice = ClampToUint((ulong)selectedTarget.UnitSellPrice * quantity);
            var description = string.Empty;
            var itemParameter = itemDatabase.GetItem(selectedTarget.ItemId);
            if (itemParameter != null)
            {
                description = itemParameter.Description;
            }

            return new ShopDetailViewModel(
                true,
                selectedTarget.Icon,
                shopView.GetRarityFrame(selectedTarget.Rarity),
                uiParameter.FormatDetailName(selectedTarget.DisplayName),
                description,
                uiParameter.FormatOwnedQuantity(selectedTarget.Quantity),
                uiParameter.FormatUnitPrice(selectedTarget.UnitSellPrice),
                uiParameter.FormatQuantity(quantity),
                uiParameter.FormatTotalPrice(totalPrice),
                uiParameter.SellButtonLabel,
                validation.IsSuccess,
                validation.IsSuccess ? string.Empty : uiParameter.GetErrorMessage(validation.Error));
        }

        private uint CountOwnedQuantity(ShopProductEntity product)
        {
            ulong ownedQuantity = 0;
            foreach (var slot in playerInventoryController.GetCurrentInventory().ItemSlots)
            {
                if (slot.Item.HasValue && slot.Item.Value.ItemId == product.ItemId && slot.Item.Value.Rarity == product.Rarity)
                {
                    ownedQuantity += slot.Item.Value.Quantity;
                }
            }

            return ClampToUint(ownedQuantity);
        }

        private static uint ClampToUint(ulong value)
        {
            return value > uint.MaxValue ? uint.MaxValue : (uint)value;
        }
    }
}
