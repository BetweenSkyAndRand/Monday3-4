using TMPro;
using Game.Inventory;
using Game.Inventory.Parameter;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
// ⚠️ このツールが触れる範囲：
//   - PlayerInventoryDisplayParameter.asset / StorageInventoryDisplayParameter.asset の新規作成
//   - 指定した Canvas への InventoryPanel / StoragePanel
//   - ItemSlotPrefab.prefab の新規作成または上書き作成
//   - StoragePanel への InventoryView アタッチ + StorageInventoryController 全フィールドアサイン
//
// ◎ 対象 Canvas を ObjectField で指定してから各ボタンを実行してください。
//   既存のオブジェクトが指定された場合は、不足しているコンポーネントのみ追加します（上書き作成なし）。
// ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━

namespace Editor
{
    public class InventoryUISetupWindow : EditorWindow
    {
        private const string PlayerParameterPath = "Assets/Game/Inventory/Parameter/PlayerInventoryDisplayParameter.asset";
        private const string StorageParameterPath = "Assets/Game/Inventory/Parameter/StorageInventoryDisplayParameter.asset";
        private const string ItemSlotPrefabPath = "Assets/Game/Inventory/Prefabs/ItemSlotPrefab.prefab";

        // ユーザーが指定する対象 Canvas（ObjectField で設定する）
        private GameObject inventoryCanvasTarget;
        private GameObject storageCanvasTarget;
        private GameObject hudCanvasTarget;

        // Step F 用：InventoryView フィールドアサイン確認に使う対象 Canvas
        private GameObject inventoryViewAssignTarget;

        private Vector2 scrollPosition;

        [MenuItem("Tools/2. Asset Creation/03. Inventory UI Setup Window")]
        public static void ShowWindow()
        {
            var window = GetWindow<InventoryUISetupWindow>("Inventory UI Setup");
            window.minSize = new Vector2(520, 740);
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            EditorGUILayout.HelpBox(
                "⚠️ 対象 Canvas を ObjectField で指定してから各ボタンを実行してください。\n" +
                "既存オブジェクトへの再適用も可能です。実行前に必ず Ctrl+S でシーンを保存してください。",
                MessageType.Warning);

            EditorGUILayout.Space(6);
            DrawSeparator("Step A: ItemSlotPrefab を作成");
            DrawSectionA();

            EditorGUILayout.Space(6);
            DrawSeparator("Step B: InventoryCanvas をセットアップ");
            DrawSectionB();

            EditorGUILayout.Space(6);
            DrawSeparator("Step C: StorageCanvas を全自動セットアップ ★");
            DrawSectionC();

            EditorGUILayout.Space(6);
            DrawSeparator("Step D: HUDCanvas をセットアップ");
            DrawSectionD();

            EditorGUILayout.Space(6);
            DrawSeparator("Step E: InventoryDisplayParameter アセット作成");
            DrawSectionE();

            EditorGUILayout.Space(6);
            DrawSeparator("Step F: InventoryView フィールドアサイン確認・再適用");
            DrawSectionF();

            EditorGUILayout.EndScrollView();
        }

        private static void DrawSeparator(string title)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
        }

        // ═══════════════════════════════════════════════════════════════
        // Step A: ItemSlotPrefab
        // ═══════════════════════════════════════════════════════════════

