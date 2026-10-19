using UnityEngine;

namespace Game.Dungeon
{
    /// <summary>
    /// 確定配置済みの部屋ビュー。配置のたびに Instantiate される。
    /// ドアの開閉と確定後のエンティティ保持を担う。
    /// 現行の RoomPrefabController に相当する壁差し替えロジックを内包する。
    /// </summary>
    public class DungeonPlacedRoomView : MonoBehaviour
    {
        [Header("BigWall プレハブ（方向別）")]
        [SerializeField] private DirectionalWallPrefabs bigWallPrefabs = new();

        [Header("HoleWall プレハブ（方向別）")]
        [SerializeField] private DirectionalWallPrefabs holeWallPrefabs = new();

        [Header("壁コンテナ（子オブジェクト）")]
        [SerializeField] private Transform wallN;
        [SerializeField] private Transform wallS;
        [SerializeField] private Transform wallE;
        [SerializeField] private Transform wallW;

        [Header("方向別 初期壁タイプ設定")]
        [SerializeField] private DirectionalWallConfig wallConfig = new();

        [Header("配置演出")]
        [SerializeField] private GameObject placementVfxPrefab;
        [SerializeField] private AudioClip placementSe;
        [SerializeField] private float scaleAnimationDuration = 0.5f;
        [SerializeField] private bool enablePlacementEffect = true;

        [Header("エフェクト設定")]
        [SerializeField] private float effectPlaybackSpeed = 1.0f;
        [SerializeField] private bool blockPlayerMovementDuringEffect = true;
        [SerializeField] private float effectDuration = 2.0f;
        [SerializeField] private float effectHeightOffset = 0.5f;

        [Header("ライト演出")]
        [SerializeField] private bool enablePlacementLight = true;
        [SerializeField] private Color lightColor = new Color(0.3f, 0.6f, 1.0f);
        [SerializeField] private float lightIntensity = 5.0f;
        [SerializeField] private float lightRange = 15.0f;
        [SerializeField] private float lightFadeDuration = 1.5f;
        [SerializeField] private float lightHeightOffset = 1.0f;

        /// <summary>
        /// エンティティを受け取り、ワールド座標に配置して各辺を wallConfig の設定で初期化する。
        /// </summary>
        public void Initialize(DungeonRoomEntity roomEntity)
        {
            // roomEntity は将来の拡張（セル情報の参照など）に備えて受け取るが
            // 現状は InitByConfig() の呼び出しのみ使用する
            InitByConfig();

            // 配置演出を再生
            if (enablePlacementEffect)
            {
                PlayPlacementEffect();
            }
        }

        /// <summary>
        /// 指定方向の壁を Hole（ドア）に差し替える。
        /// </summary>
        public void SetDoor(Direction dir)
        {
            var container = dir switch
            {
                Direction.North => wallN,
                Direction.South => wallS,
                Direction.East  => wallE,
                Direction.West  => wallW,
                _               => null
            };
            ReplaceWall(container, holeWallPrefabs.Get(dir));
        }

        /// <summary>
        /// 指定方向の壁を通常壁（BigWall）に差し替える。SetDoor() と対になる、壁を閉じるメソッド。
        /// ダンジョン崩壊システムが、崩壊対象部屋に接続していた隣接部屋の壁を閉じる際に使用する。
        /// </summary>
        public void SetWall(Direction dir)
        {
            var container = dir switch
            {
                Direction.North => wallN,
                Direction.South => wallS,
                Direction.East  => wallE,
                Direction.West  => wallW,
                _               => null
            };
            ReplaceWall(container, bigWallPrefabs.Get(dir));
        }

        // ──────────────────────────────────────────
        // private
        // ──────────────────────────────────────────

