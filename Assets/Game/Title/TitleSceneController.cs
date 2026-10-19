using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.Title
{
    /// <summary>
    /// タイトル画面の制御クラス。
    /// スタートボタンのクリックで GameScene へ遷移する。
    /// 終了ボタンのクリックでゲームを終了する。
    /// </summary>
    public class TitleSceneController : MonoBehaviour
    {
        [SerializeField] private string gameSceneName = "GameScene";
        [SerializeField] private Button startButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private float fadeInDuration = 0.5f;
        [SerializeField] private float bgmFadeOutDuration = 0.5f;

        private void Awake()
        {
            if (startButton == null)
            {
                UnityEngine.Debug.LogWarning("[TitleSceneController] startButton が未アサインです。Inspector で設定してください。");
            }
            else
            {
                startButton.onClick.AddListener(StartGame);
            }

            if (quitButton == null)
            {
                UnityEngine.Debug.LogWarning("[TitleSceneController] quitButton が未アサインです。Inspector で設定してください。");
            }
            else
            {
                quitButton.onClick.AddListener(QuitGame);
            }
        }

        private void Start()
        {
            // ScreenEffectManager のフェードアウトで黒くなった画面をフェードインで解除する
            var screenEffectManager = Effect.ScreenEffectManager.Instance;
            if (screenEffectManager != null)
            {
                screenEffectManager.FadeIn(fadeInDuration);
            }
        }

        private void OnDestroy()
        {
            if (startButton != null)
            {
                startButton.onClick.RemoveListener(StartGame);
            }

            if (quitButton != null)
            {
                quitButton.onClick.RemoveListener(QuitGame);
            }
        }

        /// <summary>
        /// スタートボタン押下時に GameScene を読み込む。
        /// </summary>
        private void StartGame()
        {
            var audioManager = Audio.AudioManager.Instance;
            if (audioManager != null)
            {
                audioManager.StopBgmWithFadeOut(bgmFadeOutDuration);
            }

            SceneManager.LoadScene(gameSceneName);
        }

        /// <summary>
        /// 終了ボタン押下時にゲームを終了する。
        /// エディタ実行中は再生を停止し、ビルド実行時はアプリケーションを終了する。
        /// </summary>
        private void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            // UnityEngine.Debug.Log("[TitleSceneController] エディタの再生を停止しました");
#else
            Application.Quit();
            // UnityEngine.Debug.Log("[TitleSceneController] ゲームを終了します");
#endif
        }
    }
}

