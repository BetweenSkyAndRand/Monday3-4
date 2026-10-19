using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Shop
{
    /// <summary>
    /// ショップ画面の表示テキスト・色をまとめた ScriptableObject。
    /// 文言や色を変えたいときはこのアセットだけを編集する（コード・プレハブの変更は不要）。
    /// 書式文字列の {0}, {1} は string.Format の引数。
    /// </summary>
    [CreateAssetMenu(fileName = "ShopUiParameter", menuName = "Game/Shop/ShopUiParameter")]
    public class ShopUiParameter : ScriptableObject
    {
        [Header("見出し・ボタン")]
        [SerializeField] private string shopTitle = "ショップ";
        [SerializeField] private string buyButtonLabel = "購入する";
        [SerializeField] private string sellButtonLabel = "売却する";
        [SerializeField] private string cancelButtonLabel = "キャンセル";
        [SerializeField] private string backButtonLabel = "戻る";
        [SerializeField] private string inventoryHeaderLabel = "所持品（売却）";
        [SerializeField] private string productHeaderLabel = "商品（購入）";

        [Header("書式 {0}=金額 など")]
        [Tooltip("所持金表示。{0}=所持金")]
        [SerializeField] private string moneyFormat = "所持金 {0} G";
        [Tooltip("商品行の価格表示。{0}=価格")]
        [SerializeField] private string priceFormat = "{0} G";
        [Tooltip("単価表示。{0}=単価")]
        [SerializeField] private string unitPriceFormat = "単価 {0} G";
        [Tooltip("合計表示。{0}=合計")]
        [SerializeField] private string totalPriceFormat = "合計 {0} G";
        [Tooltip("数量表示。{0}=数量")]
        [SerializeField] private string quantityFormat = "{0}";
        [Tooltip("所持数表示（購入時）。{0}=現在の所持数")]
        [SerializeField] private string ownedQuantityFormat = "所持数 {0}";
        [Tooltip("スロットの数量表示。{0}=数量")]
        [SerializeField] private string slotQuantityFormat = "x{0}";
        [Tooltip("詳細のアイテム名表示。{0}=アイテム名")]
        [SerializeField] private string detailNameFormat = "{0}";

        [Header("メッセージ")]
        [SerializeField] private string selectHintMessage = "商品または所持品を選んでください。";
        [Tooltip("購入成功。{0}=アイテム名 {1}=数量 {2}=合計")]
        [SerializeField] private string purchaseSuccessFormat = "{0} x{1} を購入しました（-{2} G）";
        [Tooltip("売却成功。{0}=アイテム名 {1}=数量 {2}=合計")]
        [SerializeField] private string sellSuccessFormat = "{0} x{1} を売却しました（+{2} G）";

        [Header("失敗理由")]
        [SerializeField] private string invalidProductMessage = "この商品は購入できません。";
        [SerializeField] private string invalidQuantityMessage = "数量が正しくありません。";
        [SerializeField] private string insufficientMoneyMessage = "所持金が足りません。";
        [SerializeField] private string inventoryFullMessage = "所持品がいっぱいです。";
        [SerializeField] private string targetChangedMessage = "所持品が変化したため、もう一度選び直してください。";
        [SerializeField] private string notSellableMessage = "このアイテムは売却できません。";
        [SerializeField] private string equippedWeaponMessage = "装備中の武器は売却できません。";
        [SerializeField] private string rollbackFailedMessage = "取引に失敗しました。所持金を確認してください。";
        [SerializeField] private string failedMessage = "取引に失敗しました。";

        [Header("色")]
        [SerializeField] private Color normalSlotColor = Color.white;
        [SerializeField] private Color selectedSlotColor = new Color(1f, 0.9f, 0.4f, 1f);
        [SerializeField] private Color unavailableSlotColor = new Color(1f, 1f, 1f, 0.5f);
        [SerializeField] private Color successMessageColor = new Color(0.6f, 1f, 0.6f, 1f);
        [SerializeField] private Color errorMessageColor = new Color(1f, 0.5f, 0.5f, 1f);

        public string ShopTitle => shopTitle;
        public string BuyButtonLabel => buyButtonLabel;
        public string SellButtonLabel => sellButtonLabel;
        public string CancelButtonLabel => cancelButtonLabel;
        public string BackButtonLabel => backButtonLabel;
        public string InventoryHeaderLabel => inventoryHeaderLabel;
        public string ProductHeaderLabel => productHeaderLabel;
        public string SelectHintMessage => selectHintMessage;
        public Color NormalSlotColor => normalSlotColor;
        public Color SelectedSlotColor => selectedSlotColor;
        public Color UnavailableSlotColor => unavailableSlotColor;
        public Color SuccessMessageColor => successMessageColor;
        public Color ErrorMessageColor => errorMessageColor;

        public string FormatMoney(uint money)
        {
            return string.Format(moneyFormat, money);
        }

        public string FormatPrice(uint price)
        {
            return string.Format(priceFormat, price);
        }

        public string FormatUnitPrice(uint unitPrice)
        {
            return string.Format(unitPriceFormat, unitPrice);
        }

        public string FormatTotalPrice(uint totalPrice)
        {
            return string.Format(totalPriceFormat, totalPrice);
        }

        public string FormatQuantity(uint quantity)
        {
            return string.Format(quantityFormat, quantity);
        }

        public string FormatOwnedQuantity(uint ownedQuantity)
        {
            return string.Format(ownedQuantityFormat, ownedQuantity);
        }

        public string FormatSlotQuantity(uint quantity)
        {
            return string.Format(slotQuantityFormat, quantity);
        }

        public string FormatDetailName(string displayName)
        {
            return string.Format(detailNameFormat, displayName);
        }

        public string FormatPurchaseSuccess(string displayName, uint quantity, uint totalPrice)
        {
            return string.Format(purchaseSuccessFormat, displayName, quantity, totalPrice);
        }

        public string FormatSellSuccess(string displayName, uint quantity, uint totalPrice)
        {
            return string.Format(sellSuccessFormat, displayName, quantity, totalPrice);
        }

        public string GetErrorMessage(ShopTransactionError error)
        {
            switch (error)
            {
                case ShopTransactionError.InvalidProduct:
                    return invalidProductMessage;
                case ShopTransactionError.InvalidQuantity:
                    return invalidQuantityMessage;
                case ShopTransactionError.InsufficientMoney:
                    return insufficientMoneyMessage;
                case ShopTransactionError.InventoryFull:
                    return inventoryFullMessage;
                case ShopTransactionError.TargetChanged:
                    return targetChangedMessage;
                case ShopTransactionError.NotSellable:
                    return notSellableMessage;
                case ShopTransactionError.EquippedWeapon:
                    return equippedWeaponMessage;
                case ShopTransactionError.RollbackFailed:
                    return rollbackFailedMessage;
                default:
                    return failedMessage;
            }
        }
    }
}
