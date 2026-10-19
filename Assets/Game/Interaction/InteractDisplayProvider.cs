using Game.Item;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Interaction
{
    /// <summary>
    /// インタラクト UI のカードに表示するアイコン情報。
    /// アイコンの取得元は ItemParameter に統一し、枠はレア度から UI 側で決める。
    /// </summary>
    public readonly struct ItemIconDisplayData
    {
        public readonly Sprite Icon;
        public readonly ItemRarity Rarity;
        public readonly uint Quantity;

        public ItemIconDisplayData(Sprite icon, ItemRarity rarity, uint quantity)
        {
            Icon = icon;
            Rarity = rarity;
            Quantity = quantity;
        }
    }

    /// <summary>
    /// IInteractableObject の追加機能。アイコン付きで表示したい対象が実装する。
    /// </summary>
    public interface IInteractDisplayProvider
    {
        ItemIconDisplayData GetInteractDisplayData();
    }
}
