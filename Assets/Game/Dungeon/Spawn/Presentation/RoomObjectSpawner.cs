// ReSharper disable once CheckNamespace
// プロジェクト規約でフォルダ階層より短い名前空間を使用している (例: Game.Search, Game.Dungeon)
using System.Collections.Generic;
using Game.Dungeon.Spawn.UseCase;
using Game.Enemy;
using Game.Enemy.Presentation;
using Game.Search;
using Game.Treasure;
using UnityEngine;

namespace Game.Dungeon.Spawn
{
    /// <summary>
    /// ISpawnInstructionHandler を実装し、探索開始時に各部屋へオブジェクトをスポーンする。
    /// room_spawn.json に定義された割合・数量に基づき、シード値を用いてランダムに配置する。
    ///
    /// 責務:
    ///   - SearchController からのスポーン要求受付（OnSpawnInstruction）
    ///   - スポーン実行の全体フロー制御
    ///   - 配置実行（Instantiate / EnemyVirusController 呼び出し）
    ///
    /// 委譲先:
    ///   - SpawnConfigRepository: JSON 読み込み
    ///   - RoomFreeCellPicker: 空きセル選択
    ///   - TreasureContentsSelector: 宝箱中身抽選
    ///
    /// スポーンルール：
    ///   - 初期部屋（RoomGridPosition == (0,0)）にはスポーンしない
    ///   - 敵・宝箱・雑貨は同一セルに重複配置しない
    ///   - 1 部屋あたりの敵数は minCount ～ maxCount の乱数で決定する
    /// </summary>
    public class RoomObjectSpawner : MonoBehaviour, ISpawnInstructionHandler
    {
        [Header("ダンジョン参照")]
        [SerializeField] private DungeonController dungeonController;

        [Header("敵スポーン設定（EnemyVirusController 経由でスポーンする）")]
        [SerializeField] private EnemyVirusController enemyVirusController;

        [Header("スポーン対象の敵 UID リスト（複数設定した場合はランダムに選択。1 種類でも動作する）")]
        [SerializeField] private string[] spawnableEnemyUids;

        [Header("宝箱プレハブ（TreasureBoxController アタッチ済みであること）")]
        [SerializeField] private GameObject treasureBoxPrefab;

        [Header("宝箱の中身テーブル（アイテムごとに出現率を万分率で設定）")]
        [Tooltip("ratePer10000 で設定する。例: Weapon(10000) → 100%")]
        [SerializeField] private TreasureItemWeight[] treasureItemWeights;

        [Header("敵強化（ダンジョン生成回数のマイルストーン）")]
        [SerializeField] private EnemyEnhancementParameter enemyEnhancementParameter;

        private EnemyEnhancementUseCase enemyEnhancementUseCase;

        /// <summary>
        /// 雑貨オブジェクトのプレハブ一覧。
        /// 未選定のため空配列で開始し、選定後にここへ追加するだけで対応できる可変設計。
        /// </summary>
        [SerializeField] private GameObject[] miscPrefabs;

        [Header("宝箱・雑貨の出現数と確率（同じ種別の行があれば room_spawn.json より優先）")]
        [SerializeField] private SpawnRuleSetting[] spawnRuleSettings;

        // ─── 委譲先クラス ───
        private SpawnConfigRepository spawnConfigRepository;
        private RoomFreeCellPicker roomFreeCellPicker;
        private TreasureContentsSelector treasureContentsSelector;

        private SpawnConfigData spawnConfig;
        private readonly RoomSpawnedObjectRegistry spawnedObjectRegistry = new RoomSpawnedObjectRegistry();
        private readonly List<SpawnRule> effectiveSpawnRules = new List<SpawnRule>();

        private void Awake()
        {
            spawnConfigRepository = new SpawnConfigRepository();
            roomFreeCellPicker = new RoomFreeCellPicker();
            treasureContentsSelector = new TreasureContentsSelector();
            enemyEnhancementUseCase = new EnemyEnhancementUseCase();

            LoadSpawnConfig();
        }

