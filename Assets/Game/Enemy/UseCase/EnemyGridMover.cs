using UnityEngine;

namespace Game.Enemy.UseCase
{
    /// <summary>
    /// 敵のグリッド補間移動を担当するクラス。
    /// 責務: 目標グリッド座標への MoveTowards 補間移動のみ。
    /// AI 状態管理やノックバック計算は行わない。
    /// </summary>
    public class EnemyGridMover
    {
        private readonly Transform transform;
        private readonly float moveSpeed;
        private readonly bool enableDebugLog;

        private Vector3 targetWorldPos;
        private bool isMoving;

        /// <summary>現在移動中か</summary>
        public bool IsMoving => isMoving;

        /// <summary>目標ワールド座標</summary>
        public Vector3 TargetWorldPos => targetWorldPos;

        public EnemyGridMover(Transform transform, float moveSpeed, bool enableDebugLog = false)
        {
            this.transform = transform;
            this.moveSpeed = moveSpeed;
            this.enableDebugLog = enableDebugLog;
            this.targetWorldPos = transform.position;
            this.isMoving = false;
        }

        /// <summary>
        /// 新しい目標グリッド座標を設定して移動を開始する。
        /// </summary>
        public void StartMove(Vector3 newTargetWorldPos)
        {
            targetWorldPos = newTargetWorldPos;
            isMoving = true;

            if (enableDebugLog)
            {
                UnityEngine.Debug.Log($"[EnemyGridMover] 移動開始 → {targetWorldPos}");
            }
        }

        /// <summary>
        /// 毎フレーム呼び出して補間移動を進める。
        /// 目標に到達したら isMoving を false にする。
        /// </summary>
        public void UpdateMove()
        {
            if (!isMoving)
            {
                return;
            }

            transform.position = Vector3.MoveTowards(
                transform.position,
                targetWorldPos,
                moveSpeed * Time.deltaTime
            );

            if (Vector3.Distance(transform.position, targetWorldPos) < 0.01f)
            {
                transform.position = targetWorldPos;
                isMoving = false;

                if (enableDebugLog)
                {
                    UnityEngine.Debug.Log($"[EnemyGridMover] 移動完了: {targetWorldPos}");
                }
            }
        }

        /// <summary>
        /// 移動をキャンセルする。
        /// </summary>
        public void CancelMove()
        {
            isMoving = false;
        }

        /// <summary>
        /// 目標座標を強制的に現在位置にリセットする（ノックバック時など）。
        /// </summary>
        public void ResetTarget()
        {
            targetWorldPos = transform.position;
            isMoving = false;
        }
    }
}

