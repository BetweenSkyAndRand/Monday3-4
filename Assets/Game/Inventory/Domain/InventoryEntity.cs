using System.Collections.Generic;

namespace Game.Inventory
{
    /// <summary>
    /// Minecraftスタイルのホットバー型インベントリのスナップショット。
    /// 装備専用スロットは持たず、N個のアイテムスロットだけを持つ。
    /// プレイヤーが選択しているスロット（ActiveSlotIndex）のアイテムが武器なら、
    /// それが現在装備中の武器として扱われる（EquippedWeaponSlot は派生プロパティ）。
    /// </summary>
    public readonly struct InventoryEntity
    {
        public IReadOnlyList<InventorySlotEntity> ItemSlots { get; }

        /// <summary>
        /// 現在選択中のスロットインデックス（0始まり）。-1 は未選択。
        /// 1〜N キーまたはマウスクリックで更新される。
        /// </summary>
        public int ActiveSlotIndex { get; }

        public uint Version { get; }

        /// <summary>
        /// 選択中スロットに武器があればそれを返す（旧API互換の派生プロパティ）。
        /// それ以外（未選択・空スロット・パッチ選択中）の場合は null。
        /// </summary>
        public InventoryItemEntity? EquippedWeaponSlot
        {
            get
            {
                if (ItemSlots == null)
                {
                    return null;
                }

                if (ActiveSlotIndex < 0 || ActiveSlotIndex >= ItemSlots.Count)
                {
                    return null;
                }

                var slot = ItemSlots[ActiveSlotIndex];
                if (!slot.Item.HasValue)
                {
                    return null;
                }

                if (slot.Item.Value.ItemType != InventoryItemType.Weapon)
                {
                    return null;
                }

                return slot.Item.Value;
            }
        }

        public InventoryEntity(
            IReadOnlyList<InventorySlotEntity> itemSlots,
            int activeSlotIndex,
            uint version)
        {
            ItemSlots = itemSlots;
            ActiveSlotIndex = activeSlotIndex;
            Version = version;
        }
    }
}

