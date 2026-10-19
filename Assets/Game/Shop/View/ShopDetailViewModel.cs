using UnityEngine;

namespace Game.Shop
{
    /// <summary>右カラム（詳細・購入/売却操作）に表示する内容。</summary>
    public readonly struct ShopDetailViewModel
    {
        public bool HasSelection { get; }
        public Sprite Icon { get; }
        public Sprite RarityFrame { get; }
        public string DisplayName { get; }
        public string DescriptionText { get; }
        public string OwnedQuantityText { get; }
        public string UnitPriceText { get; }
        public string QuantityText { get; }
        public string TotalPriceText { get; }
        public string ActionLabel { get; }
        public bool CanExecute { get; }
        public string ReasonText { get; }

        public ShopDetailViewModel(
            bool hasSelection,
            Sprite icon,
            Sprite rarityFrame,
            string displayName,
            string descriptionText,
            string ownedQuantityText,
            string unitPriceText,
            string quantityText,
            string totalPriceText,
            string actionLabel,
            bool canExecute,
            string reasonText)
        {
            HasSelection = hasSelection;
            Icon = icon;
            RarityFrame = rarityFrame;
            DisplayName = displayName;
            DescriptionText = descriptionText;
            OwnedQuantityText = ownedQuantityText;
            UnitPriceText = unitPriceText;
            QuantityText = quantityText;
            TotalPriceText = totalPriceText;
            ActionLabel = actionLabel;
            CanExecute = canExecute;
            ReasonText = reasonText;
        }

        public static ShopDetailViewModel Empty()
        {
            return new ShopDetailViewModel(false, null, null, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, string.Empty, false, string.Empty);
        }
    }
}
