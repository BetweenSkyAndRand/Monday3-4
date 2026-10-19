using UnityEngine;
using Game.BuildMode;
using Game.Player.Script;

namespace Game.Pause
{
    /// <summary>
    /// ポーズキー入力を受け取ってPauseManagerを制御するクラス。
    /// GameInputReaderのOnPauseイベントを購読し、ポーズ状態をトグルする。
    /// </summary>
    public class PauseInputHandler : MonoBehaviour
    {
        [SerializeField] private GameInputReader gameInputReader;

        private void Awake()
        {
            if (gameInputReader != null)
            {
                // UnityEngine.Debug.Log("[PauseInputHandler] GameInputReader が正常に設定されました");
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PauseInputHandler] GameInputReader が見つかりません");
            }
        }

        private void OnEnable()
        {
            if (gameInputReader != null)
            {
                gameInputReader.OnPause += HandlePauseInput;
                // UnityEngine.Debug.Log("[PauseInputHandler] OnPause イベントを購読しました");
            }
        }

        private void OnDisable()
        {
            if (gameInputReader != null)
            {
                gameInputReader.OnPause -= HandlePauseInput;
                // UnityEngine.Debug.Log("[PauseInputHandler] OnPause イベントの購読を解除しました");
            }
        }

        /// <summary>
        /// ポーズ入力を受け取り、PauseManagerに転送する。
        /// </summary>
        private void HandlePauseInput()
        {
            // 部屋配置中はポーズさせない（崩壊タイマーを止めて配置を考える抜け道を防ぐため）。
            // すでにポーズ中の解除は妨げない。
            // 探索終了の確認ダイアログ表示中は、ポーズ解除時に時間停止が食い違わないようポーズさせない
            if (Game.Search.SearchEndConfirmDialogView.IsOpen)
            {
                return;
            }

            var isBuilding = BuildModeRepository.GetBuildMode() == BuildModeType.Building;
            var isAlreadyPaused = PauseManager.Instance != null && PauseManager.Instance.IsPaused;
            if (isBuilding && !isAlreadyPaused)
            {
                return;
            }

            if (PauseManager.Instance != null)
            {
                // UnityEngine.Debug.Log("[PauseInputHandler] ポーズ入力を受信しました - PauseManager.TogglePause() を実行");
                PauseManager.Instance.TogglePause();
            }
            else
            {
                UnityEngine.Debug.LogWarning("[PauseInputHandler] PauseManager が存在しないため、ポーズ処理をスキップしました");
            }
        }
    }
}


