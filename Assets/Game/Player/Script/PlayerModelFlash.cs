using System.Collections;
using UnityEngine;

namespace Game.Player.Script
{
    /// <summary>
    /// プレイヤーモデルを一時的に発光させる（フラッシュ）クラス。
    /// PlayerHitEffect から分離され、モデル発光演出のみを担当する。
    /// 他のエフェクト（攻撃時の発光など）でも再利用可能。
    /// </summary>
    public class PlayerModelFlash : MonoBehaviour
    {
        [Header("モデル発光設定")]
        [SerializeField] private Renderer[] modelRenderers;
        [SerializeField] private Color flashColor = new Color(1f, 0.3f, 0.3f, 1f);
        [SerializeField] private float flashDuration = 0.1f;

        private Material[] originalMaterials;
        private Coroutine flashCoroutine;
        private static readonly int colorPropertyId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            // モデル発光用のマテリアルをキャッシュ
            if (modelRenderers != null && modelRenderers.Length > 0)
            {
                CacheOriginalMaterials();
            }
        }

        /// <summary>
        /// モデルを一時的に発光させる。
        /// 外部から呼び出される公開メソッド。
        /// </summary>
        public void Flash()
        {
            Flash(flashColor, flashDuration);
        }

        /// <summary>
        /// モデルを指定した色と時間で発光させる。
        /// カスタム色・時間で発光させたい場合に使用する。
        /// </summary>
        public void Flash(Color color, float duration)
        {
            if (modelRenderers == null || modelRenderers.Length == 0)
            {
                return;
            }

            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
            }

            flashCoroutine = StartCoroutine(FlashCoroutine(color, duration));
        }

        private void CacheOriginalMaterials()
        {
            var totalMaterialCount = 0;
            foreach (var targetRenderer in modelRenderers)
            {
                if (targetRenderer != null)
                {
                    totalMaterialCount += targetRenderer.materials.Length;
                }
            }

            originalMaterials = new Material[totalMaterialCount];
            var index = 0;

            foreach (var targetRenderer in modelRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                var materials = targetRenderer.materials;
                foreach (var material in materials)
                {
                    originalMaterials[index] = material;
                    index++;
                }
            }
        }

        private IEnumerator FlashCoroutine(Color color, float duration)
        {
            // 全マテリアルの色を一時的に変更
            foreach (var targetRenderer in modelRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                foreach (var material in targetRenderer.materials)
                {
                    if (material.HasProperty(colorPropertyId))
                    {
                        material.color = color;
                    }
                }
            }

            yield return new WaitForSeconds(duration);

            // 元の色に戻す
            var index = 0;
            foreach (var targetRenderer in modelRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                var materials = targetRenderer.materials;
                for (var i = 0; i < materials.Length; i++)
                {
                    if (index < originalMaterials.Length && originalMaterials[index] != null)
                    {
                        materials[i].color = originalMaterials[index].color;
                    }
                    index++;
                }
            }

            flashCoroutine = null;
        }

        private void OnDestroy()
        {
            if (flashCoroutine != null)
            {
                StopCoroutine(flashCoroutine);
                flashCoroutine = null;
            }
        }
    }
}

