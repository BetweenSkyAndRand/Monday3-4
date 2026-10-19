using UnityEngine;

namespace Game.CameraSystem.Transparency
{
    /// <summary>
    /// 壁透過フェードの受け手コンポーネント。壁プレハブの Root に付ける。
    ///
    /// WallOcclusionController がカメラ→プレイヤー間にこの壁が入ったと判定したとき
    /// SetFade(true) を呼び出し、transparentMaterial に差し替えて半透明にする。
    /// 視界が遮られなくなれば SetFade(false) で元のマテリアルに戻す。
    ///
    /// 注意: transparentMaterial には URP/Lit の SurfaceType=Transparent に設定した
    ///       マテリアルをアサインすること。Opaque マテリアルでは透明にならない。
    /// </summary>
    public class WallTransparencyTarget : MonoBehaviour
    {
        [Header("半透明時に差し替えるマテリアル（URP/Lit SurfaceType=Transparent）")]
        [SerializeField] private Material transparentMaterial;

        [Header("フェード対象の Renderer 一覧（空の場合は自動取得）")]
        [SerializeField] private Renderer[] targetRenderers;

        /// <summary>元のマテリアル配列（Renderer ごとに保存）</summary>
        private Material[][] originalMaterials;

        private bool isFading;

        private void Awake()
        {
            // 未設定の場合は子を含む全 Renderer を自動取得する
            if (targetRenderers == null || targetRenderers.Length == 0)
            {
                targetRenderers = GetComponentsInChildren<Renderer>();
            }

            // 元のマテリアルをキャッシュしておく（SetFade(false) で復元するため）
            originalMaterials = new Material[targetRenderers.Length][];
            for (var i = 0; i < targetRenderers.Length; i++)
            {
                originalMaterials[i] = targetRenderers[i].sharedMaterials;
            }
        }

        /// <summary>
        /// フェード状態を切り替える。
        /// true: 半透明マテリアルに切り替え / false: 元のマテリアルに戻す
        /// </summary>
        public void SetFade(bool fade)
        {
            // 状態が変わらない場合はスキップ（毎フレームの無駄な SetMaterials を防ぐ）
            if (isFading == fade)
            {
                return;
            }

            isFading = fade;

            for (var i = 0; i < targetRenderers.Length; i++)
            {
                if (targetRenderers[i] == null)
                {
                    continue;
                }

                if (fade)
                {
                    if (transparentMaterial == null)
                    {
                        UnityEngine.Debug.LogWarning(
                            $"[WallTransparencyTarget] transparentMaterial が未設定です: {name}", this);
                        return;
                    }

                    // 全スロットを透明マテリアルで埋める
                    var fadeMaterials = new Material[originalMaterials[i].Length];
                    for (var j = 0; j < fadeMaterials.Length; j++)
                    {
                        fadeMaterials[j] = transparentMaterial;
                    }

                    targetRenderers[i].materials = fadeMaterials;
                }
                else
                {
                    // SharedMaterials をコピーして Renderer に再アサインする
                    targetRenderers[i].materials = originalMaterials[i];
                }
            }
        }

        /// <summary>
        /// このコンポーネントが破棄されるとき、フェード中なら元に戻す。
        /// </summary>
        private void OnDestroy()
        {
            if (!isFading)
            {
                return;
            }

            SetFade(false);
        }
    }
}


