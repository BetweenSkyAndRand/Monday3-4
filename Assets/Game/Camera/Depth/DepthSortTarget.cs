using UnityEngine;

namespace Game.CameraSystem.Depth
{
    /// <summary>
    /// カメラ距離深度ソートの対象を示すマーカーコンポーネント。
    /// 深度ソートさせたい GameObject にアタッチする。
    ///
    /// CameraDistanceDepthSorter がシーン内の全 DepthSortTarget を収集し、
    /// カメラとの距離に基づいて targetRenderer.sortingOrder を毎フレーム更新する。
    /// 遠いオブジェクトは sortingOrder が低く（奥に描画）、
    /// 近いオブジェクトは sortingOrder が高く（手前に描画）なる。
    /// </summary>
    public class DepthSortTarget : MonoBehaviour
    {
        [Header("ソートオーダーを操作する Renderer（未設定時は GetComponent で自動取得）")]
        [SerializeField] private Renderer targetRenderer;

        [Header("操作する SortingLayer 名（空の場合は変更しない）")]
        [SerializeField] private string sortingLayerName = "Default";

        /// <summary>深度ソートで制御する Renderer。</summary>
        public Renderer TargetRenderer => targetRenderer;

        /// <summary>操作する SortingLayer 名。</summary>
        public string SortingLayerName => sortingLayerName;

        private void Awake()
        {
            if (targetRenderer != null)
            {
                return;
            }

            targetRenderer = GetComponent<Renderer>();

            if (targetRenderer == null)
            {
                UnityEngine.Debug.LogWarning(
                    $"[DepthSortTarget] Renderer が見つかりません。targetRenderer を手動でアサインしてください: {name}", this);
            }
        }

        private void OnEnable()
        {
            // シーン内の CameraDistanceDepthSorter に自分を登録する
            var sorter = FindFirstObjectByType<Game.CameraSystem.Depth.CameraDistanceDepthSorter>();
            if (sorter != null)
            {
                sorter.RegisterTarget(this);
            }
        }

        private void OnDisable()
        {
            // 無効化・破棄時は登録を解除する
            var sorter = FindFirstObjectByType<Game.CameraSystem.Depth.CameraDistanceDepthSorter>();
            if (sorter != null)
            {
                sorter.UnregisterTarget(this);
            }
        }
    }
}
