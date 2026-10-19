using UnityEngine;

namespace Game.Audio
{
    /// <summary>
    /// 「サーチ前 BGM」を再生するハンドラー。
    /// シーン読み込み時（Start）に AudioManager 経由で BGM を再生する。
    /// SearchBgmHandler が再生する「探索開始 BGM」とは別 ID で管理する。
    ///
    /// 配置:
    ///   - GameScene の任意 GameObject にアタッチする（AudioManager より後の Start で OK）
    ///   - bgmId に GamePresentationSettings.bgmPresentations に登録した ID を指定
    /// </summary>
    public class PreSearchBgmStarter : MonoBehaviour
    {
        [Header("再生する BGM の ID")]
        [Tooltip("GamePresentationSettings の bgmPresentations に登録してある bgmId と一致させること。\n" +
                 "「サーチ前」の状態で流したい BGM を指定する。")]
        [SerializeField] private string bgmId = "pre_search_bgm";

        [Header("フェードイン設定")]
        [SerializeField] private bool useFadeIn = true;
        [SerializeField] private float fadeInDuration = 1f;

        [Header("再生タイミング")]
        [Tooltip("true の場合 Start で自動再生する。false の場合は外部から PlayPreSearchBgm() を呼ぶ。")]
        [SerializeField] private bool playOnStart = true;

        private void Start()
        {
            if (playOnStart)
            {
                PlayPreSearchBgm();
            }
        }

        /// <summary>
        /// サーチ前 BGM を再生する。外部から手動再生する場合にも使用できる。
        /// </summary>
        public void PlayPreSearchBgm()
        {
            var audioManager = AudioManager.Instance;
            if (audioManager == null)
            {
                UnityEngine.Debug.LogWarning("[PreSearchBgmStarter] AudioManager が見つかりません。BGM を再生できませんでした。");
                return;
            }

            audioManager.PlayConfiguredBgm(bgmId, useFadeIn, fadeInDuration);
        }
    }
}


