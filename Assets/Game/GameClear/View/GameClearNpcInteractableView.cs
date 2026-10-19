using Game.Interaction;
using Game.Money.Presentation;
using Game.Search;
using Game.Utility;
using UnityEngine;

namespace Game.GameClear
{
    /// <summary>
    /// クリア判定用NPCのインタラクトを提供する。
    /// 所持金が閾値以上かつ探索中でない場合にクリアを発火する。
    /// </summary>
    public class GameClearNpcInteractableView : MonoBehaviour, IInteractableObject
    {
        [Header("参照")]
        [SerializeField] private MoneyWalletController moneyWalletController;
        [SerializeField] private SearchController searchController;
        [SerializeField] private MonoBehaviour gameClearTriggerObject;

        [Header("クリア条件")]
        [SerializeField] private uint clearLineMoney = 3000;

        [Header("UI表示設定")]
        [SerializeField] private string interactText = "Talk";

        [Header("グリッド占有")]
        [SerializeField] private bool occupyGridCell = true;
        [SerializeField] private float cellSize = 1f;

        private IGameClearTrigger gameClearTrigger;

        private void Awake()
        {
            gameClearTrigger = MonoBehaviourCaster.CastSingle<IGameClearTrigger>(gameClearTriggerObject, nameof(GameClearNpcInteractableView));
        }

        private void Start()
        {
            if (!occupyGridCell)
            {
                return;
            }

            var gridPosition = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / cellSize),
                Mathf.RoundToInt(transform.position.z / cellSize));

            Game.Grid.GridOccupationManager.Occupy(gridPosition, gameObject);
        }

        private void OnDestroy()
        {
            if (occupyGridCell)
            {
                Game.Grid.GridOccupationManager.ReleaseByOccupant(gameObject);
            }
        }

        public string GetInteractText()
        {
            if (searchController == null)
            {
                return string.Empty;
            }

            if (searchController.IsSearchInProgress)
            {
                return string.Empty;
            }

            return interactText;
        }

        public void Interact()
        {
            if (searchController == null || searchController.IsSearchInProgress)
            {
                return;
            }

            if (moneyWalletController == null || gameClearTrigger == null)
            {
                return;
            }

            if (moneyWalletController.CurrentMoney < clearLineMoney)
            {
                // 仕様通り未達時は何も起こさない
                return;
            }

            gameClearTrigger.TriggerGameClear();
        }
    }
}


