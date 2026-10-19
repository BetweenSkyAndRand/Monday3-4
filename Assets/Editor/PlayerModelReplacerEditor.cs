using UnityEngine;
using UnityEditor;
using Game.Player;
using Game.Player.Domain;

namespace MapTest.EditorTools
{
    /// <summary>
    /// プレイヤーモデル置き換えツールのEditorWindow。
    /// Tools > Player > Model Replacer からアクセス可能。
    /// 任意の3DモデルをCapsuleプレイヤーと同じサイズ（1×1×2セル）に自動調整して置き換える。
    /// </summary>
    public class PlayerModelReplacerEditor : EditorWindow
    {
        private GameObject targetPlayerObject;
        private GameObject modelPrefab;
        private PlayerModelConfig config;
        private bool autoFindPlayer = true;
        private Vector2 scrollPosition;

        [MenuItem("Tools/4. Player Tools/01. Model Replacer")]
        public static void ShowWindow()
        {
            var window = GetWindow<PlayerModelReplacerEditor>("プレイヤーモデル置き換え");
            window.minSize = new Vector2(450, 600);
            window.Show();
        }

        private void OnEnable()
        {
            // デフォルトConfigをロード（存在しない場合は一時作成）
            LoadOrCreateDefaultConfig();

            // シーン内のPlayerを自動検索
            if (autoFindPlayer)
            {
                FindPlayerInScene();
            }
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            DrawHeader();
            EditorGUILayout.Space(10);

            DrawPlayerSection();
            EditorGUILayout.Space(10);

            DrawModelSection();
            EditorGUILayout.Space(10);

            DrawConfigSection();
            EditorGUILayout.Space(10);

            DrawReplaceButton();
            EditorGUILayout.Space(10);

            DrawHelpSection();

            EditorGUILayout.EndScrollView();
        }

        private void DrawHeader()
        {
            GUILayout.Label("プレイヤーモデル置き換えツール", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "任意の3Dモデルを現在のプレイヤー（Capsule）と同じサイズに自動調整して置き換えます。\n" +
                "既存のゲームロジック（移動、入力、攻撃等）は保持されます。",
                MessageType.Info);
        }

        private void DrawPlayerSection()
        {
            GUILayout.Label("1. 対象プレイヤー", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            autoFindPlayer = EditorGUILayout.Toggle("自動検索", autoFindPlayer);
            if (GUILayout.Button("再検索", GUILayout.Width(80)))
            {
                FindPlayerInScene();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUI.BeginDisabledGroup(autoFindPlayer);
            targetPlayerObject = (GameObject)EditorGUILayout.ObjectField(
                "対象プレイヤー",
                targetPlayerObject,
                typeof(GameObject),
                true);
            EditorGUI.EndDisabledGroup();

            if (targetPlayerObject != null)
            {
                EditorGUILayout.HelpBox(
                    $"✓ プレイヤー検出: {targetPlayerObject.name}\n" +
                    $"位置: {targetPlayerObject.transform.position}",
                    MessageType.None);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "シーン内にプレイヤーが見つかりません。\n" +
                    "GameSceneを開いているか確認してください。",
                    MessageType.Warning);
            }
        }

        private void DrawModelSection()
        {
            GUILayout.Label("2. 新しいモデル", EditorStyles.boldLabel);

            modelPrefab = (GameObject)EditorGUILayout.ObjectField(
                "モデルPrefab",
                modelPrefab,
                typeof(GameObject),
                false);

            if (modelPrefab != null)
            {
                // モデル情報を表示
                var hasRenderer = modelPrefab.GetComponentsInChildren<Renderer>(true).Length > 0;
                var hasAnimator = modelPrefab.GetComponent<Animator>() != null;

                EditorGUILayout.HelpBox(
                    $"モデル: {modelPrefab.name}\n" +
                    $"Renderer: {(hasRenderer ? "✓" : "✗")}\n" +
                    $"Animator: {(hasAnimator ? "✓" : "✗")}",
                    hasRenderer ? MessageType.None : MessageType.Warning);

                // クイック選択ボタン
                EditorGUILayout.BeginHorizontal();
                if (GUILayout.Button("SpaceMan"))
                {
                    modelPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
                        "Assets/FreeAnimatedSpaceMan/Prefab/space_man_model.prefab");
                }
                EditorGUILayout.EndHorizontal();
            }
        }

