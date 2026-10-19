using UnityEngine;

namespace Game.Player.Domain
{
    /// <summary>
    /// モデルのバウンディングボックスを計算する純粋C#クラス。
    /// 全ての子Rendererを統合したBoundsを取得する。
    /// </summary>
    public static class ModelBoundsCalculator
    {
        /// <summary>
        /// GameObjectの全Rendererを統合したBoundsを取得する。
        /// </summary>
        /// <param name="model">対象のGameObject</param>
        /// <param name="bounds">取得されたBounds</param>
        /// <returns>Rendererが存在し、有効なBoundsが取得できた場合true</returns>
        public static bool TryGetBounds(GameObject model, out ModelBounds bounds)
        {
            if (model == null)
            {
                bounds = default;
                return false;
            }

            // 全ての子Rendererを取得（非アクティブも含む）
            var renderers = model.GetComponentsInChildren<Renderer>(true);
            if (renderers.Length == 0)
            {
                bounds = default;
                return false;
            }

            // localScaleが1,1,1であることを確認（親の影響を排除）
            var originalScale = model.transform.localScale;
            var wasScaleModified = false;
            
            if (originalScale != Vector3.one)
            {
                UnityEngine.Debug.LogWarning($"[ModelBoundsCalculator] モデルのlocalScaleが1,1,1ではありません: {originalScale}。一時的にリセットします。");
                model.transform.localScale = Vector3.one;
                wasScaleModified = true;
            }

            // 最初のRendererのBoundsを基準にする
            var combinedBounds = renderers[0].bounds;

            // 残りのRendererのBoundsを統合
            for (var i = 1; i < renderers.Length; i++)
            {
                combinedBounds.Encapsulate(renderers[i].bounds);
            }

            // スケールを元に戻す
            if (wasScaleModified)
            {
                model.transform.localScale = originalScale;
            }

            // サイズが0または負の場合は無効
            if (combinedBounds.size.x <= 0 || combinedBounds.size.y <= 0 || combinedBounds.size.z <= 0)
            {
                bounds = default;
                return false;
            }

            bounds = new ModelBounds(
                combinedBounds.size,
                combinedBounds.center,
                combinedBounds.min,
                combinedBounds.max);

            UnityEngine.Debug.Log($"[ModelBoundsCalculator] Bounds取得成功 - Size: {bounds.Size}, Center: {bounds.Center}");
            return true;
        }

        /// <summary>
        /// スケール適用後のBoundsを再計算する。
        /// 地面合わせのオフセット計算に使用。
        /// </summary>
        public static bool TryGetBoundsAfterScale(GameObject model, Vector3 scale, out ModelBounds bounds)
        {
            if (model == null)
            {
                bounds = default;
                return false;
            }

            // 一時的にスケールを適用してBoundsを取得
            var originalScale = model.transform.localScale;
            model.transform.localScale = scale;

            var result = TryGetBounds(model, out bounds);

            // スケールを元に戻す
            model.transform.localScale = originalScale;

            return result;
        }
    }
}

