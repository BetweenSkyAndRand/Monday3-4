using Game.Interaction;
using Game.Item;
using UnityEngine;

namespace Game.GroundItem
{
    /// <summary>
    /// 地面アイテムの見た目とインタラクト窓口。ロジック（消滅判定・取得可否）は持たず、取得は GroundItemRuntime に委ねる。
    /// グリッド占有には登録しない（プレイヤーの移動を妨げない）。
    /// </summary>
    public class GroundItemView : MonoBehaviour, IInteractableObject, IInteractPriority, IInteractDisplayProvider
    {
        // 他のインタラクト対象より先に選ばれるようにする
        private const int GroundItemInteractPriority = 100;

        private string displayName;
        private Sprite itemIcon;

        public string InstanceId { get; private set; }
        public string ItemId { get; private set; }
        public uint Quantity { get; private set; }
        public ItemRarity Rarity { get; private set; }
        public int InteractPriority => GroundItemInteractPriority;

        public void Initialize(string instanceId, string itemId, uint quantity, ItemRarity rarity, string itemDisplayName = null, Sprite icon = null)
        {
            Rarity = rarity;
            InstanceId = instanceId;
            ItemId = itemId;
            Quantity = quantity;
            displayName = string.IsNullOrEmpty(itemDisplayName) ? itemId : itemDisplayName;
            itemIcon = icon;
        }

        // レア度は枠で判別するため文字では出さない。数量はカード側で表示する。
        public string GetInteractText()
        {
            return $"Pick Up {displayName}";
        }

        public ItemIconDisplayData GetInteractDisplayData()
        {
            return new ItemIconDisplayData(itemIcon, Rarity, Quantity);
        }

        public void Interact()
        {
            GroundItemRuntime.TryPickup(InstanceId);
        }
    }
}