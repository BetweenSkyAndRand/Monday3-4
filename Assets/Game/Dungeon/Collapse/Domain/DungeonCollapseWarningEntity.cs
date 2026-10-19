// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// 崩壊警告の表示に必要な「方向」と「距離」を保持する不変エンティティ。
    /// View はこの値だけを参照して円環グローの角度・色・広がり・点滅速度を決定する。
    /// </summary>
    public readonly struct DungeonCollapseWarningEntity
    {
        /// <summary>プレイヤーが崩壊対象部屋の中にいるか。true の場合は方向指示を行わず全周を警告する。</summary>
        public bool IsPlayerInsideTargetRoom { get; }

        /// <summary>
        /// 画面上での崩壊対象方向。画面上方向を 0 度とし、時計回りを正とした 0〜360 度の連続値。
        /// <see cref="IsPlayerInsideTargetRoom"/> が true のときは意味を持たない。
        /// </summary>
        public float ScreenAngleDegrees { get; }

        /// <summary>プレイヤーと崩壊対象部屋の距離を 0（最も近い）〜1（十分遠い）に正規化した値。</summary>
        public float NormalizedDistance { get; }

        public DungeonCollapseWarningEntity(bool isPlayerInsideTargetRoom, float screenAngleDegrees, float normalizedDistance)
        {
            IsPlayerInsideTargetRoom = isPlayerInsideTargetRoom;
            ScreenAngleDegrees = screenAngleDegrees;
            NormalizedDistance = normalizedDistance;
        }
    }
}
