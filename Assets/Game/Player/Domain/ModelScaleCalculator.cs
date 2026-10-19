using UnityEngine;

namespace Game.Player.Domain
{
    /// <summary>
    /// モデルのスケールを計算する純粋C#クラス。
    /// ターゲットサイズに合わせてスケール係数を算出する。
    /// </summary>
    public static class ModelScaleCalculator
    {
        private const float MinSize = 0.0001f;
        private const float MinScaleClamp = 0.01f;
        private const float MaxScaleClamp = 100f;

        /// <summary>
        /// ソースモデルのBoundsとターゲットサイズからスケールを計算する。
        /// </summary>
        /// <param name="sourceBounds">元のモデルのBounds</param>
        /// <param name="targetSize">目標サイズ (x=幅, y=高さ, z=奥行)</param>
        /// <param name="preserveAspectRatio">アスペクト比を維持するか</param>
        /// <returns>計算されたスケール結果</returns>
        public static ScaleResult Calculate(
            ModelBounds sourceBounds,
            Vector3 targetSize,
            bool preserveAspectRatio)
        {
            // Boundsが有効かチェック
            if (!sourceBounds.IsValid)
            {
                return ScaleResult.Failure("モデルのBoundsが無効です");
            }

            // 目標サイズが有効かチェック
            if (targetSize.x <= 0 || targetSize.y <= 0 || targetSize.z <= 0)
            {
                return ScaleResult.Failure("目標サイズが無効です");
            }

            // 各軸のスケール係数を計算
            var scaleX = targetSize.x / Mathf.Max(sourceBounds.Size.x, MinSize);
            var scaleY = targetSize.y / Mathf.Max(sourceBounds.Size.y, MinSize);
            var scaleZ = targetSize.z / Mathf.Max(sourceBounds.Size.z, MinSize);

            UnityEngine.Debug.Log($"[ModelScaleCalculator] 各軸のスケール係数 - X: {scaleX:F3}, Y: {scaleY:F3}, Z: {scaleZ:F3}");

            Vector3 finalScale;

            if (preserveAspectRatio)
            {
                // アスペクト比を維持する場合、最小係数を使用（形を崩さない）
                var uniformScale = Mathf.Min(scaleX, scaleY, scaleZ);
                uniformScale = Mathf.Clamp(uniformScale, MinScaleClamp, MaxScaleClamp);
                finalScale = Vector3.one * uniformScale;
                
                UnityEngine.Debug.Log($"[ModelScaleCalculator] アスペクト比維持モード - 統一スケール: {uniformScale:F3}");
            }
            else
            {
                // アスペクト比を無視する場合、各軸独立にスケール
                finalScale = new Vector3(
                    Mathf.Clamp(scaleX, MinScaleClamp, MaxScaleClamp),
                    Mathf.Clamp(scaleY, MinScaleClamp, MaxScaleClamp),
                    Mathf.Clamp(scaleZ, MinScaleClamp, MaxScaleClamp));
                
                UnityEngine.Debug.Log($"[ModelScaleCalculator] アスペクト比無視モード - 個別スケール: {finalScale}");
            }

            return ScaleResult.Success(finalScale, Vector3.zero);
        }

        /// <summary>
        /// 地面合わせのための位置オフセットを計算する。
        /// モデルの底面をY=0に合わせる。
        /// </summary>
        /// <param name="boundsAfterScale">スケール適用後のBounds</param>
        /// <param name="parentY">親オブジェクトのY座標</param>
        /// <returns>適用すべき位置オフセット</returns>
        public static Vector3 CalculateGroundAlignmentOffset(ModelBounds boundsAfterScale, float parentY)
        {
            if (!boundsAfterScale.IsValid)
            {
                return Vector3.zero;
            }

            // モデルの底面Y座標
            var bottomY = boundsAfterScale.Min.y;

            // 親のY座標に合わせるためのオフセット
            var offsetY = parentY - bottomY;

            return Vector3.up * offsetY;
        }
    }
}

