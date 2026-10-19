using Game.Item;
using Game.Item.Parameter;
using UnityEngine;

// ReSharper disable once CheckNamespace
// プロジェクト規約でフォルダ階層より短い名前空間を使用している (例: Game.Shop)
namespace Game.Shop
{
    /// <summary>
    /// ショップで販売する商品1件分のデータ。購入価格は ItemParameter.PurchasePrice を使う。
    /// 商品は ItemParameter + レア度で識別し、購入したアイテムのレア度は常に rarity で固定される。
    /// </summary>
    [CreateAssetMenu(fileName = "ShopItemData", menuName = "Game/Shop/ShopItemData")]
    public class ShopItemData : ScriptableObject
    {
        [Tooltip("販売するアイテム。購入価格はこの ItemParameter の purchasePrice を使う。")]
        [SerializeField] private ItemParameter itemParameter;

        [Tooltip("購入時に付与されるレア度。ItemParameter 側でこのレア度が有効になっている必要がある。")]
        [SerializeField] private ItemRarity rarity = ItemRarity.N;

        [Tooltip("false の場合、一覧に表示しない。")]
        [SerializeField] private bool isPurchasable = true;

        [Tooltip("将来の商品解放用。現仕様では評価しない。")]
        [SerializeField] private string unlockConditionId;

        public ItemParameter ItemParameter => itemParameter;
        public ItemRarity Rarity => rarity;
        public bool IsPurchasable => isPurchasable;
        public string UnlockConditionId => unlockConditionId;
    }
}