        private void OnEnable()
        {
            if (dungeonController != null)
            {
                dungeonController.OnRoomRemoved += HandleRoomRemoved;
            }
        }

        private void OnDisable()
        {
            if (dungeonController != null)
            {
                dungeonController.OnRoomRemoved -= HandleRoomRemoved;
            }
        }

        // 崩壊した部屋の宝箱・雑貨を消す（探索終了時も残りの部屋が崩壊するため同じ経路で消える）
        private void HandleRoomRemoved(Vector2Int roomGridPosition)
        {
            spawnedObjectRegistry.DestroyRoomObjects(roomGridPosition);
        }

        /// <summary>
        /// room_spawn.json を読み込む。
        /// </summary>
        private void LoadSpawnConfig()
        {
            spawnConfig = spawnConfigRepository.Load();
        }

        /// <summary>
        /// JSON のルールのうち、Inspector に同じ種別の行があるものを置き換えた、実際に使うルール一覧を作る。
        /// </summary>
        private void BuildEffectiveSpawnRules()
        {
            effectiveSpawnRules.Clear();
            var overriddenTypes = new HashSet<string>();
            if (spawnRuleSettings != null)
            {
                foreach (var setting in spawnRuleSettings)
                {
                    if (setting == null)
                    {
                        continue;
                    }

                    var rule = setting.ToSpawnRule();
                    overriddenTypes.Add(rule.type);
                    effectiveSpawnRules.Add(rule);
                }
            }

            foreach (var jsonRule in spawnConfig.spawnRules)
            {
                if (!overriddenTypes.Contains(jsonRule.type))
                {
                    effectiveSpawnRules.Add(jsonRule);
                }
            }
        }

        /// <summary>
        /// SearchController から探索開始時に呼ばれる。
        /// 全配置済み部屋（初期部屋を除く）にオブジェクトをスポーンする。
        /// シード値はスポーン命令受信時点で生成する。
        /// </summary>
        public void OnSpawnInstruction()
        {
            if (spawnConfig == null)
            {
                UnityEngine.Debug.LogWarning("[RoomObjectSpawner] SpawnConfig が未ロードのためスポーンをスキップします。");
                return;
            }

            if (dungeonController == null)
            {
                UnityEngine.Debug.LogError("[RoomObjectSpawner] dungeonController が設定されていません。");
                return;
            }

            BuildEffectiveSpawnRules();
            var rooms = dungeonController.RoomRepository.GetAll();
            var seed = Random.Range(int.MinValue, int.MaxValue);
            var random = new System.Random(seed);
            // UnityEngine.Debug.Log($"[RoomObjectSpawner] スポーン開始。部屋数: {rooms.Count}, シード: {seed}");

            foreach (var room in rooms)
            {
                // 初期部屋（グリッド原点）にはスポーンしない
                if (room.RoomGridPosition == Vector2Int.zero)
                {
                    continue;
                }

                SpawnInRoom(room, random);
            }
        }

        /// <summary>
        /// 指定部屋にスポーンルールに基づきオブジェクトを配置する。
        /// 同一セルへの重複配置は usedCells によって防ぐ。
        ///
        /// スポーン数の計算ルール：
        ///   1. minCount 分は確率抽選なしで確定スポーン
        ///   2. minCount を超えた追加枠（maxCount まで）は 1 体ずつ ratePer10000 で抽選し、
        ///      外れた時点で追加スポーンを打ち切る
        /// </summary>
        private void SpawnInRoom(DungeonRoomEntity room, System.Random random)
        {
            var usedCells = new HashSet<Vector2Int>();

            foreach (var spawnRule in effectiveSpawnRules)
            {
                // 確定スポーン（minCount 分は必ず配置）
                for (var i = 0; i < spawnRule.minCount; i++)
                {
                    var spawnPosition = PickFreeCell(room, usedCells, random);
                    if (spawnPosition == null)
                    {
                        break;
                    }

                    usedCells.Add(spawnPosition.Value);
                    PlaceObject(spawnRule.type, spawnPosition.Value, room, random);
                }

                // 追加スポーン（minCount を超えた分を ratePer10000 で 1 体ずつ抽選）
                for (var i = spawnRule.minCount; i < spawnRule.maxCount; i++)
                {
                    var roll = random.Next(0, 10000);
                    if (roll >= spawnRule.ratePer10000)
                    {
                        // 外れたら追加スポーンを打ち切る
                        break;
                    }

                    var spawnPosition = PickFreeCell(room, usedCells, random);
                    if (spawnPosition == null)
                    {
                        break;
                    }

                    usedCells.Add(spawnPosition.Value);
                    PlaceObject(spawnRule.type, spawnPosition.Value, room, random);
                }
            }
        }

