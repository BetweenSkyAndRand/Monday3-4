using System.Collections.Generic;
using UnityEngine;
// ReSharper disable Unity.PreferNonAllocApi

namespace Game.CameraSystem.Transparency
{
    /// <summary>
    /// カメラとプレイヤーの間に壁があるとき、その壁を半透明にする管理コンポーネント。
    /// メインカメラ、またはシーン内の専用 GameObject にアタッチする。
    ///
    /// 処理の流れ:
    ///   LateUpdate ごとに以下の2つの方法で壁を検出し、フェード状態を切り替える。
    ///   1. 「カメラ位置 → プレイヤー重心 + 延長距離」へ Physics.RaycastAll を実行
    ///   2. プレイヤー周辺を Physics.OverlapSphere で検索
    ///   前フレームでフェードしていた壁が今フレームでヒットしなければ自動復元する。
    ///   ただし、fadeOutCooldown 秒経過するまでは透明のまま維持される。
    ///
    /// extraTransparencyRange:
    ///   プレイヤーの先まで透明化範囲を延長する距離。
    ///   壁の向こう側にあるオブジェクトや敵が見えるようにするため、1マス分（1.0f）が推奨。
    ///
    /// playerSurroundingRadius:
    ///   プレイヤー周辺の壁を検出する範囲。
    ///   プレイヤーが移動しても手前の壁が透明のまま維持されるため、1.5マス分（1.5f）が推奨。
    ///   例: 壁壁→敵→プレイヤー の配置で、プレイヤーが奥に移動しても手前の壁は透明のまま。
    ///
    /// fadeOutCooldown:
    ///   透明化解除のクールタイム（秒）。
    ///   プレイヤーが離れてから、この時間が経過するまで壁は透明のまま維持される。
    ///   推奨値: 0.5秒（チラつき防止とレスポンスのバランス）
    /// </summary>
    public class WallOcclusionController : MonoBehaviour
    {
        [Header("プレイヤーの Transform（未設定時は 'Player' タグで自動取得）")]
        [SerializeField] private Transform playerTransform;

        [Header("壁として検出する Physics Layer（例: 'Wall' レイヤー）")]
        [SerializeField] private LayerMask wallLayerMask;

        [Header("プレイヤーの重心オフセット（カメラがキャラクターの頭部方向を向いている場合に調整）")]
        [SerializeField] private Vector3 playerBodyOffset = new Vector3(0f, 0.5f, 0f);

        [Header("透明化範囲の延長距離（プレイヤーの先まで透明化する距離、セル1マス = 1.0f）")]
        [SerializeField] private float extraTransparencyRange = 1.0f;

        [Header("プレイヤー周辺の壁検出範囲（プレイヤーの周りの壁も透明化、セル1マス = 1.0f）")]
        [SerializeField] private float playerSurroundingRadius = 1.5f;

        [Header("透明化解除のクールタイム（秒）- プレイヤーが離れてもすぐには不透明に戻らない")]
        [SerializeField] private float fadeOutCooldown = 0.5f;

        private const string PlayerTag = "Player";

        /// <summary>現在フェード中の壁一覧。前フレームと差分を見て自動復元する。</summary>
        private readonly HashSet<WallTransparencyTarget> fadedTargets = new HashSet<WallTransparencyTarget>();

        /// <summary>今フレームでヒットした壁一覧（フレームごとに再構築）</summary>
        private readonly HashSet<WallTransparencyTarget> hitTargetsThisFrame = new HashSet<WallTransparencyTarget>();

        /// <summary>各壁が最後にヒットした時刻を記録</summary>
        private readonly Dictionary<WallTransparencyTarget, float> lastHitTimes = new Dictionary<WallTransparencyTarget, float>();

        private void Start()
        {
            if (playerTransform != null)
            {
                return;
            }

            var playerObject = GameObject.FindGameObjectWithTag(PlayerTag);
            if (playerObject == null)
            {
                UnityEngine.Debug.LogWarning("[WallOcclusionController] 'Player' タグを持つオブジェクトが見つかりません。", this);
                return;
            }

            playerTransform = playerObject.transform;
        }

        /// <summary>
        /// カメラ移動後に実行されるよう LateUpdate で処理する。
        /// </summary>
        private void LateUpdate()
        {
            if (playerTransform == null)
            {
                return;
            }

            UpdateOcclusionFade();
        }

