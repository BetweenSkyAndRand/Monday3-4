using System;
using Game.Weapon;
using UnityEngine;
using UnityEngine.Serialization;

// ReSharper disable once CheckNamespace
namespace Game.Treasure
{
    /// <summary>
    /// 宝箱内アイテム 1 種類の出現率（万分率）を定義するエントリ。
    /// RoomObjectSpawner の treasureItemWeights 配列に並べることで、
    /// アイテムごとの出現確率を ratePer10000（0 ～ 10000）でコントロールできる。
    ///
    /// ─── 使い方 ───
    ///   各エントリに ratePer10000 を直接設定する（10000 = 100%）。
    ///   例: Weapon(10000) → 武器 100%
    ///   ratePer10000 が 0 以下のエントリはテーブルから除外される。
    /// </summary>
    [Serializable]
    public struct TreasureItemWeight
    {
        [Tooltip("宝箱の中身の種別（Weapon）")]
        public TreasureContentsType contentsType;

        [Tooltip("Weapon の場合に使用する WeaponData アセット")]
        public WeaponData weaponData;

        [Tooltip("出現率（万分率）。10000 = 100%、6000 = 60%。0 以下のエントリはテーブルから除外される")]
        [Min(0)]
        [FormerlySerializedAs("weight")]
        public int ratePer10000;
    }
}

