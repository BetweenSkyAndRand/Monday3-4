using System.Collections.Generic;
using UnityEngine;

namespace Game.CameraSystem.Depth
{
    /// <summary>
    /// シーン内の全 DepthSortTarget のレンダリングソートオーダーを管理するコンポーネント。
    /// シーン内の空 GameObject（または Camera GameObject）にアタッチする。
    ///
    /// 処理の流れ:
    ///   LateUpdate ごとに、各 DepthSortTarget のカメラからの距離を計算し、
    ///   遠いものほど sortingOrder を低く（奥に描画）、近いものほど高く（手前に描画）設定する。
    ///
    /// sortingOrder 計算式:
    ///   ratio = Clamp01(distance / maxSortDistance)
    ///   order = Lerp(+sortingOrderRange/2, -sortingOrderRange/2, ratio)
    ///   → ratio が 0（近い）なら最大値、1（遠い）なら最小値になる
    /// </summary>
    public class CameraDistanceDepthSorter : MonoBehaviour
    {
        [Header("距離計算に使用するカメラ（未設定時は Camera.main で自動取得）")]
        [SerializeField] private UnityEngine.Camera targetCamera;

        [Header("最大ソート距離（この距離以上は最小 sortingOrder になる）")]
        [SerializeField] private float maxSortDistance = 50f;

        [Header("sortingOrder の振り幅（+half ～ -half の範囲で設定される）")]
        [SerializeField] private int sortingOrderRange = 100;

        [Header("Start 時に全 DepthSortTarget を自動収集するか")]
        [SerializeField] private bool autoRefreshOnStart = true;

        /// <summary>深度ソート対象の一覧。</summary>
        private readonly List<DepthSortTarget> sortTargets = new List<DepthSortTarget>();

        private void Start()
        {
            if (targetCamera == null)
            {
                targetCamera = UnityEngine.Camera.main;
            }

            if (targetCamera == null)
            {
                UnityEngine.Debug.LogWarning("[CameraDistanceDepthSorter] カメラが見つかりません。", this);
            }

            if (autoRefreshOnStart)
            {
                RefreshTargets();
            }
        }

        private void LateUpdate()
        {
            if (targetCamera == null)
            {
                return;
            }

            UpdateSortingOrders();
        }

        /// <summary>
        /// シーン内の全 DepthSortTarget を再収集する。
        /// 動的スポーン後などに呼び出すことでリストを最新状態に保つ。
        /// </summary>
        public void RefreshTargets()
        {
            sortTargets.Clear();
            var found = FindObjectsByType<DepthSortTarget>(FindObjectsSortMode.None);
            foreach (var target in found)
            {
                sortTargets.Add(target);
            }
        }

        /// <summary>
        /// 個別のソート対象を登録する。
        /// DepthSortTarget.OnEnable から呼ばれる（動的スポーン対応）。
        /// </summary>
        public void RegisterTarget(DepthSortTarget target)
        {
            if (target == null || sortTargets.Contains(target))
            {
                return;
            }

            sortTargets.Add(target);
        }

        /// <summary>
        /// 個別のソート対象を削除する。
        /// DepthSortTarget.OnDisable から呼ばれる（破棄・無効化対応）。
        /// </summary>
        public void UnregisterTarget(DepthSortTarget target)
        {
            sortTargets.Remove(target);
        }

        /// <summary>
        /// 全ソート対象の sortingOrder をカメラ距離に基づいて更新する。
        /// </summary>
        private void UpdateSortingOrders()
        {
            var cameraPosition = targetCamera.transform.position;
            var halfRange = sortingOrderRange / 2;

            for (var i = sortTargets.Count - 1; i >= 0; i--)
            {
                var sortTarget = sortTargets[i];

                // 破棄済みエントリは削除する
                if (sortTarget == null)
                {
                    sortTargets.RemoveAt(i);
                    continue;
                }

                var targetRenderer = sortTarget.TargetRenderer;
                if (targetRenderer == null)
                {
                    continue;
                }

                var distance = Vector3.Distance(cameraPosition, sortTarget.transform.position);

                // 遠い(ratio → 1)ほど sortingOrder が低くなる（奥に描画）
                var ratio = Mathf.Clamp01(distance / Mathf.Max(maxSortDistance, 0.001f));
                var sortingOrder = Mathf.RoundToInt(Mathf.Lerp(halfRange, -halfRange, ratio));

                // SortingLayer 名が指定されている場合は合わせて設定する
                if (!string.IsNullOrEmpty(sortTarget.SortingLayerName))
                {
                    targetRenderer.sortingLayerName = sortTarget.SortingLayerName;
                }

                targetRenderer.sortingOrder = sortingOrder;
            }
        }
    }
}


