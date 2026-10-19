using UnityEngine;
using UnityEngine.UI;

namespace Game.Search
{
    /// <summary>
    /// 探索終了ボタンの UI View。
    /// ボタン押下を SearchController に転送するだけで、終了ロジックは持たない。
    /// インタラクト方式（SearchEndObject）を使う場合は本クラスは不要。
    /// </summary>
    public class SearchEndButtonView : MonoBehaviour
    {
        [SerializeField] private Button endButton;
        [SerializeField] private SearchController searchController;

        private void Awake()
        {
            if (endButton == null)
            {
                UnityEngine.Debug.LogError("[SearchEndButtonView] endButton が設定されていません。");
                return;
            }

            endButton.onClick.AddListener(OnEndButtonClicked);
        }

        private void OnEndButtonClicked()
        {
            if (searchController == null)
            {
                UnityEngine.Debug.LogError("[SearchEndButtonView] searchController が設定されていません。");
                return;
            }

            searchController.RequestSearchEnd();
        }
    }
}