        /// <summary>部屋配置演出を再生する。</summary>
        private void PlayPlacementEffect()
        {
            // プレイヤー移動制御
            if (blockPlayerMovementDuringEffect)
            {
                var effectController = View.RoomPlacementEffectController.Instance;
                if (effectController != null)
                {
                    effectController.StartEffect(effectDuration);
                }
            }

            // スケールアニメーション（小→大）
            StartCoroutine(ScaleAnimationCoroutine());

            // エフェクト再生
            if (placementVfxPrefab != null)
            {
                var vfxManager = Effect.VFXManager.Instance;
                if (vfxManager != null)
                {
                    var effectPosition = transform.position + Vector3.up * effectHeightOffset;
                    var spawnedEffect = Instantiate(placementVfxPrefab, effectPosition, Quaternion.identity);

                    // エフェクトの再生速度を調整
                    var particleSystem = spawnedEffect.GetComponent<ParticleSystem>();
                    if (particleSystem != null)
                    {
                        var mainModule = particleSystem.main;
                        mainModule.simulationSpeed = effectPlaybackSpeed;

                        // 自動削除（再生速度を考慮した時間で削除）
                        var adjustedDuration = (mainModule.duration + mainModule.startLifetime.constantMax) / effectPlaybackSpeed;
                        Destroy(spawnedEffect, adjustedDuration);
                    }
                    else
                    {
                        Destroy(spawnedEffect, 3f / effectPlaybackSpeed);
                    }
                }
            }

            // ライト演出
            if (enablePlacementLight)
            {
                StartCoroutine(LightEffectCoroutine());
            }

            // SE再生
            if (placementSe != null)
            {
                var audioManager = Audio.AudioManager.Instance;
                if (audioManager != null)
                {
                    audioManager.PlaySe(placementSe);
                }
            }
        }

        /// <summary>小→大にスケールアニメーションするコルーチン。</summary>
        private System.Collections.IEnumerator ScaleAnimationCoroutine()
        {
            var startScale = Vector3.zero;
            var endScale = Vector3.one;
            var elapsed = 0f;

            while (elapsed < scaleAnimationDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / scaleAnimationDuration;
                // イージング: EaseOutBack風（少しオーバーシュート）
                t = t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;
                transform.localScale = Vector3.Lerp(startScale, endScale, t);
                yield return null;
            }

            transform.localScale = endScale;
        }

        /// <summary>ライト演出用のコルーチン。光が徐々にフェードアウトする。</summary>
        private System.Collections.IEnumerator LightEffectCoroutine()
        {
            // ライトオブジェクトを生成
            var lightGo = new GameObject("PlacementLight");
            lightGo.transform.SetParent(transform);
            lightGo.transform.localPosition = Vector3.up * lightHeightOffset;

            var pointLight = lightGo.AddComponent<Light>();
            pointLight.type = LightType.Point;
            pointLight.color = lightColor;
            pointLight.intensity = lightIntensity;
            pointLight.range = lightRange;
            pointLight.shadows = LightShadows.None;

            // フェードアウトアニメーション
            var elapsed = 0f;
            var startIntensity = lightIntensity;

            while (elapsed < lightFadeDuration)
            {
                elapsed += Time.deltaTime;
                var t = elapsed / lightFadeDuration;
                pointLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
                yield return null;
            }

            // ライトを削除
            Destroy(lightGo);
        }

        /// <summary>
        /// wallConfig の方向別設定に従い、各辺を BigWall または SmallWall で初期化する。
        /// </summary>
        private void InitByConfig()
        {
            ReplaceWall(wallN, SelectWallPrefab(Direction.North));
            ReplaceWall(wallS, SelectWallPrefab(Direction.South));
            ReplaceWall(wallE, SelectWallPrefab(Direction.East));
            ReplaceWall(wallW, SelectWallPrefab(Direction.West));
        }

        /// <summary>
        /// 方向に対応する WallType から実際のプレハブを選択して返す。
        /// </summary>
        private GameObject SelectWallPrefab(Direction direction)
        {
            return wallConfig.Get(direction) == WallType.Big
                ? bigWallPrefabs.Get(direction)
                : holeWallPrefabs.Get(direction);
        }

        private void ReplaceWall(Transform container, GameObject prefab)
        {
            if (container == null || prefab == null)
            {
                return;
            }

            for (var i = container.childCount - 1; i >= 0; i--)
            {
                Destroy(container.GetChild(i).gameObject);
            }

            Instantiate(prefab, container);
        }
    }
}



