using UnityEngine;

namespace Game.Player.Script
{
    /// <summary>
    /// グリッド座標の計算・障害物判定・次の目標座標の確定のみを担う純粋C#クラス。
    /// MonoBehaviour には依存しない。PlayerGridMover から生成・使用される。
    ///PlayerGridMover は入力受付・補間移動の MonoBehaviour」
    /// 責務の分離により、移動ロジックを単体テストしやすくする。
    /// </summary>
    public class GridMover
    {
        private readonly float cellSize;

        /// <summary>
        /// 障害物判定から除外するプレイヤー自身の GameObject。
        /// OverlapBox がプレイヤー自身を障害物として誤検出しないために必要。
        /// </summary>
        private readonly GameObject selfObject;

        /// <summary>OverlapBoxNonAlloc 用のバッファ。毎回生成を避けて GC 負荷を抑える。</summary>
        private static readonly Collider[] overlapBuffer = new Collider[8];

        /// <summary>現在のグリッド座標。ConfirmMove() によってのみ更新される。</summary>
        public Vector2Int CurrentGridPos { get; private set; }

        /// <summary>移動戦略（プレイヤー/敵で振る舞いを変える）</summary>
        private readonly Game.Grid.IGridMovementStrategy movementStrategy;

        /// <summary>詳細ログを出すかどうか。</summary>
        private readonly bool enableDebugLog;

        /// <param name="cellSize">1 セルのワールドサイズ。</param>
        /// <param name="initialGridPos">初期グリッド座標。</param>
        /// <param name="selfObject">プレイヤー自身。障害物判定から除外する。</param>
        /// <param name="movementStrategy">移動戦略（nullの場合はEnemyMovementStrategy）</param>
        public GridMover(
            float cellSize,
            Vector2Int initialGridPos,
            GameObject selfObject,
            Game.Grid.IGridMovementStrategy movementStrategy = null,
            bool enableDebugLog = false)
        {
            this.cellSize = cellSize;
            CurrentGridPos = initialGridPos;
            this.selfObject = selfObject;
            this.movementStrategy = movementStrategy ?? new Game.Grid.EnemyMovementStrategy();
            this.enableDebugLog = enableDebugLog;

            // 初期グリッドを占有
            Game.Grid.GridOccupationManager.Occupy(initialGridPos, selfObject);
        }

