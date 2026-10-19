using System;
using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// 初期配置時に各方向へ使用する壁の種類。
    /// </summary>
    public enum WallType
    {
        Big,
        Hole
    }

    /// <summary>
    /// 方向ごとの初期壁タイプを保持する設定クラス。
    /// DungeonPlacedRoomView の Inspector から各向きを個別に設定できる。
    /// </summary>
    [Serializable]
    public class DirectionalWallConfig
    {
        [SerializeField] private WallType north = WallType.Big;
        [SerializeField] private WallType south = WallType.Big;
        [SerializeField] private WallType east  = WallType.Big;
        [SerializeField] private WallType west  = WallType.Big;

        /// <summary>
        /// 指定した方向に対応する壁タイプを返す。
        /// </summary>
        public WallType Get(Direction direction)
        {
            return direction switch
            {
                Direction.North => north,
                Direction.South => south,
                Direction.East  => east,
                Direction.West  => west,
                _               => WallType.Big
            };
        }
    }

    /// <summary>
    /// 方向ごとにプレハブを保持する設定クラス。
    /// BigWall・HoleWall それぞれに対して Inspector から N/S/E/W を個別に設定できる。
    /// </summary>
    [Serializable]
    public class DirectionalWallPrefabs
    {
        [SerializeField] private GameObject north;
        [SerializeField] private GameObject south;
        [SerializeField] private GameObject east;
        [SerializeField] private GameObject west;

        /// <summary>
        /// 指定した方向に対応するプレハブを返す。
        /// </summary>
        public GameObject Get(Direction direction)
        {
            return direction switch
            {
                Direction.North => north,
                Direction.South => south,
                Direction.East  => east,
                Direction.West  => west,
                _               => null
            };
        }
    }
}