        private void DrawConfigSection()
        {
            GUILayout.Label("3. 設定", EditorStyles.boldLabel);

            config = (PlayerModelConfig)EditorGUILayout.ObjectField(
                "設定アセット",
                config,
                typeof(PlayerModelConfig),
                false);

            if (config == null)
            {
                EditorGUILayout.HelpBox(
                    "設定が指定されていません。デフォルト設定を使用します。",
                    MessageType.Info);

                if (GUILayout.Button("デフォルト設定を作成"))
                {
                    CreateDefaultConfig();
                }
            }
            else
            {
                // 設定内容を表示
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("ターゲットサイズ", $"{config.TargetWidth} × {config.TargetHeight} × {config.TargetDepth}");
                EditorGUILayout.LabelField("アスペクト比維持", config.PreserveAspectRatio ? "有効" : "無効");
                EditorGUILayout.LabelField("地面合わせ", config.AlignToGround ? "有効" : "無効");
                EditorGUILayout.LabelField("マテリアル自動適用", config.AutoApplyMaterials ? "有効" : "無効");
                EditorGUI.indentLevel--;
            }
        }

        private void DrawReplaceButton()
        {
            var canExecute = targetPlayerObject != null && modelPrefab != null;

            EditorGUI.BeginDisabledGroup(!canExecute);

            var buttonStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                fixedHeight = 40
            };

            if (GUILayout.Button("モデルを置き換える", buttonStyle))
            {
                ExecuteReplace();
            }

            EditorGUI.EndDisabledGroup();

            if (!canExecute)
            {
                EditorGUILayout.HelpBox(
                    "プレイヤーとモデルを指定してください。",
                    MessageType.Warning);
            }
        }

        private void DrawHelpSection()
        {
            GUILayout.Label("ヘルプ", EditorStyles.boldLabel);

            EditorGUILayout.HelpBox(
                "【使い方】\n" +
                "1. GameSceneを開きます\n" +
                "2. 置き換えるモデルのPrefabを選択\n" +
                "3. 「モデルを置き換える」ボタンをクリック\n\n" +
                "【注意】\n" +
                "• 既存のCapsuleビジュアルは削除されます\n" +
                "• ゲームロジック（移動、入力等）は保持されます\n" +
                "• Ctrl+Zで元に戻せます",
                MessageType.Info);
        }

        private void ExecuteReplace()
        {
            if (!ValidateInput())
            {
                return;
            }

            if (!EditorUtility.DisplayDialog(
                "確認",
                $"プレイヤーモデルを置き換えます。\n\n" +
                $"対象: {targetPlayerObject.name}\n" +
                $"新モデル: {modelPrefab.name}\n\n" +
                $"この操作はUndo（Ctrl+Z）で元に戻せます。\n" +
                $"続行しますか？",
                "はい",
                "キャンセル"))
            {
                return;
            }

            // 設定がない場合は一時的なデフォルト設定を使用
            var usedConfig = config != null ? config : CreateTemporaryConfig();

            // ユースケースを実行
            var useCase = new PlayerModelReplacementUseCase();
            var result = useCase.Execute(targetPlayerObject, modelPrefab, usedConfig);

            if (result.Success)
            {
                EditorUtility.DisplayDialog(
                    "成功",
                    $"モデルの置き換えが完了しました！\n\n" +
                    $"新しいモデル: {result.InstalledModel.name}\n" +
                    $"スケール: {result.InstalledModel.transform.localScale}\n\n" +
                    $"Ctrl+Zで元に戻せます。",
                    "OK");
            }
            else
            {
                EditorUtility.DisplayDialog(
                    "エラー",
                    $"モデルの置き換えに失敗しました。\n\n{result.Message}",
                    "OK");
            }
        }