        private static void DrawSectionA()
        {
            var exists = AssetDatabase.AssetPathToGUID(ItemSlotPrefabPath) != string.Empty;
            if (exists)
            {
                EditorGUILayout.HelpBox($"✅ ItemSlotPrefab はすでに存在します。\n{ItemSlotPrefabPath}", MessageType.Info);
                if (GUILayout.Button("ItemSlotPrefab を上書き再作成する", GUILayout.Height(28)))
                {
                    if (EditorUtility.DisplayDialog("ItemSlotPrefab 上書き再作成",
                        $"既存の ItemSlotPrefab を上書きします:\n{ItemSlotPrefabPath}\n\n実行しますか？",
                        "上書きする", "キャンセル"))
                    {
                        CreateItemSlotPrefab();
                    }
                }

                return;
            }

            EditorGUILayout.HelpBox($"作成先: {ItemSlotPrefabPath}", MessageType.None);
            if (GUILayout.Button("ItemSlotPrefab を作成する", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("ItemSlotPrefab 作成",
                    $"以下に新規作成します:\n{ItemSlotPrefabPath}\n実行しますか？", "作成する", "キャンセル"))
                {
                    CreateItemSlotPrefab();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Step B: InventoryCanvas
        // ═══════════════════════════════════════════════════════════════

        private void DrawSectionB()
        {
            inventoryCanvasTarget = (GameObject)EditorGUILayout.ObjectField(
                "対象 Canvas", inventoryCanvasTarget, typeof(GameObject), true);

            if (GUILayout.Button("シーンから InventoryCanvas を自動検索", GUILayout.Height(22)))
            {
                inventoryCanvasTarget = GameObject.Find("InventoryCanvas");
                if (inventoryCanvasTarget == null)
                {
                    EditorUtility.DisplayDialog("見つかりません",
                        "シーン内に 'InventoryCanvas' という名前のオブジェクトが見つかりませんでした。\n" +
                        "手動でフィールドに GameObject をドラッグしてください。", "OK");
                }
            }

            EditorGUILayout.Space(4);

            if (inventoryCanvasTarget == null)
            {
                EditorGUILayout.HelpBox(
                    "対象 Canvas を指定するか「自動検索」を押してください。\n" +
                    "指定がない場合は「新規作成」で InventoryCanvas をシーンに追加します。",
                    MessageType.Info);

                if (GUILayout.Button("新規 InventoryCanvas を作成してセットアップ", GUILayout.Height(32)))
                {
                    if (EditorUtility.DisplayDialog("InventoryCanvas 新規作成",
                        "新規 InventoryCanvas をシーンに追加してセットアップします。\n実行しますか？",
                        "作成する", "キャンセル"))
                    {
                        inventoryCanvasTarget = CreateAndSetupInventoryCanvas();
                    }
                }

                return;
            }

            // 対象が指定済み：現状を確認して表示する
            EditorGUILayout.HelpBox($"対象: {hudCanvasTarget.name}", MessageType.Info);
            if (GUILayout.Button("InventoryCanvas をセットアップ（既存に適用）", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("InventoryCanvas セットアップ",
                    $"対象: {inventoryCanvasTarget.name}\n\n" +
                    "不足しているコンポーネント/オブジェクトを追加します。\n実行しますか？",
                    "実行する", "キャンセル"))
                {
                    SetupInventoryCanvas(inventoryCanvasTarget);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Step C: StorageCanvas ★ 全自動セットアップ
        // ═══════════════════════════════════════════════════════════════

        private void DrawSectionC()
        {
            storageCanvasTarget = (GameObject)EditorGUILayout.ObjectField(
                "対象 Canvas", storageCanvasTarget, typeof(GameObject), true);

            if (GUILayout.Button("シーンから StorageCanvas を自動検索", GUILayout.Height(22)))
            {
                // StorageBox 内の Canvas も含めて検索する
                storageCanvasTarget = GameObject.Find("StorageCanvas");
                if (storageCanvasTarget == null)
                {
                    EditorUtility.DisplayDialog("見つかりません",
                        "シーン内に 'StorageCanvas' という名前のオブジェクトが見つかりませんでした。\n" +
                        "手動でフィールドに GameObject をドラッグしてください。", "OK");
                }
            }

            EditorGUILayout.Space(4);

            // StorageBox の存在チェック（アサイン先として必要なため常に表示）
            var storageBox = GameObject.Find("StorageBox");
            if (storageBox == null)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ Hierarchy に StorageBox が見つかりません。\n" +
                    "StorageBox を先に配置して StorageInventoryController をアタッチしてください。\n" +
                    "StorageBox がなくても Canvas のセットアップ自体は実行できますが、\n" +
                    "StorageInventoryController へのアサインは手動になります。",
                    MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox("✅ StorageBox を検出しました。", MessageType.Info);
            }

            EditorGUILayout.Space(2);

            if (storageCanvasTarget == null)
            {
                EditorGUILayout.HelpBox(
                    "対象 Canvas を指定するか「自動検索」を押してください。\n" +
                    "指定がない場合は「新規作成」で StorageCanvas を作成します。",
                    MessageType.Info);

                GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
                if (GUILayout.Button("★ 新規 StorageCanvas を全自動セットアップ", GUILayout.Height(40)))
                {
                    GUI.backgroundColor = Color.white;
                    if (EditorUtility.DisplayDialog("StorageCanvas 全自動セットアップ",
                        "新規 StorageCanvas を作成し、InventoryView・StorageInventoryController の\n" +
                        "フィールドをすべて自動でアサインします。\n\n実行しますか？",
                        "セットアップ開始", "キャンセル"))
                    {
                        SetupStorageCanvasFull(null);
                    }
                }

                GUI.backgroundColor = Color.white;
                return;
            }

            // 対象が指定済み：現状を確認して表示する
            EditorGUILayout.HelpBox($"対象: {hudCanvasTarget.name}", MessageType.Info);
            GUI.backgroundColor = new Color(0.4f, 0.8f, 1f);
            if (GUILayout.Button("★ StorageCanvas を全自動セットアップ（既存に適用）", GUILayout.Height(40)))
            {
                GUI.backgroundColor = Color.white;
                if (EditorUtility.DisplayDialog("StorageCanvas セットアップ",
                    $"対象: {storageCanvasTarget.name}\n\n" +
                    "不足しているオブジェクト/コンポーネント/フィールドを追加します。\n実行しますか？",
                    "セットアップ開始", "キャンセル"))
                {
                    SetupStorageCanvasFull(storageCanvasTarget);
                }
            }

            GUI.backgroundColor = Color.white;
        }

        // ═══════════════════════════════════════════════════════════════
        // Step D: HUDCanvas
        // ═══════════════════════════════════════════════════════════════

        private void DrawSectionD()
        {
            hudCanvasTarget = (GameObject)EditorGUILayout.ObjectField(
                "対象 Canvas", hudCanvasTarget, typeof(GameObject), true);

            if (GUILayout.Button("シーンから HUDCanvas を自動検索", GUILayout.Height(22)))
            {
                hudCanvasTarget = GameObject.Find("HUDCanvas");
                if (hudCanvasTarget == null)
                {
                    EditorUtility.DisplayDialog("見つかりません",
                        "シーン内に 'HUDCanvas' という名前のオブジェクトが見つかりませんでした。\n" +
                        "手動でフィールドに GameObject をドラッグしてください。", "OK");
                }
            }

            EditorGUILayout.Space(4);

            if (hudCanvasTarget == null)
            {
                EditorGUILayout.HelpBox(
                    "対象 Canvas を指定するか「自動検索」を押してください。\n" +
                    "指定がない場合は「新規作成」で HUDCanvas をシーンに追加します。",
                    MessageType.Info);

                if (GUILayout.Button("新規 HUDCanvas を作成してセットアップ", GUILayout.Height(32)))
                {
                    if (EditorUtility.DisplayDialog("HUDCanvas 新規作成",
                        "新規 HUDCanvas をシーンに追加してセットアップします。\n実行しますか？",
                        "作成する", "キャンセル"))
                    {
                        hudCanvasTarget = CreateAndSetupHUDCanvas();
                    }
                }

                return;
            }

            EditorGUILayout.HelpBox($"対象: {hudCanvasTarget.name}", MessageType.Info);
            if (GUILayout.Button("HUDCanvas をセットアップ（既存に適用）", GUILayout.Height(32)))
            {
                if (EditorUtility.DisplayDialog("HUDCanvas セットアップ",
                    $"対象: {hudCanvasTarget.name}\n\n" +
                    "不足しているオブジェクト/コンポーネントを追加します。\n実行しますか？",
                    "実行する", "キャンセル"))
                {
                    SetupHUDCanvas(hudCanvasTarget);
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // Step F: InventoryView フィールドアサイン確認・再適用
        // ═══════════════════════════════════════════════════════════════

        private void DrawSectionF()
        {
            inventoryViewAssignTarget = (GameObject)EditorGUILayout.ObjectField(
                "対象 Canvas (InventoryCanvas)", inventoryViewAssignTarget, typeof(GameObject), true);

            if (GUILayout.Button("シーンから InventoryCanvas を自動検索", GUILayout.Height(22)))
            {
                inventoryViewAssignTarget = GameObject.Find("InventoryCanvas");
                if (inventoryViewAssignTarget == null)
                {
                    EditorUtility.DisplayDialog("見つかりません",
                        "シーン内に 'InventoryCanvas' という名前のオブジェクトが見つかりませんでした。",
                        "OK");
                }
            }

            EditorGUILayout.Space(4);

            if (inventoryViewAssignTarget == null)
            {
                EditorGUILayout.HelpBox("対象 Canvas を指定するか「自動検索」を押してください。", MessageType.Info);
                return;
            }

            // InventoryPanel と InventoryView の状態を確認する
            var panelTransform = inventoryViewAssignTarget.transform.Find("InventoryPanel");
            if (panelTransform == null)
            {
                EditorGUILayout.HelpBox(
                    $"⚠️ {inventoryViewAssignTarget.name} 内に InventoryPanel が見つかりません。\n" +
                    "先に Step B でセットアップしてください。",
                    MessageType.Warning);
                return;
            }

            var panelGo = panelTransform.gameObject;
            var inventoryViewType = GetTypeByName("Game.Inventory.InventoryView");
            if (inventoryViewType == null)
            {
                EditorGUILayout.HelpBox("⚠️ InventoryView クラスが見つかりません。", MessageType.Error);
                return;
            }

            var inventoryView = (InventoryView)panelGo.GetComponent(inventoryViewType);
            if (inventoryView == null)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ InventoryPanel に InventoryView がアタッチされていません。\n" +
                    "Step B を再実行してください。",
                    MessageType.Warning);
                return;
            }

            // 現在のアサイン状態を表示する
            var serializedView = new SerializedObject(inventoryView);
            var propPrefab = serializedView.FindProperty("itemSlotPrefab");
            var propParent = serializedView.FindProperty("itemSlotParent");
            var propRoot = serializedView.FindProperty("inventoryRoot");
            var propEquip = serializedView.FindProperty("equipButton");

            var statusLines = BuildFieldStatusLines(
                ("itemSlotPrefab", propPrefab.objectReferenceValue != null),
                ("itemSlotParent (SlotsContainer)", propParent.objectReferenceValue != null),
                ("inventoryRoot (InventoryPanel)", propRoot.objectReferenceValue != null),
                ("equipButton", propEquip.objectReferenceValue != null)
            );

            var allAssigned = statusLines.allAssigned;
            EditorGUILayout.HelpBox(
                $"InventoryView フィールドの状態:\n{statusLines.text}",
                allAssigned ? MessageType.Info : MessageType.Warning);

            // ActionButtons の位置も確認する
            var actionBtnTransform = panelGo.transform.Find("ActionButtons");
            if (actionBtnTransform != null)
            {
                var actionRect = actionBtnTransform.GetComponent<RectTransform>();
                // 推奨配置かどうかを anchorMin で判定する（横ストレッチなら anchorMin.x == 0）
                var isRecommendedLayout = actionRect != null && Mathf.Approximately(actionRect.anchorMin.x, 0f) && Mathf.Approximately(actionRect.anchorMax.x, 1f);
                if (isRecommendedLayout)
                {
                    EditorGUILayout.HelpBox("✅ ActionButtons は推奨レイアウト（横ストレッチ）で配置されています。", MessageType.Info);
                }
                else
                {
                    EditorGUILayout.HelpBox(
                        "⚠️ ActionButtons が初期値の配置（固定幅中央アンカー）のままです。\n" +
                        "「再アサイン & 位置修正」で推奨レイアウトに移動します。",
                        MessageType.Warning);
                }
            }

            EditorGUILayout.Space(4);

            GUI.backgroundColor = allAssigned ? new Color(0.6f, 1f, 0.6f) : new Color(1f, 0.8f, 0.4f);
            var btnLabel = allAssigned ? "InventoryView フィールドを再アサイン & 位置修正" : "InventoryView フィールドをアサイン & 位置修正";
            if (GUILayout.Button(btnLabel, GUILayout.Height(36)))
            {
                GUI.backgroundColor = Color.white;
                if (EditorUtility.DisplayDialog("InventoryView アサイン & 位置修正",
                    $"対象: {inventoryViewAssignTarget.name}\n\n" +
                    "InventoryView の全フィールドをアサインし、\n" +
                    "ActionButtons を推奨配置（横ストレッチ・下端 12px）に移動します。\n\n実行しますか？",
                    "実行する", "キャンセル"))
                {
                    AssignInventoryViewFields(inventoryViewAssignTarget);
                }
            }

            GUI.backgroundColor = Color.white;
        }

        /// <summary>
        /// InventoryCanvas の InventoryPanel 上にある InventoryView フィールドを全アサインし、
        /// ActionButtons を推奨配置に移動する。
        /// </summary>
        private static void AssignInventoryViewFields(GameObject canvasGo)
        {
            var itemSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemSlotPrefabPath);
            if (itemSlotPrefab == null)
            {
                EditorUtility.DisplayDialog("エラー",
                    $"ItemSlotPrefab が見つかりません:\n{ItemSlotPrefabPath}\n\nStep A で先に作成してください。",
                    "OK");
                return;
            }

            var panelTransform = canvasGo.transform.Find("InventoryPanel");
            if (panelTransform == null)
            {
                EditorUtility.DisplayDialog("エラー", "InventoryPanel が見つかりません。Step B でセットアップしてください。", "OK");
                return;
            }

            var panelGo = panelTransform.gameObject;
            var inventoryViewType = GetTypeByName("Game.Inventory.InventoryView");
            if (inventoryViewType == null)
            {
                EditorUtility.DisplayDialog("エラー", "InventoryView クラスが見つかりません。", "OK");
                return;
            }

            var inventoryView = (InventoryView)panelGo.GetComponent(inventoryViewType);
            if (inventoryView == null)
            {
                EditorUtility.DisplayDialog("エラー", "InventoryPanel に InventoryView がアタッチされていません。Step B を再実行してください。", "OK");
                return;
            }

            // SlotsContainer を取得する
            var slotsContainerTransform = panelGo.transform.Find("SlotsContainer");

            // ActionButtons を取得して推奨位置に移動する
            var actionBtnTransform = panelGo.transform.Find("ActionButtons");
            Button bEquip = null;
            if (actionBtnTransform != null)
            {
                var actionRect = actionBtnTransform.GetComponent<RectTransform>();
                if (actionRect != null)
                {
                    // 推奨配置：横ストレッチ・下端 12px
                    Undo.RecordObject(actionRect, "Move ActionButtons to Recommended Layout");
                    actionRect.anchorMin = new Vector2(0f, 0f);
                    actionRect.anchorMax = new Vector2(1f, 0f);
                    actionRect.pivot = new Vector2(0.5f, 0f);
                    actionRect.sizeDelta = new Vector2(-20f, 46f);
                    actionRect.anchoredPosition = new Vector2(0f, 12f);
                }

                bEquip = actionBtnTransform.Find("EquipButton")?.GetComponent<Button>();
            }

            // SlotsContainer を推奨配置に移動する（初期値で作られていた場合に修正する）
            if (slotsContainerTransform != null)
            {
                var slotsRect = slotsContainerTransform.GetComponent<RectTransform>();
                if (slotsRect != null && Mathf.Approximately(slotsRect.anchorMin.x, slotsRect.anchorMax.x))
                {
                    // 固定幅アンカーのままなら推奨配置に更新する
                    Undo.RecordObject(slotsRect, "Move SlotsContainer to Recommended Layout");
                    slotsRect.anchorMin = new Vector2(0f, 0.5f);
                    slotsRect.anchorMax = new Vector2(1f, 0.5f);
                    slotsRect.pivot = new Vector2(0.5f, 0.5f);
                    slotsRect.sizeDelta = new Vector2(-20f, 100f);
                    slotsRect.anchoredPosition = new Vector2(0f, 20f);
                }
            }

            // InventoryView フィールドをアサインする
            Undo.RecordObject(inventoryView, "Assign InventoryView Fields");
            var serializedView = new SerializedObject(inventoryView);
            serializedView.FindProperty("itemSlotPrefab").objectReferenceValue = itemSlotPrefab;
            if (slotsContainerTransform != null)
            {
                serializedView.FindProperty("itemSlotParent").objectReferenceValue = slotsContainerTransform.transform;
            }

            serializedView.FindProperty("inventoryRoot").objectReferenceValue = panelGo;
            if (bEquip != null)
            {
                serializedView.FindProperty("equipButton").objectReferenceValue = bEquip;
            }

            serializedView.ApplyModifiedPropertiesWithoutUndo();

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("完了",
                $"InventoryView フィールドのアサインと位置修正が完了しました。\n対象: {canvasGo.name}\n\n" +
                "✅ itemSlotPrefab アサイン\n" +
                "✅ itemSlotParent (SlotsContainer) アサイン\n" +
                "✅ inventoryRoot (InventoryPanel) アサイン\n" +
                (bEquip != null ? "✅" : "⚠️") + " equipButton\n" +
                "✅ ActionButtons → 推奨配置（横ストレッチ・下端 12px）\n" +
                "✅ SlotsContainer → 推奨配置（横ストレッチ・中央 +20px）\n\n" +
                "⚠️ アサインが ⚠️ のボタンは ActionButtons 配下に存在しませんでした。\n手動でアサインしてください。\n\n" +
                "Ctrl+S でシーンを保存してください。",
                "OK");
            Debug.Log("[InventoryUISetup] InventoryView の全フィールドをアサインし、ActionButtons を推奨配置に移動しました。");
        }

        /// <summary>
        /// フィールドのアサイン状態を確認用テキストとして構築する。
        /// </summary>
        private static (string text, bool allAssigned) BuildFieldStatusLines(params (string name, bool assigned)[] fields)
        {
            var sb = new System.Text.StringBuilder();
            var allAssigned = true;
            foreach (var (name, assigned) in fields)
            {
                sb.AppendLine(assigned ? $"  ✅ {name}" : $"  ❌ {name}（未アサイン）");
                if (!assigned)
                {
                    allAssigned = false;
                }
            }

            return (sb.ToString().TrimEnd(), allAssigned);
        }

        // ═══════════════════════════════════════════════════════════════
        // Step E: ScriptableObject アセット作成
        // ═══════════════════════════════════════════════════════════════

        private static void DrawSectionE()
        {
            DrawParameterRow(
                "PlayerInventoryDisplayParameter.asset",
                PlayerParameterPath,
                "slotCount=3 / cooldown=0.2",
                CreatePlayerInventoryDisplayParameter);

            EditorGUILayout.Space(4);

            DrawParameterRow(
                "StorageInventoryDisplayParameter.asset",
                StorageParameterPath,
                "slotCount=2 / cooldown=0",
                CreateStorageInventoryDisplayParameter);
        }

        private static void DrawParameterRow(string label, string path, string desc, System.Action onCreate)
        {
            var exists = AssetDatabase.AssetPathToGUID(path) != string.Empty;
            if (exists)
            {
                EditorGUILayout.HelpBox($"✅ {label} は存在します。", MessageType.Info);
                return;
            }

            EditorGUILayout.HelpBox($"作成先: {path}\n設定値: {desc}", MessageType.None);
            if (GUILayout.Button($"{label} を作成する", GUILayout.Height(30)))
            {
                if (EditorUtility.DisplayDialog($"{label} 作成",
                    $"以下に新規作成します:\n{path}\n実行しますか？", "作成する", "キャンセル"))
                {
                    onCreate?.Invoke();
                }
            }
        }

        // ═══════════════════════════════════════════════════════════════
        // InventoryCanvas セットアップ実装
        // ═══════════════════════════════════════════════════════════════

        /// <summary>新規 InventoryCanvas を作成してセットアップを適用する。</summary>
        private static GameObject CreateAndSetupInventoryCanvas()
        {
            var canvasGo = new GameObject("InventoryCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create InventoryCanvas");
            SetupInventoryCanvas(canvasGo);
            return canvasGo;
        }

        /// <summary>
        /// 指定した GameObject に InventoryCanvas 構造をセットアップする。
        /// 既存コンポーネント/子オブジェクトが存在する場合は追加をスキップして再利用する。
        /// InventoryView の全フィールドも自動アサインする。
        /// </summary>
        private static void SetupInventoryCanvas(GameObject canvasGo)
        {
            // ─── 1. ItemSlotPrefab をロード ─────────────────────────────
            var itemSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemSlotPrefabPath);
            if (itemSlotPrefab == null)
            {
                EditorUtility.DisplayDialog("エラー",
                    $"ItemSlotPrefab が見つかりません:\n{ItemSlotPrefabPath}\n\nStep A で先に作成してください。",
                    "OK");
                return;
            }

            // ─── 2. Canvas コンポーネントがなければ追加する ─────────────
            if (canvasGo.GetComponent<Canvas>() == null)
            {
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 10;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // ─── 3. InventoryPanel を取得または作成する ──────────────────
            var panelTransform = canvasGo.transform.Find("InventoryPanel");
            GameObject panelGo;
            if (panelTransform != null)
            {
                panelGo = panelTransform.gameObject;
                Debug.Log("[InventoryUISetup] InventoryPanel は既に存在します。コンポーネントのみ確認します。");
            }
            else
            {
                panelGo = new GameObject("InventoryPanel");
                Undo.RegisterCreatedObjectUndo(panelGo, "Create InventoryPanel");
                panelGo.transform.SetParent(canvasGo.transform, false);
                var panelRect = panelGo.AddComponent<RectTransform>();
                panelRect.sizeDelta = new Vector2(560, 220);
                panelRect.anchorMin = new Vector2(0.5f, 0f);
                panelRect.anchorMax = new Vector2(0.5f, 0f);
                panelRect.pivot = new Vector2(0.5f, 0f);
                panelRect.anchoredPosition = new Vector2(0, 20);
                panelGo.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.7f);
            }

            // ─── 4. InventoryView が存在しなければ追加する ───────────────
            var inventoryViewType = GetTypeByName("Game.Inventory.InventoryView");
            InventoryView inventoryView = null;
            if (inventoryViewType != null)
            {
                inventoryView = (InventoryView)panelGo.GetComponent(inventoryViewType);
                if (inventoryView == null)
                {
                    inventoryView = (InventoryView)panelGo.AddComponent(inventoryViewType);
                }
            }

            // ─── 5. SlotsContainer を取得または作成する ─────────────────
            //        推奨配置：パネル幅フルストレッチ／縦中央寄り・上オフセット
            var slotsContainerTransform = panelGo.transform.Find("SlotsContainer");
            GameObject slotsContainerGo;
            if (slotsContainerTransform != null)
            {
                slotsContainerGo = slotsContainerTransform.gameObject;
            }
            else
            {
                slotsContainerGo = new GameObject("SlotsContainer");
                Undo.RegisterCreatedObjectUndo(slotsContainerGo, "Create SlotsContainer");
                slotsContainerGo.transform.SetParent(panelGo.transform, false);
                var slotsRect = slotsContainerGo.AddComponent<RectTransform>();
                // 推奨配置：横幅をパネルにストレッチ、縦は中心アンカー
                slotsRect.anchorMin = new Vector2(0f, 0.5f);
                slotsRect.anchorMax = new Vector2(1f, 0.5f);
                slotsRect.pivot = new Vector2(0.5f, 0.5f);
                slotsRect.sizeDelta = new Vector2(-20f, 100f);    // 左右 10px パディング
                slotsRect.anchoredPosition = new Vector2(0f, 20f); // 中心より 20px 上
                var hLayout = slotsContainerGo.AddComponent<HorizontalLayoutGroup>();
                hLayout.spacing = 10;
                hLayout.childAlignment = TextAnchor.MiddleCenter;
                hLayout.childForceExpandWidth = false;
                hLayout.childForceExpandHeight = false;
            }

            // ─── 6. ActionButtons を取得または作成する ───────────────────
            //        推奨配置：パネル幅フルストレッチ／下端から 12px
            var actionBtnTransform = panelGo.transform.Find("ActionButtons");
            GameObject actionButtonsGo;
            // bEquip は if-else の両ブランチで必ず代入される
            Button bEquip;
            if (actionBtnTransform != null)
            {
                actionButtonsGo = actionBtnTransform.gameObject;
                // 推奨配置に移動する（既存でも位置を上書きして整列させる）
                var actionRect = actionButtonsGo.GetComponent<RectTransform>();
                if (actionRect != null)
                {
                    actionRect.anchorMin = new Vector2(0f, 0f);
                    actionRect.anchorMax = new Vector2(1f, 0f);
                    actionRect.pivot = new Vector2(0.5f, 0f);
                    actionRect.sizeDelta = new Vector2(-20f, 46f);  // 左右 10px パディング
                    actionRect.anchoredPosition = new Vector2(0f, 12f);
                }

                // 既存のボタン参照を取得する
                bEquip = actionBtnTransform.Find("EquipButton")?.GetComponent<Button>();
            }
            else
            {
                actionButtonsGo = new GameObject("ActionButtons");
                Undo.RegisterCreatedObjectUndo(actionButtonsGo, "Create ActionButtons");
                actionButtonsGo.transform.SetParent(panelGo.transform, false);
                var actionRect = actionButtonsGo.AddComponent<RectTransform>();
                // 推奨配置：横幅をパネルにストレッチ、下端から 12px
                actionRect.anchorMin = new Vector2(0f, 0f);
                actionRect.anchorMax = new Vector2(1f, 0f);
                actionRect.pivot = new Vector2(0.5f, 0f);
                actionRect.sizeDelta = new Vector2(-20f, 46f);
                actionRect.anchoredPosition = new Vector2(0f, 12f);
                var actionLayout = actionButtonsGo.AddComponent<HorizontalLayoutGroup>();
                actionLayout.spacing = 8;
                actionLayout.childAlignment = TextAnchor.MiddleCenter;
                actionLayout.childForceExpandWidth = false;
                actionLayout.childForceExpandHeight = false;

                // ボタンを新規作成する（初期非表示：選択スロットの種類に応じてトグルされる）
                var bEquipGo = CreateTextButton(actionButtonsGo, "EquipButton", "装備する", new Color(0.27f, 0.67f, 1f), new Vector2(130f, 40f));
                bEquipGo.SetActive(false);
                bEquip = bEquipGo.GetComponent<Button>();
            }

            // ─── 7. InventoryView の全フィールドをアサインする ─────────────
            if (inventoryView != null)
            {
                var serializedView = new SerializedObject(inventoryView);
                serializedView.FindProperty("itemSlotPrefab").objectReferenceValue = itemSlotPrefab;
                serializedView.FindProperty("itemSlotParent").objectReferenceValue = slotsContainerGo.transform;
                serializedView.FindProperty("inventoryRoot").objectReferenceValue = panelGo;
                if (bEquip != null)
                {
                    serializedView.FindProperty("equipButton").objectReferenceValue = bEquip;
                }

                serializedView.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[InventoryUISetup] InventoryView の全フィールドをアサインしました。");
            }
            else
            {
                Debug.LogWarning("[InventoryUISetup] InventoryView が見つかりませんでした。手動でアタッチしてください。");
            }

            panelGo.SetActive(false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            var assignMsg = (inventoryView != null) ? "✅ InventoryView フィールドをすべて自動アサイン済み" : "⚠️ InventoryView が見つかりません。手動でアタッチしてください。";
            EditorUtility.DisplayDialog("完了",
                $"InventoryCanvas のセットアップが完了しました。\n対象: {canvasGo.name}\n\n" +
                "✅ InventoryPanel 確認/作成\n" +
                "✅ SlotsContainer 配置（横ストレッチ・中央上寄り）\n" +
                "✅ ActionButtons 配置（横ストレッチ・下端 12px）\n" +
                assignMsg + "\n\n" +
                "Ctrl+S でシーンを保存してください。",
                "OK");
        }

        // ═══════════════════════════════════════════════════════════════
        // StorageCanvas 全自動セットアップ実装
        // ═══════════════════════════════════════════════════════════════

        /// <summary>
        /// 指定した Canvas（または新規作成）に StorageCanvas 構造を全自動セットアップする。
        /// 既存コンポーネント/子オブジェクトが存在する場合は追加をスキップして再利用する。
        /// targetCanvasGo が null の場合は新規 StorageCanvas を作成する。
        /// </summary>
        private static void SetupStorageCanvasFull(GameObject targetCanvasGo)
        {
            // ─── 1. ItemSlotPrefab をロード ─────────────────────────────
            var itemSlotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ItemSlotPrefabPath);
            if (itemSlotPrefab == null)
            {
                EditorUtility.DisplayDialog("エラー",
                    $"ItemSlotPrefab が見つかりません:\n{ItemSlotPrefabPath}\n\nStep A で先に作成してください。",
                    "OK");
                return;
            }

            // ─── 2. StorageInventoryDisplayParameter をロード ────────────
            var storageParameter = AssetDatabase.LoadAssetAtPath<InventoryDisplayParameter>(StorageParameterPath);
            if (storageParameter == null)
            {
                EditorUtility.DisplayDialog("エラー",
                    $"StorageInventoryDisplayParameter が見つかりません:\n{StorageParameterPath}\n\nStep E で先に作成してください。",
                    "OK");
                return;
            }

            // ─── 3. Canvas オブジェクトを取得または新規作成 ──────────────
            var isNewCanvas = targetCanvasGo == null;
            var canvasGo = isNewCanvas ? new GameObject("StorageCanvas") : targetCanvasGo;
            if (isNewCanvas)
            {
                Undo.RegisterCreatedObjectUndo(canvasGo, "Create StorageCanvas");
            }

            // Canvas コンポーネントがなければ追加する
            if (canvasGo.GetComponent<Canvas>() == null)
            {
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 15;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            // ─── 4. StoragePanel を取得または作成 ───────────────────────
            var panelTransform = canvasGo.transform.Find("StoragePanel");
            GameObject panelGo;
            if (panelTransform != null)
            {
                panelGo = panelTransform.gameObject;
                Debug.Log("[InventoryUISetup] StoragePanel は既に存在します。コンポーネントのみ確認します。");
            }
            else
            {
                panelGo = new GameObject("StoragePanel");
                Undo.RegisterCreatedObjectUndo(panelGo, "Create StoragePanel");
                panelGo.transform.SetParent(canvasGo.transform, false);
                var panelRect = panelGo.AddComponent<RectTransform>();
                panelRect.sizeDelta = new Vector2(380, 260);
                panelRect.anchorMin = new Vector2(0.5f, 0.5f);
                panelRect.anchorMax = new Vector2(0.5f, 0.5f);
                panelRect.pivot = new Vector2(0.5f, 0.5f);
                panelRect.anchoredPosition = Vector2.zero;
                panelGo.AddComponent<Image>().color = new Color(0.102f, 0.102f, 0.176f, 0.82f);
            }

            // ─── 5. StorageSlotsParent を取得または作成 ─────────────────
            var slotsParentTransform = panelGo.transform.Find("StorageSlotsParent");
            GameObject slotsParentGo;
            if (slotsParentTransform != null)
            {
                slotsParentGo = slotsParentTransform.gameObject;
            }
            else
            {
                slotsParentGo = new GameObject("StorageSlotsParent");
                Undo.RegisterCreatedObjectUndo(slotsParentGo, "Create StorageSlotsParent");
                slotsParentGo.transform.SetParent(panelGo.transform, false);
                var slotsRect = slotsParentGo.AddComponent<RectTransform>();
                slotsRect.anchorMin = new Vector2(0.5f, 0.5f);
                slotsRect.anchorMax = new Vector2(0.5f, 0.5f);
                slotsRect.pivot = new Vector2(0.5f, 0.5f);
                slotsRect.sizeDelta = new Vector2(200, 100);
                slotsRect.anchoredPosition = new Vector2(0, 20);
                var hLayout = slotsParentGo.AddComponent<HorizontalLayoutGroup>();
                hLayout.spacing = 20;
                hLayout.childAlignment = TextAnchor.MiddleCenter;
                hLayout.childForceExpandWidth = false;
                hLayout.childForceExpandHeight = false;
            }

            // ─── 6. CloseButton を取得または作成 ────────────────────────
            var closeBtnTransform = panelGo.transform.Find("CloseButton");
            Button closeButton;
            if (closeBtnTransform != null)
            {
                closeButton = closeBtnTransform.GetComponent<Button>();
            }
            else
            {
                var closeBtnGo = new GameObject("CloseButton");
                Undo.RegisterCreatedObjectUndo(closeBtnGo, "Create CloseButton");
                closeBtnGo.transform.SetParent(panelGo.transform, false);
                var closeRect = closeBtnGo.AddComponent<RectTransform>();
                closeRect.sizeDelta = new Vector2(100, 36);
                closeRect.anchorMin = new Vector2(1f, 0f);
                closeRect.anchorMax = new Vector2(1f, 0f);
                closeRect.pivot = new Vector2(1f, 0f);
                closeRect.anchoredPosition = new Vector2(-10, 10);
                closeBtnGo.AddComponent<Image>().color = new Color(0.5f, 0.5f, 0.5f, 1f);
                closeButton = closeBtnGo.AddComponent<Button>();

                var closeLabelGo = new GameObject("Label");
                Undo.RegisterCreatedObjectUndo(closeLabelGo, "Create CloseButton Label");
                closeLabelGo.transform.SetParent(closeBtnGo.transform, false);
                var closeLabelRect = closeLabelGo.AddComponent<RectTransform>();
                closeLabelRect.anchorMin = Vector2.zero;
                closeLabelRect.anchorMax = Vector2.one;
                closeLabelRect.offsetMin = Vector2.zero;
                closeLabelRect.offsetMax = Vector2.zero;
                var closeTmp = closeLabelGo.AddComponent<TextMeshProUGUI>();
                closeTmp.text = "閉じる";
                closeTmp.fontSize = 13;
                closeTmp.alignment = TextAlignmentOptions.Center;
                closeTmp.color = Color.white;
                closeTmp.raycastTarget = false;
            }

            // ─── 7. InventoryView をアタッチ（なければ追加） ─────────────
            var inventoryViewType = GetTypeByName("Game.Inventory.InventoryView");
            InventoryView inventoryView = null;
            if (inventoryViewType != null)
            {
                inventoryView = (InventoryView)panelGo.GetComponent(inventoryViewType);
                if (inventoryView == null)
                {
                    inventoryView = (InventoryView)panelGo.AddComponent(inventoryViewType);
                }
            }
            else
            {
                Debug.LogWarning("[InventoryUISetup] InventoryView が見つかりませんでした。手動でアタッチしてください。");
            }

            // ─── 8. InventoryView フィールドをアサイン ───────────────────
            if (inventoryView != null)
            {
                var serializedView = new SerializedObject(inventoryView);
                serializedView.FindProperty("itemSlotPrefab").objectReferenceValue = itemSlotPrefab;
                serializedView.FindProperty("itemSlotParent").objectReferenceValue = slotsParentGo.transform;
                serializedView.FindProperty("inventoryRoot").objectReferenceValue = panelGo;
                // StorageCanvas にはパッチ/装備ボタンは不要なので null のまま
                serializedView.ApplyModifiedPropertiesWithoutUndo();
                Debug.Log("[InventoryUISetup] InventoryView フィールドをアサインしました。");
            }

            // ─── 9. 新規作成時のみ StorageBox の子に移動 ────────────────
            var storageBox = GameObject.Find("StorageBox");
            if (isNewCanvas)
            {
                if (storageBox != null)
                {
                    canvasGo.transform.SetParent(storageBox.transform, false);
                    Debug.Log("[InventoryUISetup] StorageCanvas を StorageBox の子に移動しました。");
                }
                else
                {
                    Debug.LogWarning("[InventoryUISetup] StorageBox が見つかりません。StorageCanvas の親設定は手動で行ってください。");
                }
            }

            // ─── 10. StorageInventoryController フィールドをアサイン ─────
            var storageControllerType = GetTypeByName("Game.InteractObj.StorageInventoryController");
            if (storageBox != null && storageControllerType != null)
            {
                var storageController = storageBox.GetComponent(storageControllerType);
                if (storageController != null && inventoryView != null)
                {
                    var serializedCtrl = new SerializedObject(storageController);
                    serializedCtrl.FindProperty("storageDisplayParameter").objectReferenceValue = storageParameter;
                    serializedCtrl.FindProperty("storageInventoryView").objectReferenceValue = inventoryView;
                    serializedCtrl.FindProperty("closeButton").objectReferenceValue = closeButton;
                    serializedCtrl.ApplyModifiedPropertiesWithoutUndo();
                    Debug.Log("[InventoryUISetup] StorageInventoryController フィールドをアサインしました。");
                }
                else if (storageController == null)
                {
                    Debug.LogWarning("[InventoryUISetup] StorageBox に StorageInventoryController がアタッチされていません。手動でアタッチ後、再実行してください。");
                }
            }

            // ─── 11. StoragePanel を初期非表示 ──────────────────────────
            panelGo.SetActive(false);

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());

            var ctrlMsg = (storageBox != null)
                ? "✅ StorageInventoryController フィールドもアサイン済み"
                : "⚠️ StorageBox 未発見のためフィールドアサインは手動で行ってください";
            EditorUtility.DisplayDialog("完了",
                $"StorageCanvas のセットアップが完了しました！\n対象: {canvasGo.name}\n\n" +
                "✅ StoragePanel 確認/作成\n" +
                "✅ StorageSlotsParent 確認/作成\n" +
                "✅ CloseButton 確認/作成\n" +
                "✅ InventoryView アタッチ/フィールドアサイン\n" +
                ctrlMsg + "\n\n" +
                "Ctrl+S でシーンを保存してください。",
                "OK");
        }

        // ═══════════════════════════════════════════════════════════════
        // HUDCanvas セットアップ実装
        // ═══════════════════════════════════════════════════════════════

        /// <summary>新規 HUDCanvas を作成してセットアップを適用する。</summary>
        private static GameObject CreateAndSetupHUDCanvas()
        {
            var canvasGo = new GameObject("HUDCanvas");
            Undo.RegisterCreatedObjectUndo(canvasGo, "Create HUDCanvas");
            SetupHUDCanvas(canvasGo);
            return canvasGo;
        }

        /// <summary>
        /// 指定した GameObject に HUDCanvas 構造をセットアップする。
        /// 既存コンポーネント/子オブジェクトが存在する場合は追加をスキップして再利用する。
        /// </summary>
        private static void SetupHUDCanvas(GameObject canvasGo)
        {
            // Canvas コンポーネントがなければ追加する
            if (canvasGo.GetComponent<Canvas>() == null)
            {
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 5;
                canvasGo.AddComponent<CanvasScaler>();
                canvasGo.AddComponent<GraphicRaycaster>();
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorUtility.DisplayDialog("完了",
                $"HUDCanvas のセットアップが完了しました。\n対象: {canvasGo.name}",
                "OK");
        }

        // ═══════════════════════════════════════════════════════════════
        // ScriptableObject 作成
        // ═══════════════════════════════════════════════════════════════

        private static void CreateItemSlotPrefab()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Game/Inventory/Prefabs"))
            {
                AssetDatabase.CreateFolder("Assets/Game/Inventory", "Prefabs");
            }

            var tempCanvas = new GameObject("__TempCanvas__");
            tempCanvas.AddComponent<Canvas>();

            var rootGo = new GameObject("ItemSlot");
            rootGo.transform.SetParent(tempCanvas.transform, false);
            Undo.RegisterCreatedObjectUndo(rootGo, "Create ItemSlot Root");

            var rootRect = rootGo.AddComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(80, 80);
            rootGo.AddComponent<Button>();
            rootGo.AddComponent<Image>().color = new Color(0.15f, 0.25f, 0.35f, 0.9f);

            var itemSlotViewType = GetTypeByName("Game.Inventory.ItemSlotView");
            if (itemSlotViewType != null)
            {
                rootGo.AddComponent(itemSlotViewType);
            }

            var highlight = CreateChildImage(rootGo, "Highlight", new Color(1f, 1f, 0f, 0.25f), new Vector2(80, 80));
            highlight.SetActive(false);

            var equippedMarker = CreateChildImage(rootGo, "EquippedMarker", new Color(0.2f, 1f, 0.2f, 1f), new Vector2(14, 14));
            var equippedRect = equippedMarker.GetComponent<RectTransform>();
            equippedRect.anchorMin = new Vector2(1f, 0f);
            equippedRect.anchorMax = new Vector2(1f, 0f);
            equippedRect.anchoredPosition = new Vector2(-6, 6);
            equippedMarker.SetActive(false);

            var slotIconGo = CreateChildImage(rootGo, "SlotIcon", Color.white, new Vector2(60, 60));
            slotIconGo.GetComponent<Image>().raycastTarget = false;

            var quantityGo = new GameObject("QuantityText");
            quantityGo.transform.SetParent(rootGo.transform, false);
            var quantityRect = quantityGo.AddComponent<RectTransform>();
            quantityRect.anchorMin = new Vector2(0.5f, 0f);
            quantityRect.anchorMax = new Vector2(1f, 0.4f);
            quantityRect.offsetMin = Vector2.zero;
            quantityRect.offsetMax = Vector2.zero;
            var tmpText = quantityGo.AddComponent<TextMeshProUGUI>();
            tmpText.text = string.Empty;
            tmpText.fontSize = 14;
            tmpText.alignment = TextAlignmentOptions.BottomRight;
            tmpText.color = Color.white;
            tmpText.raycastTarget = false;

            var prefab = PrefabUtility.SaveAsPrefabAsset(rootGo, ItemSlotPrefabPath);
            Object.DestroyImmediate(tempCanvas);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            if (prefab != null)
            {
                EditorUtility.DisplayDialog("完了",
                    $"ItemSlotPrefab を作成しました。\n{ItemSlotPrefabPath}\n\n" +
                    "※ ItemSlotView の各フィールドを Inspector で手動アサインしてください。",
                    "OK");
            }
        }

        private static void CreatePlayerInventoryDisplayParameter()
        {
            EnsureFolder("Assets/Game/Inventory/Parameter");
            var asset = ScriptableObject.CreateInstance<InventoryDisplayParameter>();
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("slotCount").intValue = 3;
            serialized.FindProperty("openCloseCooldown").floatValue = 0.2f;
            serialized.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(asset, PlayerParameterPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(asset);
            EditorUtility.DisplayDialog("完了",
                $"作成しました: {PlayerParameterPath}\nslotCount=3 / cooldown=0.2\n\n" +
                "InventoryController の displayParameter にアサインしてください。", "OK");
        }

        private static void CreateStorageInventoryDisplayParameter()
        {
            EnsureFolder("Assets/Game/Inventory/Parameter");
            var asset = ScriptableObject.CreateInstance<InventoryDisplayParameter>();
            var serialized = new SerializedObject(asset);
            serialized.FindProperty("slotCount").intValue = 2;
            serialized.FindProperty("openCloseCooldown").floatValue = 0f;
            serialized.ApplyModifiedProperties();
            AssetDatabase.CreateAsset(asset, StorageParameterPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            EditorGUIUtility.PingObject(asset);
            EditorUtility.DisplayDialog("完了",
                $"作成しました: {StorageParameterPath}\nslotCount=2 / cooldown=0\n\n" +
                "StorageInventoryController の storageDisplayParameter にアサインしてください。", "OK");
        }

        // ═══════════════════════════════════════════════════════════════
        // ユーティリティ
        // ═══════════════════════════════════════════════════════════════

        private static GameObject CreateChildImage(GameObject parent, string name, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create {name}");
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            return go;
        }

        private static GameObject CreateTextButton(GameObject parent, string name, string text, Color color, Vector2 size)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, $"Create Button {name}");
            go.transform.SetParent(parent.transform, false);
            var rect = go.AddComponent<RectTransform>();
            rect.sizeDelta = size;
            go.AddComponent<Image>().color = color;
            go.AddComponent<Button>();

            var labelGo = new GameObject("Label");
            Undo.RegisterCreatedObjectUndo(labelGo, "Create Label");
            labelGo.transform.SetParent(go.transform, false);
            var labelRect = labelGo.AddComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = 13;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = Color.white;
            tmp.raycastTarget = false;
            return go;
        }

        private static System.Type GetTypeByName(string fullName)
        {
            foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies())
            {
                var type = assembly.GetType(fullName);
                if (type != null)
                {
                    return type;
                }
            }

            return null;
        }

        private static void EnsureFolder(string folderPath)
        {
            if (AssetDatabase.IsValidFolder(folderPath))
            {
                return;
            }

            var parts = folderPath.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }
    }
}

