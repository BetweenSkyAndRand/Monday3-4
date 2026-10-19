using System.Collections.Generic;

namespace Game.Shop
{
    /// <summary>商品データ（ScriptableObject）から、一覧に表示できる商品だけを取り出す。</summary>
    public class ShopCatalogUseCase
    {
        public List<ShopProductEntity> BuildProducts(IReadOnlyList<ShopItemData> shopItems)
        {
            var products = new List<ShopProductEntity>();
            if (shopItems == null)
            {
                return products;
            }

            foreach (var shopItem in shopItems)
            {
                if (shopItem == null || !shopItem.IsPurchasable || shopItem.ItemParameter == null)
                {
                    continue;
                }

                var itemParameter = shopItem.ItemParameter;
                if (!itemParameter.IsRarityEnabled(shopItem.Rarity))
                {
                    continue;
                }

                // 購入価格 0 は設定漏れとして扱い、無料で入手できないよう一覧から除外する
                if (itemParameter.PurchasePrice == 0)
                {
                    continue;
                }

                products.Add(new ShopProductEntity(itemParameter, shopItem.Rarity, itemParameter.PurchasePrice));
            }

            return products;
        }
    }
}
