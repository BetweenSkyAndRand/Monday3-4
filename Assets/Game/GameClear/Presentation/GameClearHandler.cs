using Game.Search;
using Game.Player;
using Game.Utility;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// ReSharper disable once CheckNamespace
namespace Game.GameClear
{
    /// <summary>
    /// ゲームクリア遷移を制御するクラス。
    /// IGameClearTrigger: ゲームクリア条件は未定（納品システムは廃止）。現在は呼び出し元がない。
    ///   クリア条件を決めたら TriggerGameClear() を呼ぶ口として残している。
    ///   納品カウントは周回を通じた累積。1インタラクトにつきパッチを1個納品する。
    /// ISceneTransitionHandler: SearchController の sceneTransitionHandlerObject にアサインして
    ///   探索終了でクリア扱いにしたい場合に使用できる（将来拡張）。
    ///
    /// 二重発火防止のため hasTriggered フラグで一度だけ遷移する。
    /// </summary>
    public class GameClearHandler : MonoBehaviour, IGameClearTrigger, ISceneTransitionHandler
    {
        [Header("遷移先シーン名")]
        [SerializeField] private string gameClearSceneName = "GameClearScene";

        [Header("遷移演出")]
        [SerializeField] private GameObject clearVfxPrefab;
        [SerializeField] private AudioClip clearSe;
        [SerializeField] private float fadeOutDuration = 1.5f;
        [SerializeField] private bool enableTransitionEffect = true;

        [Header("クリア確定メッセージ")]
        [SerializeField] private GameObject clearPromptRoot;
        [SerializeField] private TextMeshProUGUI clearPromptText;
        [SerializeField] private string clearPromptMessage = "クリア条件を達成しました。クリックでゲームクリア画面へ進みます。";

        [Header("入力制御")]
        [SerializeField] private MonoBehaviour gameplayInputControllerObject;

        /// <summary>二重遷移防止フラグ。TriggerGameClear() は一度だけ実行する。</summary>
        private bool hasTriggered;
        private IGameplayInputController gameplayInputController;

        private void Awake()
        {
            gameplayInputController = MonoBehaviourCaster.CastSingle<IGameplayInputController>(gameplayInputControllerObject, nameof(GameClearHandler));
            SetClearPromptVisible(false);
        }

        // ─── IGameClearTrigger ────────────────────────────────────────

        /// <summary>
        /// パッチ累積20個納品達成時に、松尾担当の納品システムから呼ばれる。
        /// 二重発火を防ぎつつ TransitionToEndScreen() へ委譲する。
        /// </summary>
        public void TriggerGameClear()
        {
            if (hasTriggered)
            {
                return;
            }

            hasTriggered = true;
            UnityEngine.Debug.Log("[GameClearHandler] ゲームクリア発火。確認クリック待機に入ります。");
            StartCoroutine(WaitForClickAndTransitionCoroutine());
        }

        // ─── ISceneTransitionHandler ──────────────────────────────────

        /// <summary>
        /// ゲームクリアシーンへ遷移する。
        /// SearchController の sceneTransitionHandlerObject にアサインすることで
        /// 探索終了時のクリア遷移にも使用できる（将来拡張用）。
        /// </summary>
        public void TransitionToEndScreen()
        {
            if (string.IsNullOrEmpty(gameClearSceneName))
            {
                UnityEngine.Debug.LogError("[GameClearHandler] gameClearSceneName が未設定です。Inspector で設定してください。");
                return;
            }

            if (enableTransitionEffect)
            {
                StartCoroutine(TransitionWithEffectCoroutine());
            }
            else
            {
                SceneManager.LoadScene(gameClearSceneName);
            }
        }

        /// <summary>演出付きでシーン遷移するコルーチン。</summary>
        private System.Collections.IEnumerator TransitionWithEffectCoroutine()
        {
            // クリアエフェクト再生
            if (clearVfxPrefab != null)
            {
                var vfxManager = Effect.VFXManager.Instance;
                if (vfxManager != null)
                {
                    vfxManager.PlayEffect(clearVfxPrefab, transform.position, Quaternion.identity);
                }
            }

            // SE再生
            if (clearSe != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    audioManager.PlaySe(clearSe);
                }
            }

            // フェードアウト（白）
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
            SceneManager.LoadScene(gameClearSceneName);
        }

        private System.Collections.IEnumerator WaitForClickAndTransitionCoroutine()
        {
            gameplayInputController?.DisableGameplayInputExceptPause();

            if (clearPromptRoot != null)
            {
                if (clearPromptText != null)
                {
                    clearPromptText.text = clearPromptMessage;
                }

                SetClearPromptVisible(true);
            }

            yield return new WaitUntil(IsLeftClickTriggered);

            SetClearPromptVisible(false);

            TransitionToEndScreen();
        }

        private static bool IsLeftClickTriggered()
        {
            return Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame;
        }

        private void SetClearPromptVisible(bool isVisible)
        {
            if (clearPromptRoot != null)
            {
                clearPromptRoot.SetActive(isVisible);
            }
        }
    }
}



