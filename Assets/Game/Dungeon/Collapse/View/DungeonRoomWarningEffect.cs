using System.Collections;
using UnityEngine;

// ReSharper disable once CheckNamespace
namespace Game.Dungeon
{
    /// <summary>
    /// 崩壊警告時に対象部屋のみを発光させる演出コンポーネント。
    /// DungeonCollapseController が対象の DungeonPlacedRoomView の GameObject に
    /// 動的に AddComponent して使用する（既存プレハブの変更は不要）。
    /// </summary>
    public class DungeonRoomWarningEffect : MonoBehaviour
    {
        [Tooltip("発光ライトの設置高さオフセット。")]
        [SerializeField] private float lightHeightOffset = 1.5f;

        [Tooltip("発光ライトの到達範囲。")]
        [SerializeField] private float lightRange = 15f;

        private Light warningLight;
        private Coroutine glowCoroutine;

        /// <summary>
        /// 警告発光を開始する。既に発光中の場合は一度停止してから再生する。
        /// </summary>
        public void StartWarning(Color glowColor, float duration, float pulseSpeed)
        {
            StopWarning();
            glowCoroutine = StartCoroutine(GlowCoroutine(glowColor, duration, pulseSpeed));
        }

        /// <summary>
        /// 警告発光を停止し、生成したライトを破棄する。
        /// 崩壊実行時・探索終了時（リセット）に呼ばれる。
        /// </summary>
        public void StopWarning()
        {
            if (glowCoroutine != null)
            {
                StopCoroutine(glowCoroutine);
                glowCoroutine = null;
            }

            if (warningLight != null)
            {
                Destroy(warningLight.gameObject);
                warningLight = null;
            }
        }

        private IEnumerator GlowCoroutine(Color glowColor, float duration, float pulseSpeed)
        {
            var lightGo = new GameObject("CollapseWarningLight");
            lightGo.transform.SetParent(transform);
            lightGo.transform.localPosition = Vector3.up * lightHeightOffset;

            warningLight = lightGo.AddComponent<Light>();
            warningLight.type = LightType.Point;
            warningLight.color = glowColor;
            warningLight.range = lightRange;
            warningLight.shadows = LightShadows.None;

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                // sin波で明滅させることで「対象部屋のみ光る」警告を分かりやすく演出する
                var pulse = (Mathf.Sin(elapsed * pulseSpeed) + 1f) * 0.5f;
                warningLight.intensity = Mathf.Lerp(2f, 8f, pulse);
                yield return null;
            }

            StopWarning();
        }

        private void OnDestroy()
        {
            StopWarning();
        }
    }
}