        /// <summary>
        /// 部屋の CellEntities から使用済みでないセルをランダムに選んで返す。
        /// すべてのセルが使用済みの場合は null を返す。
        ///
        /// ロジックは RoomFreeCellPicker に委譲する。
        /// </summary>
        private Vector2Int? PickFreeCell(DungeonRoomEntity room, HashSet<Vector2Int> usedCells, System.Random random)
        {
            return roomFreeCellPicker.Pick(room, usedCells, random);
        }

        /// <summary>
        /// スポーン種別に応じてプレハブを Instantiate する。
        /// 宝箱の場合はスポーン後に TreasureBoxController.Initialize() で中身を確定する。
        /// </summary>
        private void PlaceObject(string spawnType, Vector2Int gridPos, DungeonRoomEntity room, System.Random random)
        {
            var worldPos = GetWorldPosition(gridPos, room);

            switch (spawnType)
            {
                case "Enemy":
                    PlaceEnemy(worldPos, random);
                    break;
                case "TreasureBox":
                    PlaceTreasureBox(worldPos, random, room.RoomGridPosition);
                    break;
                case "Misc":
                    PlaceMisc(worldPos, room.RoomGridPosition);
                    break;
                default:
                    UnityEngine.Debug.LogWarning($"[RoomObjectSpawner] 未知のスポーン種別: {spawnType}");
                    break;
            }
        }

        /// <summary>
        /// spawnableEnemyUids からランダムに 1 種を選び、EnemyVirusController 経由でスポーンする。
        /// 1 種類のみ設定されていれば毎回同じ種類が出る。
        ///
        /// 強化は累計ダンジョン生成回数のマイルストーン（EnemyEnhancementParameter）に基づく加算式。
        /// </summary>
        private void PlaceEnemy(Vector3 worldPos, System.Random random)
        {
            if (enemyVirusController == null)
            {
                UnityEngine.Debug.LogWarning("[RoomObjectSpawner] enemyVirusController が未設定のためスキップします。");
                return;
            }

            if (spawnableEnemyUids == null || spawnableEnemyUids.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[RoomObjectSpawner] spawnableEnemyUids が未設定のためスキップします。");
                return;
            }

            var selectedUid = spawnableEnemyUids[random.Next(0, spawnableEnemyUids.Length)];
            if (string.IsNullOrEmpty(selectedUid))
            {
                UnityEngine.Debug.LogWarning("[RoomObjectSpawner] 選択された enemyUid が空のためスキップします。");
                return;
            }

            // 累計ダンジョン生成回数からマイルストーン方式（加算式）で強化値を求める
            var generationCount = dungeonController != null ? dungeonController.GenerationCount : 0;
            var enhancement = enemyEnhancementUseCase.Execute(generationCount, enemyEnhancementParameter);
            // EnemyVirusController 経由でスポーンすることで、HP・行動パターン・武器の初期化が保証される
            var success = enemyVirusController.TryRequestSpawn(selectedUid, worldPos, enhancement.HpBonus, enhancement.AttackPowerBonus, enhancement.DropRateMultiplier);
            if (!success)
            {
                UnityEngine.Debug.LogWarning($"[RoomObjectSpawner] 敵のスポーンに失敗しました。uid={selectedUid}, pos={worldPos}, hpBonus={enhancement.HpBonus}, attackPowerBonus={enhancement.AttackPowerBonus}");
            }
            else
            {
                UnityEngine.Debug.Log($"[RoomObjectSpawner] 敵をスポーンしました。uid={selectedUid}, pos={worldPos}, 生成回数={generationCount}, HPボーナス={enhancement.HpBonus}, 攻撃力ボーナス={enhancement.AttackPowerBonus}");
            }
        }        /// <summary>
        /// 宝箱をスポーンし、中身を重みテーブルからランダムに確定して TreasureBoxController に渡す。
        /// treasureItemWeights が未設定の場合は空の宝箱になる。
        /// </summary>
        private void PlaceTreasureBox(Vector3 worldPos, System.Random random, Vector2Int roomGridPosition)
        {
            if (treasureBoxPrefab == null)
            {
                UnityEngine.Debug.LogWarning("[RoomObjectSpawner] treasureBoxPrefab が未設定のためスキップします。");
                return;
            }

            var spawnedBox = Instantiate(treasureBoxPrefab, worldPos, Quaternion.identity);
            spawnedObjectRegistry.Register(roomGridPosition, spawnedBox);
            var controller = spawnedBox.GetComponent<TreasureBoxController>();

            if (controller == null)
            {
                UnityEngine.Debug.LogWarning("[RoomObjectSpawner] treasureBoxPrefab に TreasureBoxController がアタッチされていません。");
                return;
            }

            var treasureContents = SelectTreasureContents(random);
            controller.Initialize(treasureContents);
            UnityEngine.Debug.Log($"[RoomObjectSpawner] 宝箱をスポーン: 中身={treasureContents.ContentsType}, world={worldPos}");
        }

