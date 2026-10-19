using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Game.GameClear
{
    /// <summary>
    /// ゲームクリア画面の制御クラス。
    /// 著作権表示と、タイトルに戻るボタンを管理する。
    /// </summary>
    public class GameClearView : MonoBehaviour
    {
        [SerializeField] private string titleSceneName = "TitleScene";
        [SerializeField] private Button returnToTitleButton;
        [SerializeField] private float fadeInDuration = 0.5f;

        private void Awake()
        {
            if (returnToTitleButton == null)
            {
                UnityEngine.Debug.LogWarning("[GameClearView] returnToTitleButton が未アサインです。Inspector で設定してください。");
                return;
            }

            returnToTitleButton.onClick.AddListener(ReturnToTitle);
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
            if (returnToTitleButton != null)
            {
                returnToTitleButton.onClick.RemoveListener(ReturnToTitle);
            }
        }

        /// <summary>
        /// タイトルに戻るボタン押下時に TitleScene を読み込む。
        /// </summary>
        private void ReturnToTitle()
        {
            SceneManager.LoadScene(titleSceneName);
        }
    }
}

