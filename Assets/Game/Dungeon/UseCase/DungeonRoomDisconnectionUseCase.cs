using System.Collections.Generic;
using UnityEngine;

// ReSharper disable once CheckNamespace
// プロジェクト規約でフォルダ階層より短い名前空間を使用している (例: Game.Dungeon)
namespace Game.Dungeon
{
    /// <summary>
    /// 崩壊対象部屋に接続していた隣接部屋（中央拠点含む）と、
    /// 各隣接部屋側で閉じるべき壁の方向を特定するユースケース。
    /// DungeonRoomConnectionUseCase（配置時の接続判定）と対になる、切断専用のユースケース。
    /// </summary>
    public class DungeonRoomDisconnectionUseCase
    {
        public readonly struct DisconnectionResult
        {
            /// <summary>閉じる壁を持つ隣接部屋のグリッド座標。</summary>
            public Vector2Int NeighborRoomGridPosition { get; }

            /// <summary>隣接部屋側で閉じるべき方向（崩壊対象部屋に面していた方向）。</summary>
            public Direction DirectionToClose { get; }

            public DisconnectionResult(Vector2Int neighborRoomGridPosition, Direction directionToClose)
            {
                NeighborRoomGridPosition = neighborRoomGridPosition;
                DirectionToClose = directionToClose;
            }
        }

        /// <summary>
        /// 崩壊対象部屋から見た4方向のオフセットと、
        /// その方向に隣接部屋が存在した場合に隣接部屋側で閉じるべき方向の対応表。
        /// 例：崩壊対象の北（up）に隣接部屋がある場合、その隣接部屋から見て崩壊対象は南にあるため、
        /// 隣接部屋側は South の壁を閉じる。
        /// </summary>
        private static readonly (Vector2Int offset, Direction directionToCloseOnNeighbor)[] neighborOffsets =
        {
            (Vector2Int.up,    Direction.South),
            (Vector2Int.down,  Direction.North),
            (Vector2Int.right, Direction.West),
            (Vector2Int.left,  Direction.East),
        };

        /// <summary>
        /// 崩壊対象部屋の削除前に呼び出すこと。
        /// 削除後は隣接判定に必要な情報が失われるため、判定は削除前に行う必要がある。
        /// </summary>
        /// <param name="targetRoomGridPosition">崩壊対象部屋のグリッド座標。</param>
        /// <param name="repository">配置済み部屋の読み取り専用リポジトリ。</param>
        public IReadOnlyList<DisconnectionResult> Execute(
            Vector2Int targetRoomGridPosition,
            IReadOnlyDungeonRoomRepository repository)
        {
            var results = new List<DisconnectionResult>();

            foreach (var (offset, directionToCloseOnNeighbor) in neighborOffsets)
            {
                var neighborGridPosition = targetRoomGridPosition + offset;
                if (!repository.TryGetRoomByGridPos(neighborGridPosition, out _))
                {
                    // 隣接部屋が存在しない方向は何もしない
                    continue;
                }

                results.Add(new DisconnectionResult(neighborGridPosition, directionToCloseOnNeighbor));
            }

            return results;
        }
    }
}

