using UnityEngine;

namespace Game.Dungeon.View
{
    /// <summary>
    /// 部屋配置エフェクトの再生状態を管理し、エフェクト再生中のプレイヤー移動を禁止する。
    /// DungeonPlacedRoomView から呼び出され、シングルトンとして動作する。
    /// </summary>
    public class RoomPlacementEffectController : MonoBehaviour, Player.IMovementBlocker
    {
        private static RoomPlacementEffectController instance;
        private static bool isDestroying;

        /// <summary>エフェクト再生中かどうか。</summary>
        private bool isPlayingEffect;

        /// <summary>エフェクト終了予定時刻。</summary>
        private float effectEndTime;

        public static RoomPlacementEffectController Instance
        {
            get
            {
                if (isDestroying || !Application.isPlaying)
                {
                    return null;
                }

                if (instance == null)
                {
                    var go = new GameObject("RoomPlacementEffectController");
                    instance = go.AddComponent<RoomPlacementEffectController>();
                    DontDestroyOnLoad(go);
                }
                return instance;
            }
        }

        /// <summary>IMovementBlocker 実装: エフェクト再生中は移動を禁止する。</summary>
        public bool IsMovementBlocked => isPlayingEffect;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                DontDestroyOnLoad(gameObject);
            }
            else if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Update()
        {
            // エフェクト再生中で、終了時刻を過ぎたら解除
            if (isPlayingEffect && Time.time >= effectEndTime)
            {
                isPlayingEffect = false;
            }
        }

        /// <summary>
        /// エフェクト再生を開始し、指定時間プレイヤーの移動を禁止する。
        /// </summary>
        /// <param name="duration">エフェクトの再生時間（秒）</param>
        public void StartEffect(float duration)
        {
            isPlayingEffect = true;
            effectEndTime = Time.time + duration;
        }

        /// <summary>
        /// エフェクト再生を強制終了する。
        /// </summary>
        public void StopEffect()
        {
            isPlayingEffect = false;
        }

        private void OnApplicationQuit()
        {
            isDestroying = true;
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}