        /// <summary>
        /// treasureItemWeights の万分率テーブル（ratePer10000）を参照して宝箱の中身を決定する。
        /// 抽選ロジックは TreasureContentsSelector に委譲する。
        /// </summary>
        private TreasureContents SelectTreasureContents(System.Random random)
        {
            return treasureContentsSelector.Select(treasureItemWeights, random);
        }

        /// <summary>
        /// 雑貨プレハブ配列からランダムに 1 種を選んでスポーンする。
        /// </summary>
        private void PlaceMisc(Vector3 worldPos, Vector2Int roomGridPosition)
        {
            if (miscPrefabs == null || miscPrefabs.Length == 0)
            {
                UnityEngine.Debug.LogWarning("[RoomObjectSpawner] miscPrefabs が未設定のためスキップします。");
                return;
            }

            var miscIndex = Random.Range(0, miscPrefabs.Length);
            if (miscPrefabs[miscIndex] == null)
            {
                return;
            }

            var spawnedMisc = Instantiate(miscPrefabs[miscIndex], worldPos, Quaternion.identity);
            spawnedObjectRegistry.Register(roomGridPosition, spawnedMisc);
        }

        /// <summary>
        /// gridPos に対応する WorldPosition を計算して返す。
        /// DungeonRoomEntityFactory と同じ座標系を使用し、セルの XZ 平面での中心座標を返す。
        /// Y 座標は床面（タイル上面）に合わせるために cellSize / 2 = 0.5f に設定する。
        ///
        /// セルの中心に正確にスポーンすることで、以下の問題を解決する：
        ///   - プレイヤーと同じグリッド座標系に乗るため、GridMover の障害物判定が正しく機能する
        ///   - 敵の初期 GridMover 座標が正確になり、移動時の境界チェックが正しく動作する
        /// </summary>
        private static Vector3 GetWorldPosition(Vector2Int gridPos, DungeonRoomEntity room)
        {
            const float cellSize = 1f; // DungeonCellParameter.CellSize と同じ値（ハードコード）
            const float floorSurfaceOffset = 0.5f; // 床面（タイル上面）のオフセット

            // GridPosition から直接セル中心のワールド座標を計算
            // DungeonRoomEntityFactory.Create() と同じ計算式を使用
            return new Vector3(gridPos.x * cellSize, floorSurfaceOffset, gridPos.y * cellSize);
        }
    }
}
