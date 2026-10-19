using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Shop
{
    /// <summary>
    /// ショップの商品一覧（表示順 = リストの並び順）と購入数の上限を管理する ScriptableObject。
    /// </summary>
    [CreateAssetMenu(fileName = "ShopCatalogParameter", menuName = "Game/Shop/ShopCatalogParameter")]
    public class ShopCatalogParameter : ScriptableObject
    {
        [Tooltip("商品一覧。上から順に左カラムへ表示される。")]
        [SerializeField] private List<ShopItemData> shopItems = new List<ShopItemData>();

        [Tooltip("1回の購入で指定できる最大数量。")]
        [Min(1)]
        [SerializeField] private uint maxPurchaseQuantityPerTransaction = 99;

        public IReadOnlyList<ShopItemData> ShopItems => shopItems;
        public uint MaxPurchaseQuantityPerTransaction => maxPurchaseQuantityPerTransaction < 1 ? 1u : maxPurchaseQuantityPerTransaction;

        private void OnValidate()
        {
            for (var itemIndex = 0; itemIndex < shopItems.Count; itemIndex++)
            {
                var shopItem = shopItems[itemIndex];
                if (shopItem == null || shopItem.ItemParameter == null)
                {
                    UnityEngine.Debug.LogWarning($"[ShopCatalogParameter] shopItems[{itemIndex}] または ItemParameter が未設定です。", this);
                    continue;
                }

                if (!shopItem.ItemParameter.IsRarityEnabled(shopItem.Rarity))
                {
                    UnityEngine.Debug.LogWarning($"[ShopCatalogParameter] {shopItem.name}: レア度 {shopItem.Rarity} は {shopItem.ItemParameter.ItemId} で無効です。", this);
                }

                if (shopItem.ItemParameter.PurchasePrice == 0)
                {
                    UnityEngine.Debug.LogWarning($"[ShopCatalogParameter] {shopItem.name}: 購入価格が 0 のため一覧に表示されません。", this);
                }
            }
        }
    }
}