        /// <summary>
        /// レイキャストを実行してフェード対象を更新し、不要になったフェードを解除する。
        /// カメラ→プレイヤーの直線上の壁に加えて、プレイヤー周辺の壁も透明化する。
        /// </summary>
        private void UpdateOcclusionFade()
        {
            hitTargetsThisFrame.Clear();

            var cameraPosition = transform.position;
            var playerCenter = playerTransform.position + playerBodyOffset;
            var direction = playerCenter - cameraPosition;
            var distance = direction.magnitude;

            if (distance < 0.01f)
            {
                RestoreAllFadedTargets();
                return;
            }

            // カメラからプレイヤーへ raycastAll を実行（壁 Layer のみ）
            // プレイヤーの先まで extraTransparencyRange 分延長して、奥にあるオブジェクトや敵が見えるようにする
            var extendedDistance = distance + Mathf.Max(0f, extraTransparencyRange);
            var raycastHits = Physics.RaycastAll(
                cameraPosition,
                direction.normalized,
                extendedDistance,
                wallLayerMask,
                QueryTriggerInteraction.Ignore
            );

            foreach (var hit in raycastHits)
            {
                DetectAndAddTarget(hit.collider);
            }

            // プレイヤー周辺の壁も検出する（プレイヤーが移動しても手前の壁が透明のまま維持される）
            if (playerSurroundingRadius > 0.01f)
            {
                var surroundingColliders = Physics.OverlapSphere(
                    playerCenter,
                    playerSurroundingRadius,
                    wallLayerMask,
                    QueryTriggerInteraction.Ignore
                );

                foreach (var collider in surroundingColliders)
                {
                    DetectAndAddTarget(collider);
                }
            }

            // 今フレームでヒットしなかった壁を元に戻す
            RestoreTargetsNotHitThisFrame();
        }

        /// <summary>
        /// Collider から WallTransparencyTarget を検出して hitTargetsThisFrame に追加する。
        /// 最後にヒットした時刻も記録する。
        /// </summary>
        private void DetectAndAddTarget(Collider collider)
        {
            if (collider == null)
            {
                return;
            }

            // WallTransparencyTarget を持つオブジェクトのみフェード対象とする
            if (!collider.TryGetComponent<WallTransparencyTarget>(out var transparencyTarget))
            {
                // Collider の親オブジェクトを辿って探す
                transparencyTarget = collider.GetComponentInParent<WallTransparencyTarget>();
            }

            if (transparencyTarget == null)
            {
                return;
            }

            hitTargetsThisFrame.Add(transparencyTarget);

            // 最後にヒットした時刻を記録（重要！）
            lastHitTimes[transparencyTarget] = Time.time;

            // まだフェードしていなければフェードを開始する
            if (!fadedTargets.Contains(transparencyTarget))
            {
                transparencyTarget.SetFade(true);
                fadedTargets.Add(transparencyTarget);
            }
        }

        /// <summary>
        /// 今フレームでヒットしなかったフェード対象を復元する。
        /// ただし、最後にヒットしてから fadeOutCooldown 秒経過していない場合は復元しない。
        /// </summary>
        private void RestoreTargetsNotHitThisFrame()
        {
            var targetsToRemove = new List<WallTransparencyTarget>();
            var currentTime = Time.time;

            foreach (var fadedTarget in fadedTargets)
            {
                // 今フレームでヒットした壁はスキップ
                if (hitTargetsThisFrame.Contains(fadedTarget))
                {
                    continue;
                }

                // 最後のヒット時刻を取得
                if (lastHitTimes.TryGetValue(fadedTarget, out var lastHitTime))
                {
                    // クールタイムが経過していない場合は透明のまま維持
                    var timeSinceLastHit = currentTime - lastHitTime;
                    if (timeSinceLastHit < fadeOutCooldown)
                    {
                        continue;
                    }
                }

                // クールタイムが経過したので透明化を解除
                if (fadedTarget != null)
                {
                    fadedTarget.SetFade(false);
                }

                targetsToRemove.Add(fadedTarget);
            }

            foreach (var targetToRemove in targetsToRemove)
            {
                fadedTargets.Remove(targetToRemove);
                lastHitTimes.Remove(targetToRemove);
            }
        }

        /// <summary>
        /// 全フェード対象を復元する（プレイヤー消失時や無効化時に呼ぶ）。
        /// </summary>
        private void RestoreAllFadedTargets()
        {
            foreach (var fadedTarget in fadedTargets)
            {
                if (fadedTarget != null)
                {
                    fadedTarget.SetFade(false);
                }
            }

            fadedTargets.Clear();
            lastHitTimes.Clear();
        }

        private void OnDisable()
        {
            RestoreAllFadedTargets();
        }
    }
}