        private bool ValidateInput()
        {
            if (targetPlayerObject == null)
            {
                EditorUtility.DisplayDialog("エラー", "対象プレイヤーが指定されていません。", "OK");
                return false;
            }

            if (modelPrefab == null)
            {
                EditorUtility.DisplayDialog("エラー", "新しいモデルが指定されていません。", "OK");
                return false;
            }

            // Rendererの存在チェック
            if (modelPrefab.GetComponentsInChildren<Renderer>(true).Length == 0)
            {
                if (!EditorUtility.DisplayDialog(
                    "警告",
                    "選択されたモデルにRendererが見つかりません。\n" +
                    "このモデルで続行しますか？",
                    "続行",
                    "キャンセル"))
                {
                    return false;
                }
            }

            return true;
        }

        private void FindPlayerInScene()
        {
            // PlayerGridMoverを持つオブジェクトを検索
            var playerGridMoverType = System.Type.GetType("Game.Player.Script.PlayerGridMover");
            if (playerGridMoverType != null)
            {
                var player = FindFirstObjectByType(playerGridMoverType) as Component;
                if (player != null)
                {
                    targetPlayerObject = player.gameObject;
                    Debug.Log($"[PlayerModelReplacer] プレイヤーを検出: {targetPlayerObject.name}");
                    return;
                }
            }

            // Playerタグで検索
            var playerByTag = GameObject.FindGameObjectWithTag("Player");
            if (playerByTag != null)
            {
                targetPlayerObject = playerByTag;
                Debug.Log($"[PlayerModelReplacer] プレイヤーをタグで検出: {targetPlayerObject.name}");
                return;
            }

            // 名前で検索
            var playerByName = GameObject.Find("Player");
            if (playerByName != null)
            {
                targetPlayerObject = playerByName;
                Debug.Log($"[PlayerModelReplacer] プレイヤーを名前で検出: {targetPlayerObject.name}");
                return;
            }

            Debug.LogWarning("[PlayerModelReplacer] シーン内にプレイヤーが見つかりませんでした");
            targetPlayerObject = null;
        }

        private void LoadOrCreateDefaultConfig()
        {
            // Assets/Game/Player/DefaultPlayerModelConfig.asset を探す
            var configPath = "Assets/Game/Player/DefaultPlayerModelConfig.asset";
            config = AssetDatabase.LoadAssetAtPath<PlayerModelConfig>(configPath);

            if (config == null)
            {
                Debug.Log("[PlayerModelReplacer] デフォルト設定が見つかりません（実行時に一時作成されます）");
            }
        }

        private void CreateDefaultConfig()
        {
            var configPath = "Assets/Game/Player/DefaultPlayerModelConfig.asset";

            // フォルダが存在しない場合は作成
            if (!AssetDatabase.IsValidFolder("Assets/Game/Player"))
            {
                if (!AssetDatabase.IsValidFolder("Assets/Game"))
                {
                    AssetDatabase.CreateFolder("Assets", "Game");
                }
                AssetDatabase.CreateFolder("Assets/Game", "Player");
            }

            // ScriptableObjectを作成
            config = CreateInstance<PlayerModelConfig>();
            AssetDatabase.CreateAsset(config, configPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"[PlayerModelReplacer] デフォルト設定を作成しました: {configPath}");
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = config;
        }

        private PlayerModelConfig CreateTemporaryConfig()
        {
            var tempConfig = CreateInstance<PlayerModelConfig>();
            // デフォルト値はScriptableObjectで設定済み
            return tempConfig;
        }
    }
}



