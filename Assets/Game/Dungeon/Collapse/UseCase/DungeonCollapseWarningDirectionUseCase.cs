using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// プレイヤーから見た崩壊対象部屋の「画面上の方向（連続角度）」と「正規化距離」を算出するユースケース。
    /// MonoBehaviour や Camera には依存せず、必要な値はすべて引数で受け取る純粋 C# クラスとして実装している。
    /// </summary>
    public class DungeonCollapseWarningDirectionUseCase
    {
        /// <summary>方向ベクトルが極端に短い場合に「真上」とみなすための閾値。</summary>
        private const float MinimumDirectionSqrMagnitude = 0.0001f;

        /// <summary>
        /// 警告表示用のエンティティを生成する。
        /// </summary>
        /// <param name="playerWorldPosition">プレイヤーのワールド座標。</param>
        /// <param name="targetRoomWorldCenter">崩壊対象部屋の中心ワールド座標。</param>
        /// <param name="cameraRightDirection">カメラの右方向ベクトル（画面右がワールドのどちらを向いているか）。</param>
        /// <param name="isPlayerInsideTargetRoom">プレイヤーが崩壊対象部屋の中にいるか。</param>
        /// <param name="maxWarningDistance">この距離以上離れていれば正規化距離を 1（最も遠い）として扱う。</param>
        public DungeonCollapseWarningEntity Execute(
            Vector3 playerWorldPosition,
            Vector3 targetRoomWorldCenter,
            Vector3 cameraRightDirection,
            bool isPlayerInsideTargetRoom,
            float maxWarningDistance)
        {
            var toTarget = targetRoomWorldCenter - playerWorldPosition;
            toTarget.y = 0f;

            var normalizedDistance = CalculateNormalizedDistance(toTarget.magnitude, maxWarningDistance);

            if (isPlayerInsideTargetRoom || toTarget.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                return new DungeonCollapseWarningEntity(isPlayerInsideTargetRoom, 0f, normalizedDistance);
            }

            var screenAngle = CalculateScreenAngleDegrees(toTarget.normalized, cameraRightDirection);
            return new DungeonCollapseWarningEntity(false, screenAngle, normalizedDistance);
        }

        /// <summary>
        /// 水平面に投影した方向ベクトルを、カメラ基準の「画面上での角度」に変換する。
        /// 画面上方向を 0 度、時計回りを正とする 0〜360 度で返す。
        /// </summary>
        private static float CalculateScreenAngleDegrees(Vector3 horizontalDirection, Vector3 cameraRightDirection)
        {
            var screenRight = new Vector3(cameraRightDirection.x, 0f, cameraRightDirection.z);
            if (screenRight.sqrMagnitude < MinimumDirectionSqrMagnitude)
            {
                // カメラが真横に回転している等で右方向が水平成分を持たない場合はワールド右を代用する
                screenRight = Vector3.right;
            }

            screenRight.Normalize();

            // 見下ろし視点でもカメラの俯角に影響されないよう、画面奥方向は右方向から導出する
            var screenForward = Vector3.Cross(screenRight, Vector3.up);

            var horizontalComponent = Vector3.Dot(horizontalDirection, screenRight);
            var verticalComponent = Vector3.Dot(horizontalDirection, screenForward);

            var angle = Mathf.Atan2(horizontalComponent, verticalComponent) * Mathf.Rad2Deg;
            return angle < 0f ? angle + 360f : angle;
        }

        private static float CalculateNormalizedDistance(float distance, float maxWarningDistance)
        {
            if (maxWarningDistance <= 0f)
            {
                return 0f;
            }

            return Mathf.Clamp01(distance / maxWarningDistance);
        }
    }
}
