using Game.Search;
using UnityEngine;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace Game.GameOver
{
    /// <summary>
    /// ゲームオーバー遷移を制御するクラス。
    /// IGameOverTrigger: 治療費を支払えないときなどに TriggerGameOver() を呼んでもらう口。
    /// ISceneTransitionHandler: SearchController の sceneTransitionHandlerObject にアサインできる。
    ///
    /// 二重発火防止のため hasTriggered フラグで一度だけ遷移する。
    /// </summary>
    public class GameOverHandler : MonoBehaviour, IGameOverTrigger, ISceneTransitionHandler
    {
        [Header("遷移先シーン名")]
        [SerializeField] private string gameOverSceneName = "GameOverScene";

        [Header("遷移演出")]
        [SerializeField] private AudioClip gameOverSe;
        [SerializeField] private float fadeOutDuration = 1.5f;
        [SerializeField] private bool enableTransitionEffect = true;

        /// <summary>二重遷移防止フラグ。TriggerGameOver() は一度だけ実行する。</summary>
        private bool hasTriggered;

        // ─── IGameOverTrigger ─────────────────────────────────────────

        /// <summary>
        /// プレイヤーHPが0になったとき、PlayerStatusControllerから呼ばれる。
        /// 二重発火を防ぎつつ TransitionToEndScreen() へ委譲する。
        /// </summary>
        public void TriggerGameOver()
        {
            if (hasTriggered)
            {
                return;
            }

            hasTriggered = true;
            UnityEngine.Debug.Log("[GameOverHandler] ゲームオーバー発火。ゲームオーバーシーンへ遷移します。");
            TransitionToEndScreen();
        }

        // ─── ISceneTransitionHandler ──────────────────────────────

        /// <summary>
        /// ゲームオーバーシーンへ遷移する。
        /// PlayerStatusController から呼ばれるか、または SearchController の sceneTransitionHandlerObject にアサインすることで
        /// 探索終了時の遷移にも使用できる。
        /// </summary>
        public void TransitionToEndScreen()
        {
            if (string.IsNullOrEmpty(gameOverSceneName))
            {
                UnityEngine.Debug.LogError("[GameOverHandler] gameOverSceneName が未設定です。Inspector で設定してください。");
                return;
            }

            if (enableTransitionEffect)
            {
                StartCoroutine(TransitionWithEffectCoroutine());
            }
            else
            {
                SceneManager.LoadScene(gameOverSceneName);
            }
        }

        /// <summary>演出付きでシーン遷移するコルーチン。</summary>
        private System.Collections.IEnumerator TransitionWithEffectCoroutine()
        {
            // SE再生
            if (gameOverSe != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    audioManager.PlaySe(gameOverSe);
                }
            }

            // フェードアウト
            var screenEffectManager = Effect.ScreenEffectManager.Instance;
            if (screenEffectManager != null)
            {
                var fadeComplete = false;
                screenEffectManager.FadeOut(fadeOutDuration, () => fadeComplete = true);

                // フェード完了を待つ
                yield return new WaitUntil(() => fadeComplete);
            }
            else
            {
                // ScreenEffectManager がない場合は短い待機時間
                yield return new WaitForSeconds(0.5f);
            }

            // シーン遷移
            SceneManager.LoadScene(gameOverSceneName);
        }
    }
}

