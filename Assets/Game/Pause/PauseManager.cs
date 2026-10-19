using UnityEngine;
using UnityEngine.SceneManagement;

namespace Game.Pause
{
    /// <summary>
    /// ゲーム全体のポーズ状態を管理するシングルトンクラス。
    /// Time.timeScale を制御し、ポーズ中は全ての時間依存処理を停止する。
    /// </summary>
    public class PauseManager : MonoBehaviour
    {
        private static PauseManager instance;
        private static bool isDestroying;

        /// <summary>シングルトンインスタンス</summary>
        public static PauseManager Instance
        {
            get
            {
                // 破棄処理中またはアプリ終了中は新しいインスタンスを作成しない
                if (isDestroying || !Application.isPlaying)
                {
                    return null;
                }
                
                if (instance == null)
                {
                    var go = new GameObject("PauseManager");
                    instance = go.AddComponent<PauseManager>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        /// <summary>現在ポーズ中かどうか</summary>
        public bool IsPaused { get; private set; }

        /// <summary>ポーズ状態が変化したときに通知するイベント</summary>
        public event System.Action<bool> OnPauseStateChanged;

        private void Awake()
        {
            if ( instance == null)
            {
                 instance = this;
                DontDestroyOnLoad(gameObject);
                
                // 初期状態を明示的に設定
                IsPaused = false;
                Time.timeScale = 1f;
                UnityEngine.Debug.Log("[PauseManager] 初期化完了 (IsPaused=false, Time.timeScale=1)");
                
            }
            else if ( instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        /// <summary>
        /// シーンが読み込まれたときの処理。
        /// タイトルシーンに戻ったらポーズ状態をリセットする（PauseManagerは再利用のため保持）。
        /// </summary>
        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // シーン遷移時にポーズ状態をリセット
            if (IsPaused)
            {
                // UnityEngine.Debug.Log($"[PauseManager] {scene.name}に遷移したため、ポーズ状態をリセットします");
                Resume();
            }
        }

        /// <summary>
        /// ゲームをポーズする。
        /// Time.timeScale を 0 にして時間を停止。
        /// </summary>
        public void Pause()
        {
            if (IsPaused)
            {
                return;
            }

            IsPaused = true;
            Time.timeScale = 0f;
            // UnityEngine.Debug.Log("[PauseManager] ゲームをポーズしました");
            OnPauseStateChanged?.Invoke(IsPaused);
        }

        /// <summary>
        /// ポーズを解除してゲームを再開する。
        /// Time.timeScale を 1 に戻す。
        /// </summary>
        public void Resume()
        {
            if (!IsPaused)
            {
                return;
            }

            IsPaused = false;
            Time.timeScale = 1f;
            // UnityEngine.Debug.Log("[PauseManager] ゲームを再開しました");
            OnPauseStateChanged?.Invoke(IsPaused);
        }

        /// <summary>
        /// ポーズ状態をトグルする（ESCキーなどで使用）。
        /// </summary>
        public void TogglePause()
        {
            if (IsPaused)
            {
                Resume();
            }
            else
            {
                Pause();
            }
        }

        /// <summary>
        /// アプリケーション終了時の処理。
        /// エディタ停止時またはアプリ終了時にのみ呼ばれる。
        /// </summary>
        private void OnApplicationQuit()
        {
            // UnityEngine.Debug.Log("[PauseManager] アプリケーション終了 - クリーンアップを開始します");
            
            // 破棄フラグをセット（OnDestroy中に新しいインスタンスが作成されないようにする）
            isDestroying = true;
            
            // イベントハンドラを解除
            SceneManager.sceneLoaded -= OnSceneLoaded;
            
            // Time.timeScaleを元に戻す
            Time.timeScale = 1f;
        }

        private void OnDestroy()
        {
            // シングルトンインスタンスがこのオブジェクトの場合、クリアする
            if (instance == this)
            {
                // UnityEngine.Debug.Log("[PauseManager] OnDestroy - インスタンスをクリアしました");
                instance = null;
            }
        }
    }
}




