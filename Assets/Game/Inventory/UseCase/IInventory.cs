using Game.Weapon;

namespace Game.Inventory
{
    public interface IInventory
    {
        InventoryUseCaseResult TryAddWeapon(WeaponData weaponData);

        /// <summary>
        /// 任意カテゴリのアイテムを追加する。同一 ItemId のスタックを上限まで埋め、残りを空きスロットへ置く。
        /// 全量を格納できない場合は何も変更せず InventoryFull を返す（全部入るか、全く入らないか）。
        /// </summary>
        InventoryUseCaseResult TryAddItem(InventoryItemSpec itemSpec, uint quantity);

        /// <summary>
        /// 指定アイテムを追加できる最大数を返す（既存スタックの空き + 空きスロット × スタック上限）。
        /// 武器は 1 スロット 1 個固定のため、空きスロット数になる。インベントリは変更しない。
        /// </summary>
        uint GetAddableQuantity(InventoryItemSpec itemSpec);

        InventoryUseCaseResult TryConsumeWeapon(int itemSlotIndex);

        /// <summary>指定スロットのアイテムをスタックごと取り除く（武器以外にも使用できる）。</summary>
        InventoryUseCaseResult TryRemoveItemAt(int itemSlotIndex);

        /// <summary>指定スロットから指定した数量を取り除く。</summary>
        InventoryUseCaseResult TryRemoveItemAt(int itemSlotIndex, uint quantity);

        InventoryEntity GetCurrentInventory();

        /// <summary>
        /// 1〜4キーで最後に選択されたアイテムスロットのインデックス（0始まり）。
        /// 未選択の場合は -1 を返す。
        /// StorageInventoryController からスロット指定の預け入れ/引き出しに使用する。
        /// </summary>
        int GetActiveItemSlotIndex();
    }
}