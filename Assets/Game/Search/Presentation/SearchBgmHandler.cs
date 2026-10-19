using Game.Audio;
using Game.Search;
using UnityEngine;

namespace Game.Search
{
    /// <summary>
    /// 探索開始時に BGM を再生する ISearchStartHandler 実装。
    /// SearchController の searchStartHandlerObjects に登録して使用する。
    ///
    /// ─── 設定手順 ───
    ///   1. GameScene の SearchController と同じ GameObject か任意の GameObject に本コンポーネントをアタッチ
    ///   2. bgmId に GamePresentationSettings.bgmPresentations に登録した bgmId を入力する
    ///   3. SearchController Inspector の searchStartHandlerObjects 配列に本コンポーネントを追加する
    /// </summary>
    public class SearchBgmHandler : MonoBehaviour, ISearchStartHandler
    {
        [Header("再生する BGM の ID")]
        [Tooltip("GamePresentationSettings の bgmPresentations に登録してある bgmId と一致させること")]
        [SerializeField] private string bgmId = "game_bgm";

        [Header("フェードイン設定")]
        [SerializeField] private bool useFadeIn = true;
        [SerializeField] private float fadeInDuration = 1f;

        /// <summary>
        /// SearchController.RequestSearchStart() から呼ばれる。
        /// AudioManager 経由で GamePresentationSettings 登録済み BGM を ID 指定で再生する。
        /// </summary>
        public void OnSearchStart()
        {
            var audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                UnityEngine.Debug.LogWarning("[SearchBgmHandler] AudioManager が見つかりません。BGM を再生できませんでした。");
                return;
            }

            UnityEngine.Debug.Log($"[SearchBgmHandler] 探索開始 BGM を再生: {bgmId}");
            audioManager.PlayConfiguredBgm(bgmId, useFadeIn, fadeInDuration);
        }
    }
}