        /// <summary>
        /// inputDir 方向への移動が可能か判定し、可能な場合は worldPos にワールド座標を返す。
        /// プレイヤー自身のコライダーは除外して障害物判定を行う。
        /// IsTrigger = true のコライダー（HitBox 等）は障害物として扱わない。
        /// グリッド占有状態もチェックし、敵同士の重なりを防ぐ。
        /// </summary>
        /// <param name="inputDir">移動方向（グリッド単位）。</param>
        /// <param name="playerY">プレイヤーの現在の Y 座標。段差対応のため毎フレーム渡す。</param>
        /// <param name="worldPos">移動可能な場合の移動先ワールド座標。不可能の場合は Vector3.zero。</param>
        /// <returns>移動可能なら true。</returns>
        public bool TryGetNextPos(Vector2Int inputDir, float playerY, out Vector3 worldPos)
        {
            var nextGrid = CurrentGridPos + inputDir;
            worldPos = new Vector3(nextGrid.x * cellSize, playerY, nextGrid.y * cellSize);

            // グリッド占有チェック
            if (Game.Grid.GridOccupationManager.IsOccupied(nextGrid, out var occupant))
            {
                if (occupant != null && occupant != selfObject)
                {
                    // 移動戦略に基づいて占有グリッドへの移動可否を判定
                    if (!movementStrategy.CanMoveToOccupiedGrid(occupant, selfObject))
                    {
                        if (enableDebugLog && Time.frameCount % 120 == 0)
                        {
                            UnityEngine.Debug.Log($"[GridMover] グリッド占有により移動不可 - グリッド: {nextGrid}, 占有者: {occupant.name}, 自分: {selfObject?.name}", selfObject);
                        }
                        worldPos = Vector3.zero;
                        return false;
                    }
                }
            }

            // OverlapBox のチェック位置を少し上げて、地面のコライダーとの衝突を回避
            // キャラクターの中心（腰の高さ）でチェックすることで、地面を障害物として誤検出しない
            var checkPos = new Vector3(nextGrid.x * cellSize, playerY + 0.5f, nextGrid.y * cellSize);

            var hitCount = Physics.OverlapBoxNonAlloc(
                checkPos,
                Vector3.one * 0.45f,
                overlapBuffer,
                Quaternion.identity,
                Physics.AllLayers,
                QueryTriggerInteraction.Ignore
            );

            for (var i = 0; i < hitCount; i++)
            {
                var hit = overlapBuffer[i];
                if (hit == null)
                {
                    continue;
                }

                // プレイヤー自身は障害物として扱わない
                if (hit.gameObject != selfObject)
                {
                    // デバッグ: 障害物が検出された場合、その情報をログ出力
                    if (enableDebugLog && Time.frameCount % 120 == 0)
                    {
                        UnityEngine.Debug.Log($"[GridMover] 障害物検出 - オブジェクト: {hit.gameObject.name}, レイヤー: {LayerMask.LayerToName(hit.gameObject.layer)}, チェック位置: {checkPos}, selfObject: {selfObject?.name}", selfObject);
                    }
                    worldPos = Vector3.zero;
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// 移動先グリッド座標を確定する。
        /// TryGetNextPos() が true を返した後に呼ぶこと。
        /// </summary>
        public void ConfirmMove(Vector2Int nextGrid)
        {
            UpdateGridOccupation(nextGrid);
            NotifyOccupantIfNeeded(nextGrid);
            CurrentGridPos = nextGrid;
        }

        /// <summary>
        /// グリッド占有状態を更新する。
        /// 古い位置の占有を解除し、新しい位置を占有する。
        /// </summary>
        /// <param name="nextGrid">新しいグリッド座標</param>
        private void UpdateGridOccupation(Vector2Int nextGrid)
        {
            // 古いグリッドの占有を解除
            Game.Grid.GridOccupationManager.Release(CurrentGridPos);

            // 新しいグリッドを占有
            Game.Grid.GridOccupationManager.Occupy(nextGrid, selfObject);
        }

        /// <summary>
        /// 移動先のグリッドに占有者がいる場合、IKnockbackHandlerに通知する。
        /// 移動戦略によって通知するかどうかが決まる。
        /// </summary>
        /// <param name="nextGrid">移動先グリッド座標</param>
        private void NotifyOccupantIfNeeded(Vector2Int nextGrid)
        {
            // 移動戦略に従って占有者に通知
            if (!movementStrategy.ShouldNotifyOccupant())
            {
                return;
            }

            if (Game.Grid.GridOccupationManager.IsOccupied(nextGrid, out var occupant))
            {
                if (occupant != null && occupant != selfObject)
                {
                    // IKnockbackHandlerを実装している場合、吹き飛ばし処理を委譲
                    var knockbackHandler = occupant.GetComponent<Game.Grid.IKnockbackHandler>();
                    if (knockbackHandler != null)
                    {
                        var playerWorldPos = new Vector3(nextGrid.x * cellSize, selfObject.transform.position.y, nextGrid.y * cellSize);
                        knockbackHandler.OnKnockback(playerWorldPos, cellSize);
                    }
                }
            }
        }

        /// <summary>
        /// 入力方向から次のグリッド座標を計算して返す。確定（ConfirmMove）は行わない。
        /// </summary>
        public Vector2Int GetNextGrid(Vector2Int inputDir)
        {
            return CurrentGridPos + inputDir;
        }

        /// <summary>
        /// グリッド位置を強制的に設定する。
        /// 通常の移動ではなく、吹き飛ばしなど外部から位置を変更された場合に使用。
        /// </summary>
        /// <param name="newGridPos">新しいグリッド座標</param>
        public void ForceSetGridPosition(Vector2Int newGridPos)
        {
            CurrentGridPos = newGridPos;
        }
    }
}

