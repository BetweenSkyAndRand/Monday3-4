using System;
using Game.Weapon;
using UnityEngine;

namespace Game.Presentation
{
    [Serializable]
    public struct WeaponPresentationEntry
    {
        public WeaponData weaponData;
        public GameObject weaponModelPrefab;
        public GameObject attackVfxPrefab;
        public GameObject hitVfxPrefab;
        public AudioClip attackSe;
        public AudioClip hitSe;
        public Vector3 vfxOffset;

        // ─── ID ベース SE（GamePresentationSettings.sePresentations から解決） ───
        // 設定されている場合は AudioManager.PlayConfiguredSe(...) ルート経由で再生する。
        // 未設定（空文字）の場合は従来の AudioClip フィールドにフォールバックする。
        public string attackSeId;
        public string hitSeId;
    }

    [Serializable]
    public struct EnemyPresentationEntry
    {
        public string enemyUid;
        public GameObject spawnVfxPrefab;
        public GameObject hitVfxPrefab;
        public GameObject deadVfxPrefab;
        public AudioClip spawnSe;
        public AudioClip attackSe;
        public AudioClip deadSe;
        public AudioClip damageSe;
        public GameObject weaponModelPrefab;

        // ─── ID ベース SE（GamePresentationSettings.sePresentations から解決） ───
        public string spawnSeId;
        public string attackSeId;
        public string deadSeId;
        public string damageSeId;
    }

    [Serializable]
    public struct BgmPresentationEntry
    {
        public string bgmId;
        public AudioClip clip;
        public bool loop;
    }

    /// <summary>
    /// SE を ID で一元管理するためのエントリ。
    /// BgmPresentationEntry と同じ形式で、AudioManager.PlayConfiguredSe(string) から参照される。
    /// </summary>
    [Serializable]
    public struct SePresentationEntry
    {
        public string seId;
        public AudioClip clip;
        [Tooltip("0〜1。マスター×SE音量に掛け合わせる個別の音量倍率。1.0 がデフォルト。")]
        [Range(0f, 1f)]
        public float volumeScale;
    }

    /// <summary>
    /// ゲーム内の演出アセットを一箇所で設定するための集中設定コンポーネント。
    /// Scene 上に 1 つだけ配置し、武器・敵・プレイヤー被弾・BGM の参照を Inspector から編集する。
    /// </summary>
    public class GamePresentationSettings : MonoBehaviour
    {
        private static GamePresentationSettings instance;

        [Header("プレイヤー被弾演出")]
        [SerializeField] private AudioClip playerHitSe;
        [SerializeField] private Color playerHitFlashColor = new(1f, 0f, 0f, 0.5f);
        [SerializeField] private float playerHitFlashDuration = 0.2f;

        [Header("武器演出設定")]
        [SerializeField] private WeaponPresentationEntry[] weaponPresentations;

        [Header("敵演出設定")]
        [SerializeField] private EnemyPresentationEntry[] enemyPresentations;

        [Header("BGM設定")]
        [SerializeField] private BgmPresentationEntry[] bgmPresentations;

        [Header("SE設定")]
        [Tooltip("ID で管理する SE のエントリ。AudioManager.PlayConfiguredSe(seId) で再生する。")]
        [SerializeField] private SePresentationEntry[] sePresentations;

        public static GamePresentationSettings Instance
        {
            get
            {
                if (instance != null)
                {
                    return instance;
                }

                instance = FindFirstObjectByType<GamePresentationSettings>();
                return instance;
            }
        }

        public AudioClip PlayerHitSe => playerHitSe;
        public Color PlayerHitFlashColor => playerHitFlashColor;
        public float PlayerHitFlashDuration => playerHitFlashDuration;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
                return;
            }

            if (instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (instance == this)
            {
                instance = null;
            }
        }

        public bool TryGetWeaponPresentation(WeaponData weaponData, out WeaponPresentationEntry presentationEntry)
        {
            if (weaponPresentations != null)
            {
                foreach (var entry in weaponPresentations)
                {
                    if (entry.weaponData == weaponData)
                    {
                        presentationEntry = entry;
                        return true;
                    }
                }
            }

            presentationEntry = default;
            return false;
        }

        public bool TryGetEnemyPresentation(string enemyUid, out EnemyPresentationEntry presentationEntry)
        {
            if (!string.IsNullOrEmpty(enemyUid) && enemyPresentations != null)
            {
                foreach (var entry in enemyPresentations)
                {
                    if (entry.enemyUid == enemyUid)
                    {
                        presentationEntry = entry;
                        return true;
                    }
                }
            }

            presentationEntry = default;
            return false;
        }

        public bool TryGetBgmPresentation(string bgmId, out BgmPresentationEntry presentationEntry)
        {
            if (!string.IsNullOrEmpty(bgmId) && bgmPresentations != null)
            {
                foreach (var entry in bgmPresentations)
                {
                    if (entry.bgmId == bgmId)
                    {
                        presentationEntry = entry;
                        return true;
                    }
                }
            }

            presentationEntry = default;
            return false;
        }

        /// <summary>
        /// SE プリセンテーションエントリを seId で取得する。
        /// AudioManager.PlayConfiguredSe(seId) から呼ばれる。
        /// </summary>
        public bool TryGetSePresentation(string seId, out SePresentationEntry presentationEntry)
        {
            if (!string.IsNullOrEmpty(seId) && sePresentations != null)
            {
                foreach (var entry in sePresentations)
                {
                    if (entry.seId == seId)
                    {
                        presentationEntry = entry;
                        return true;
                    }
                }
            }

            presentationEntry = default;
            return false;
        }
    }
}

