using Game.Weapon;
// ReSharper disable once CheckNamespace
namespace Game.Treasure
{
    /// <summary>
    /// 宝箱 1 個の中身を表す値オブジェクト。
    /// スポーン時に RoomObjectSpawner が生成して TreasureBoxController に渡す。
    /// 現状の中身は武器のみ。宝箱の中身になるアイテムは別途追加予定。
    /// </summary>
    public class TreasureContents
    {
        /// <summary>中身の種別。</summary>
        public TreasureContentsType ContentsType { get; }
        /// <summary>武器の場合にセットされる。中身が空の場合は null。</summary>
        public WeaponData WeaponData { get; }
        /// <summary>武器用コンストラクタ。</summary>
        public TreasureContents(WeaponData weaponData)
        {
            ContentsType = TreasureContentsType.Weapon;
            WeaponData = weaponData;
        }
    }
}