using UnityEngine;

namespace Game.Enemy.View
{
    /// <summary>
    /// 敵のビジュアルモデルを移動方向に向けて回転させるコンポーネント。
    /// EnemyVirusAIComponentの移動方向を監視し、モデルを滑らかに回転させる。
    /// </summary>
    public class EnemyModelRotator : MonoBehaviour
    {
        [Header("参照")]
        [Tooltip("回転対象のモデル（通常は子オブジェクトの VisualModel）")]
        [SerializeField] private Transform visualModel;

        [Tooltip("移動方向を取得するEnemyVirusAIComponent")]
        [SerializeField] private EnemyVirusAIComponent enemyAI;

        [Header("回転設定")]
        [Tooltip("回転速度（度/秒）")]
        [SerializeField] private float rotationSpeed = 720f;

        [Tooltip("回転を滑らかに補間するか")]
        [SerializeField] private bool smoothRotation = true;

        /// <summary>現在の目標回転角度（Y軸）</summary>
        private float targetRotationY;

        /// <summary>前回の移動方向（変化検出用）</summary>
        private Vector2Int previousDirection;

        private void Awake()
        {
            // 自動検出を試みる
            if (enemyAI == null)
            {
                enemyAI = GetComponent<EnemyVirusAIComponent>();
            }

            if (visualModel == null)
            {
                // 子オブジェクトから "VisualModel" を探す
                var visualModelTransform = transform.Find("VisualModel");
                if (visualModelTransform != null)
                {
                    visualModel = visualModelTransform;
                }
                else
                {
                    // 見つからない場合は自身を使用
                    visualModel = transform;
                    UnityEngine.Debug.LogWarning("[EnemyModelRotator] VisualModelが見つかりません。Enemy本体を回転させます。", this);
                }
            }

            // 初期回転を記録
            if (visualModel != null)
            {
                targetRotationY = visualModel.localEulerAngles.y;
            }
        }

        private void LateUpdate()
        {
            if (enemyAI == null || visualModel == null)
            {
                return;
            }

            // 現在の移動方向を取得
            var currentDirection = enemyAI.CurrentMoveDirection;

            // 移動方向が変わった場合、目標回転を更新
            if (currentDirection != Vector2Int.zero && currentDirection != previousDirection)
            {
                UpdateTargetRotation(currentDirection);
                previousDirection = currentDirection;
            }

            // 回転を適用
            ApplyRotation();
        }

        /// <summary>
        /// 移動方向から目標回転角度を計算する。
        /// </summary>
        /// <param name="direction">移動方向（グリッド座標系）</param>
        private void UpdateTargetRotation(Vector2Int direction)
        {
            // グリッド方向（X=横、Y=縦）をワールド回転（Y軸）に変換
            // Unity座標系: X=右、Z=前
            // グリッド方向: X=右、Y=前（Z軸方向）

            float angle;

            if (direction.x > 0 && direction.y == 0)
            {
                // 右（East）
                angle = 90f;
            }
            else if (direction.x < 0 && direction.y == 0)
            {
                // 左（West）
                angle = -90f;
            }
            else if (direction.y > 0 && direction.x == 0)
            {
                // 前（North）
                angle = 0f;
            }
            else if (direction.y < 0 && direction.x == 0)
            {
                // 後（South）
                angle = 180f;
            }
            else
            {
                // 斜め方向（念のため対応）
                angle = Mathf.Atan2(direction.x, direction.y) * Mathf.Rad2Deg;
            }

            targetRotationY = angle;
        }

        /// <summary>
        /// 現在の回転を目標回転に向けて更新する。
        /// </summary>
        private void ApplyRotation()
        {
            if (visualModel == null)
            {
                return;
            }

            var currentEuler = visualModel.localEulerAngles;
            var currentY = currentEuler.y;

            float newY;

            if (smoothRotation)
            {
                // 滑らかに回転（Lerpまたは回転速度で制限）
                newY = Mathf.MoveTowardsAngle(currentY, targetRotationY, rotationSpeed * Time.deltaTime);
            }
            else
            {
                // 即座に回転
                newY = targetRotationY;
            }

            // 回転を適用（Y軸のみ）
            visualModel.localEulerAngles = new Vector3(currentEuler.x, newY, currentEuler.z);
        }

        /// <summary>
        /// 目標回転角度を強制設定する（外部から呼び出し可能）。
        /// </summary>
        /// <param name="angle">Y軸の回転角度（度）</param>
        public void SetRotation(float angle)
        {
            targetRotationY = angle;

            if (visualModel != null)
            {
                var euler = visualModel.localEulerAngles;
                visualModel.localEulerAngles = new Vector3(euler.x, angle, euler.z);
            }
        }

        /// <summary>
        /// 現在の回転角度を取得する。
        /// </summary>
        public float CurrentRotationY => visualModel != null ? visualModel.localEulerAngles.y : 0f;
    }
}


