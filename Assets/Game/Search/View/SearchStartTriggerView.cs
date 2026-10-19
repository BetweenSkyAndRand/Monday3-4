using UnityEngine;
namespace Game.Search
{
    /// <summary>
    /// 初期部屋の出口に配置する透明トリガー。
    /// プレイヤーが通過したことを検知して SearchController に探索開始を通知する。
    /// Renderer 不要。Collider の Is Trigger = true に設定すること。
    /// </summary>
    public class SearchStartTriggerView : MonoBehaviour
    {
        [SerializeField] private SearchController searchController;
        [SerializeField] private string playerTag = "Player";

        private Collider triggerCollider;
        private bool hasTriggered;

        private void Awake()
        {
            triggerCollider = GetComponent<Collider>();
            if (triggerCollider == null)
            {
                UnityEngine.Debug.LogError("[SearchStartTriggerView] Collider コンポーネントが見つかりません。");
            }
        }

        private void OnTriggerEnter(Collider other)
        {
            if (hasTriggered)
            {
                return;
            }

            if (!other.CompareTag(playerTag))
            {
                return;
            }

            if (searchController == null)
            {
                UnityEngine.Debug.LogError("[SearchStartTriggerView] searchController が設定されていません。");
                return;
            }

            UnityEngine.Debug.Log($"[SearchStartTriggerView] 探索開始トリガー発火。検知オブジェクト: {other.gameObject.name}");
            hasTriggered = true;
            searchController.RequestSearchStart();
        }

        /// <summary>
        /// トリガーをリセットして再度発火可能にする。
        /// 探索終了後に SearchController から呼ばれる。
        /// </summary>
        public void ResetTrigger()
        {
            hasTriggered = false;
            if (triggerCollider != null)
            {
                triggerCollider.enabled = true;
            }
            // UnityEngine.Debug.Log("[SearchStartTriggerView] トリガーをリセットしました。");
        }

        /// <summary>
        /// トリガーを無効化して発火できなくする。
        /// ダンジョンが未生成のときに SearchController から呼ばれる。
        /// </summary>
        public void DisableTrigger()
        {
            hasTriggered = true;
            if (triggerCollider != null)
            {
                triggerCollider.enabled = false;
            }
        }
    }
}
