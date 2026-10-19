using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// Game Setup Editor Utility
/// Unity menu: Tools > Game Setup
///
/// Features:
///   1. GameOverScene auto-creation with UI
///   2. TitleScene auto-creation with UI + TitleSceneController
///   3. GameClearScene auto-creation with UI + GameClearView
///   4. Build Settings scene registration
///   5. TreasureBox prefab auto-generation
///   6. GameScene Inspector auto-setup (GameOverHandler placement, SearchController, etc.)
///   [ALL] Execute all above steps
/// </summary>
public static class GameSetupEditor
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";
    private const string GameOverScenePath = "Assets/Scenes/GameOverScene.unity";
    private const string TitleScenePath = "Assets/Scenes/TitleScene.unity";
    private const string GameClearScenePath = "Assets/Scenes/GameClearScene.unity";
    private const string TreasureBoxPrefabPath = "Assets/Game/Room/Prefabs/TreasureBox.prefab";
    // Reserved for future use:
    // private const string BackupSceneFolder = "Assets/Scenes/Backups";
    // private const string LocalizationDataPath = "Assets/Game/Localization/LocalizationData.asset";
    // private const string JapaneseFontSourcePath = @"C:\Windows\Fonts\YuGothM.ttc";
    // private const string JapaneseFontDestPath = "Assets/Game/Fonts/YuGothM.ttc";
    // private const string JapaneseFontAssetPath = "Assets/Game/Fonts/YuGothM_JP TMP_Dynamic SDF.asset";

    // ====================================================================================
    // 1. GameOverScene Creation
    // ====================================================================================

    [MenuItem("Tools/Game Setup/01. Create GameOver Scene")]
    public static void CreateGameOverScene()
    {
        if (System.IO.File.Exists(GameOverScenePath))
        {
            if (!EditorUtility.DisplayDialog(
                    "Confirm",
                    $"{GameOverScenePath} already exists. Overwrite?",
                    "Overwrite", "Cancel"))
            {
                return;
            }
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Create EventSystem
        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();

        // Add GameOverView component (if exists)
        Component gameOverView = null;
        var gameOverViewType = System.Type.GetType("Game.GameOver.GameOverView");
        if (gameOverViewType != null)
        {
            gameOverView = canvasGo.AddComponent(gameOverViewType);
        }
        else
        {
            Debug.LogWarning("[GameSetupEditor] GameOverView component not found. Some features may not work.");
        }

        // Create Background Image
        var bgGo = CreateUIChild(canvasGo.transform, "Background", typeof(Image));
        var bgImage = bgGo.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 128 / 255f);
        StretchRectTransform(bgGo.GetComponent<RectTransform>());

        // Create GameOver Text
        var gameOverTextGo = CreateUIChild(canvasGo.transform, "GameOverText", typeof(TextMeshProUGUI));
        var gameOverText = gameOverTextGo.GetComponent<TextMeshProUGUI>();
        gameOverText.text = "GAME OVER";
        gameOverText.fontSize = 60;
        gameOverText.alignment = TextAlignmentOptions.Center;
        gameOverText.color = Color.white;
        SetRectTransformAnchored(gameOverTextGo.GetComponent<RectTransform>(), 800, 200, new Vector2(0, 200));

        // Create Inventory Text
        var inventoryTextGo = CreateUIChild(canvasGo.transform, "InventoryText", typeof(TextMeshProUGUI));
        var inventoryText = inventoryTextGo.GetComponent<TextMeshProUGUI>();
        inventoryText.text = "[Final Items]\nPlayer's inventory will be displayed after implementation.";
        inventoryText.fontSize = 26;
        inventoryText.alignment = TextAlignmentOptions.TopLeft;
        inventoryText.color = Color.white;
        SetRectTransformAnchored(inventoryTextGo.GetComponent<RectTransform>(), 1200, 600, new Vector2(0, -100));

        // Create Return Button
        var buttonGo = CreateUIChild(canvasGo.transform, "ReturnToTitleButton", typeof(Image), typeof(Button));
        SetRectTransformAnchored(buttonGo.GetComponent<RectTransform>(), 400, 80, new Vector2(0, -350));

        var buttonTextGo = CreateUIChild(buttonGo.transform, "Text", typeof(TextMeshProUGUI));
        var buttonText = buttonTextGo.GetComponent<TextMeshProUGUI>();
        buttonText.text = "Return to Title";
        buttonText.fontSize = 30;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.black;
        StretchRectTransform(buttonTextGo.GetComponent<RectTransform>());

        // Assign to GameOverView
        if (gameOverView != null)
        {
            var returnToTitleButtonField = gameOverView.GetType().GetField("returnToTitleButton",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (returnToTitleButtonField != null)
            {
                returnToTitleButtonField.SetValue(gameOverView, buttonGo.GetComponent<Button>());
                Debug.Log("[GameSetupEditor] GameOverView.returnToTitleButton assigned.");
            }
            else
            {
                Debug.LogWarning("[GameSetupEditor] returnToTitleButton field not found. Please assign manually.");
            }
        }

        // Save scene
        EditorSceneManager.SaveScene(scene, GameOverScenePath);
        EditorSceneManager.OpenScene(GameOverScenePath);

        EditorUtility.DisplayDialog(
            "Complete",
            $"GameOverScene created.\n{GameOverScenePath}\n\nNote: EventSystem uses InputSystem.\nIf using InputSystem project, replace StandaloneInputModule with InputSystemUIInputModule.",
            "OK");
    }

    // ====================================================================================
    // 2. TitleScene Creation
    // ====================================================================================

    [MenuItem("Tools/Game Setup/02. Create Title Scene")]
    public static void CreateTitleScene()
    {
        if (System.IO.File.Exists(TitleScenePath))
        {
            if (!EditorUtility.DisplayDialog(
                    "Confirm",
                    $"{TitleScenePath} already exists. Overwrite?",
                    "Overwrite", "Cancel"))
            {
                return;
            }
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Create EventSystem
        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();

        // Add TitleSceneController (if exists)
        Component titleController = null;
        var titleControllerType = System.Type.GetType("Game.Title.TitleSceneController");
        if (titleControllerType != null)
        {
            titleController = canvasGo.AddComponent(titleControllerType);
        }
        else
        {
            Debug.LogWarning("[GameSetupEditor] TitleSceneController component not found. Some features may not work.");
        }

        // Create Background Image
        var bgGo = CreateUIChild(canvasGo.transform, "Background", typeof(Image));
        var bgImage = bgGo.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 180 / 255f);
        StretchRectTransform(bgGo.GetComponent<RectTransform>());

        // Create Title Text
        var titleTextGo = CreateUIChild(canvasGo.transform, "TitleText", typeof(TextMeshProUGUI));
        var titleText = titleTextGo.GetComponent<TextMeshProUGUI>();
        titleText.text = "TITLE";
        titleText.fontSize = 72;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        SetRectTransformAnchored(titleTextGo.GetComponent<RectTransform>(), 800, 200, new Vector2(0, 250));

        // Create Start Button
        var startButtonGo = CreateUIChild(canvasGo.transform, "StartButton", typeof(Image), typeof(Button));
        SetRectTransformAnchored(startButtonGo.GetComponent<RectTransform>(), 400, 100, new Vector2(0, -50));

        var startButtonTextGo = CreateUIChild(startButtonGo.transform, "Text", typeof(TextMeshProUGUI));
        var startButtonText = startButtonTextGo.GetComponent<TextMeshProUGUI>();
        startButtonText.text = "Game Start";
        startButtonText.fontSize = 34;
        startButtonText.alignment = TextAlignmentOptions.Center;
        startButtonText.color = Color.black;
        StretchRectTransform(startButtonTextGo.GetComponent<RectTransform>());

        // Create Quit Button
        var quitButtonGo = CreateUIChild(canvasGo.transform, "QuitButton", typeof(Image), typeof(Button));
        SetRectTransformAnchored(quitButtonGo.GetComponent<RectTransform>(), 400, 100, new Vector2(0, -180));

        var quitButtonTextGo = CreateUIChild(quitButtonGo.transform, "Text", typeof(TextMeshProUGUI));
        var quitButtonText = quitButtonTextGo.GetComponent<TextMeshProUGUI>();
        quitButtonText.text = "Quit Game";
        quitButtonText.fontSize = 34;
        quitButtonText.alignment = TextAlignmentOptions.Center;
        quitButtonText.color = Color.black;
        StretchRectTransform(quitButtonTextGo.GetComponent<RectTransform>());

        // Assign to TitleSceneController
        if (titleController != null)
        {
            var startButtonField = titleController.GetType().GetField("startButton",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (startButtonField != null)
            {
                startButtonField.SetValue(titleController, startButtonGo.GetComponent<Button>());
                Debug.Log("[GameSetupEditor] TitleSceneController.startButton assigned.");
            }
            else
            {
                Debug.LogWarning("[GameSetupEditor] startButton field not found. Please assign manually.");
            }

            var quitButtonField = titleController.GetType().GetField("quitButton",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (quitButtonField != null)
            {
                quitButtonField.SetValue(titleController, quitButtonGo.GetComponent<Button>());
                Debug.Log("[GameSetupEditor] TitleSceneController.quitButton assigned.");
            }
            else
            {
                Debug.LogWarning("[GameSetupEditor] quitButton field not found. Please assign manually.");
            }
        }

        // Save scene
        EditorSceneManager.SaveScene(scene, TitleScenePath);
        EditorSceneManager.OpenScene(TitleScenePath);

        EditorUtility.DisplayDialog(
            "Complete",
            $"TitleScene created.\n{TitleScenePath}\n\nStart button -> GameScene load.\nQuit button -> Exit game.\nNote: EventSystem uses InputSystem project set to InputSystemUIInputModule.",
            "OK");
    }

    // ====================================================================================
    // 3. GameClearScene Creation
    // ====================================================================================

    [MenuItem("Tools/Game Setup/03. Create GameClear Scene")]
    public static void CreateGameClearScene()
    {
        if (System.IO.File.Exists(GameClearScenePath))
        {
            if (!EditorUtility.DisplayDialog(
                    "Confirm",
                    $"{GameClearScenePath} already exists. Overwrite?",
                    "Overwrite", "Cancel"))
            {
                return;
            }
        }

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Create Canvas
        var canvasGo = new GameObject("Canvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Create EventSystem
        var eventSystemGo = new GameObject("EventSystem");
        eventSystemGo.AddComponent<EventSystem>();
        eventSystemGo.AddComponent<InputSystemUIInputModule>();

        // Add GameClearView (if exists)
        Component gameClearView = null;
        var gameClearViewType = System.Type.GetType("Game.GameClear.GameClearView");
        if (gameClearViewType != null)
        {
            gameClearView = canvasGo.AddComponent(gameClearViewType);
        }
        else
        {
            Debug.LogWarning("[GameSetupEditor] GameClearView component not found. Some features may not work.");
        }

        // Create Background Image
        var bgGo = CreateUIChild(canvasGo.transform, "Background", typeof(Image));
        var bgImage = bgGo.GetComponent<Image>();
        bgImage.color = new Color(0, 0, 0, 128 / 255f);
        StretchRectTransform(bgGo.GetComponent<RectTransform>());

        // Create Clear Text
        var clearTextGo = CreateUIChild(canvasGo.transform, "ClearText", typeof(TextMeshProUGUI));
        var clearText = clearTextGo.GetComponent<TextMeshProUGUI>();
        clearText.text = "CLEAR!";
        clearText.fontSize = 72;
        clearText.alignment = TextAlignmentOptions.Center;
        clearText.color = Color.white;
        SetRectTransformAnchored(clearTextGo.GetComponent<RectTransform>(), 800, 200, new Vector2(0, 200));

        // Create Copyright Text
        var copyrightGo = CreateUIChild(canvasGo.transform, "CopyrightText", typeof(TextMeshProUGUI));
        var copyrightText = copyrightGo.GetComponent<TextMeshProUGUI>();
        copyrightText.text = "(C)2026 Your Name";
        copyrightText.fontSize = 18;
        copyrightText.alignment = TextAlignmentOptions.BottomRight;
        copyrightText.color = Color.white;
        SetRectTransformAnchored(copyrightGo.GetComponent<RectTransform>(), 400, 50, new Vector2(400, -500));

        // Create Return Button
        var buttonGo = CreateUIChild(canvasGo.transform, "ReturnToTitleButton", typeof(Image), typeof(Button));
        SetRectTransformAnchored(buttonGo.GetComponent<RectTransform>(), 400, 80, new Vector2(0, -300));

        var buttonTextGo = CreateUIChild(buttonGo.transform, "Text", typeof(TextMeshProUGUI));
        var buttonText = buttonTextGo.GetComponent<TextMeshProUGUI>();
        buttonText.text = "Return to Title";
        buttonText.fontSize = 30;
        buttonText.alignment = TextAlignmentOptions.Center;
        buttonText.color = Color.black;
        StretchRectTransform(buttonTextGo.GetComponent<RectTransform>());

        // Assign to GameClearView
        if (gameClearView != null)
        {
            var returnToTitleButtonField = gameClearView.GetType().GetField("returnToTitleButton",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
            if (returnToTitleButtonField != null)
            {
                returnToTitleButtonField.SetValue(gameClearView, buttonGo.GetComponent<Button>());
                Debug.Log("[GameSetupEditor] GameClearView.returnToTitleButton assigned.");
            }
            else
            {
                Debug.LogWarning("[GameSetupEditor] returnToTitleButton field not found. Please assign manually.");
            }
        }

        // Save scene
        EditorSceneManager.SaveScene(scene, GameClearScenePath);
        EditorSceneManager.OpenScene(GameClearScenePath);

        EditorUtility.DisplayDialog(
            "Complete",
            $"GameClearScene created.\n{GameClearScenePath}\n\nCopyright display and Return to Title button are implemented.\nNote: Edit copyright text from Canvas/CopyrightText.",
            "OK");
    }

    // ====================================================================================
    // 4. Build Settings Update
    // ====================================================================================

    [MenuItem("Tools/1. Project Setup/04. Register All Scenes in Build Settings")]
    public static void UpdateBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>();

        // Required scenes in order
        var scenePaths = new[]
        {
            TitleScenePath,
            GameScenePath,
            GameOverScenePath,
            GameClearScenePath
        };

        foreach (var path in scenePaths)
        {
            if (System.IO.File.Exists(path))
            {
                scenes.Add(new EditorBuildSettingsScene(path, true));
            }
            else
            {
                Debug.LogWarning($"[GameSetupEditor] Scene not found: {path}");
            }
        }

        EditorBuildSettings.scenes = scenes.ToArray();

        var message = $"Build Settings updated.\n\n" +
                      $"Build Index 0: TitleScene\n" +
                      $"Build Index 1: GameScene\n" +
                      $"Build Index 2: GameOverScene\n" +
                      $"Build Index 3: GameClearScene\n" +
                      $"\n\nPlease create scenes first using Menu 1-3, then execute this again.";

        EditorUtility.DisplayDialog("Build Settings Updated", message, "OK");
    }

    // ====================================================================================
    // 5. TreasureBox Prefab Creation
    // ====================================================================================

    [MenuItem("Tools/2. Asset Creation/01. Create TreasureBox Prefab")]
    public static void CreateTreasureBoxPrefab()
    {
        if (System.IO.File.Exists(TreasureBoxPrefabPath))
        {
            if (!EditorUtility.DisplayDialog(
                    "Confirm",
                    $"{TreasureBoxPrefabPath} already exists. Overwrite?",
                    "Overwrite", "Cancel"))
            {
                return;
            }
        }

        // Ensure directory exists
        var directory = System.IO.Path.GetDirectoryName(TreasureBoxPrefabPath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        try
        {
            // Create GameObject
            var treasureBoxGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
            treasureBoxGo.name = "TreasureBox";

            // Add TreasureBoxView component (if exists)
            var treasureBoxViewType = System.Type.GetType("Game.Treasure.TreasureBoxView");
            if (treasureBoxViewType != null)
            {
                treasureBoxGo.AddComponent(treasureBoxViewType);
            }
            else
            {
                Debug.LogWarning("[GameSetupEditor] TreasureBoxView component not found. Prefab created without it.");
            }

            // Set material color (yellow)
            var renderer = treasureBoxGo.GetComponent<Renderer>();
            if (renderer != null)
            {
                var mat = new Material(Shader.Find("Standard"));
                mat.color = Color.yellow;
                renderer.sharedMaterial = mat;
            }

            // Add BoxCollider (Trigger)
            var collider = treasureBoxGo.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.isTrigger = true;
            }

            // Save as prefab
            PrefabUtility.SaveAsPrefabAsset(treasureBoxGo, TreasureBoxPrefabPath);
            Object.DestroyImmediate(treasureBoxGo);

            EditorUtility.DisplayDialog(
                "Complete",
                $"TreasureBox prefab created.\n{TreasureBoxPrefabPath}\n\n" +
                "Next, assign to RoomObjectSpawner's Treasure Box Prefab (Menu 6 auto-assigns).",
                "OK");
        }
        catch (System.Exception ex)
        {
            Debug.LogError("[GameSetupEditor] TreasureBox prefab creation failed.");
            Debug.LogException(ex);
        }
    }

    // ====================================================================================
    // 6. GameScene Inspector Auto Setup
    // ====================================================================================

    [MenuItem("Tools/Game Setup/06. GameScene Complete Setup (Add Hierarchy Objects)")]
    public static void SetupGameSceneInspector()
    {
        // Open GameScene
        if (!System.IO.File.Exists(GameScenePath))
        {
            EditorUtility.DisplayDialog("Error", $"GameScene not found: {GameScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        var log = new System.Text.StringBuilder();
        log.AppendLine("[GameSetupEditor] GameScene Inspector Setup Started");
        log.AppendLine("=====================================================");

        bool sceneModified = false;

        // 1. Check/Create GameOverHandler
        var gameOverHandlerType = System.Type.GetType("Game.GameOver.GameOverHandler");
        var gameOverHandler = gameOverHandlerType != null ? Object.FindAnyObjectByType(gameOverHandlerType) : null;
        if (gameOverHandler == null)
        {
            if (gameOverHandlerType != null)
            {
                var go = new GameObject("GameOverHandler");
                go.AddComponent(gameOverHandlerType);
                log.AppendLine("+ Created GameOverHandler object.");
                sceneModified = true;
            }
            else
            {
                log.AppendLine("! GameOverHandler component not found (skip).");
            }
        }
        else
        {
            log.AppendLine("* GameOverHandler already exists (skip).");
        }

        // 2. Check/Create GameClearHandler
        var gameClearHandlerType = System.Type.GetType("Game.GameClear.GameClearHandler");
        var gameClearHandler = gameClearHandlerType != null ? Object.FindAnyObjectByType(gameClearHandlerType) : null;
        if (gameClearHandler == null)
        {
            if (gameClearHandlerType != null)
            {
                var go = new GameObject("GameClearHandler");
                go.AddComponent(gameClearHandlerType);
                log.AppendLine("+ Created GameClearHandler object.");
                sceneModified = true;
            }
            else
            {
                log.AppendLine("! GameClearHandler component not found (skip).");
            }
        }
        else
        {
            log.AppendLine("* GameClearHandler already exists (skip).");
        }

        // 3. Check/Create SearchController
        var searchControllerType = System.Type.GetType("Game.Search.SearchController");
        var searchController = searchControllerType != null ? Object.FindAnyObjectByType(searchControllerType) : null;
        if (searchController == null)
        {
            if (searchControllerType != null)
            {
                var go = new GameObject("SearchController");
                go.AddComponent(searchControllerType);
                log.AppendLine("+ Created SearchController object.");
                sceneModified = true;
            }
            else
            {
                log.AppendLine("! SearchController component not found (skip).");
            }
        }
        else
        {
            log.AppendLine("* SearchController already exists (skip).");
        }

        // 4. Check Player BuildModeMovementBlocker
        var playerGridMoverType = System.Type.GetType("Game.Player.Script.PlayerGridMover");
        var player = playerGridMoverType != null ? Object.FindAnyObjectByType(playerGridMoverType) : null;
        if (player != null)
        {
            var playerGo = (player as Component)?.gameObject;
            var blockerType = System.Type.GetType("Game.BuildMode.BuildModeMovementBlocker");
            if (blockerType != null && playerGo != null)
            {
                if (playerGo.GetComponent(blockerType) == null)
                {
                    playerGo.AddComponent(blockerType);
                    log.AppendLine("+ Added BuildModeMovementBlocker to Player.");
                    sceneModified = true;
                }
                else
                {
                    log.AppendLine("* BuildModeMovementBlocker already exists (skip).");
                }
            }
            else
            {
                log.AppendLine("! BuildModeMovementBlocker component not found (skip).");
            }
        }
        else
        {
            log.AppendLine("! PlayerGridMover not found. Please check Player object.");
        }

        // Save scene if modified
        if (sceneModified)
        {
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("\n==> Scene saved with modifications.");
        }
        else
        {
            log.AppendLine("\n==> No modifications needed.");
        }

        log.AppendLine("=====================================================");
        Debug.Log(log.ToString());

        EditorUtility.DisplayDialog(
            "GameScene Inspector Setup Complete",
            log.ToString(),
            "OK");
    }

    // ====================================================================================
    // 7. Scene Backup Helper
    // ====================================================================================

    /// <summary>Create backup of scene file with timestamp</summary>
    private static string CreateBackup(string scenePath)
    {
        if (!System.IO.File.Exists(scenePath))
        {
            return null;
        }

        var backupFolder = "Assets/Scenes/Backups";
        if (!System.IO.Directory.Exists(backupFolder))
        {
            System.IO.Directory.CreateDirectory(backupFolder);
        }

        var fileName = System.IO.Path.GetFileNameWithoutExtension(scenePath);
        var timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmmss");
        var backupPath = $"{backupFolder}/{fileName}_backup_{timestamp}.unity";

        System.IO.File.Copy(scenePath, backupPath, true);
        AssetDatabase.Refresh();

        return backupPath;
    }

    // ====================================================================================
    // 8. InputModule Fix for All Scenes
    // ====================================================================================

    [MenuItem("Tools/Game Setup/08. Fix InputModule to InputSystem (All Scenes)")]
    public static void FixInputModuleForAllScenes()
    {
        var scenePaths = new[] { TitleScenePath, GameScenePath, GameOverScenePath, GameClearScenePath };
        var log = new System.Text.StringBuilder();
        log.AppendLine("[GameSetupEditor] InputModule Replacement Started");
        log.AppendLine("=====================================================");

        foreach (var scenePath in scenePaths)
        {
            if (!System.IO.File.Exists(scenePath))
            {
                continue;
            }

            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            var sceneName = System.IO.Path.GetFileNameWithoutExtension(scenePath);

            var eventSystem = Object.FindFirstObjectByType<EventSystem>();
            if (eventSystem == null)
            {
                log.AppendLine($"! {sceneName}: EventSystem not found.");
                continue;
            }

            // Check if already using InputSystemUIInputModule
            if (eventSystem.GetComponent<InputSystemUIInputModule>() != null)
            {
                log.AppendLine($"* {sceneName}: InputSystemUIInputModule already exists (skip).");
                continue;
            }

            // Remove StandaloneInputModule
            var standaloneModule = eventSystem.GetComponent<StandaloneInputModule>();
            if (standaloneModule == null)
            {
                log.AppendLine($"! {sceneName}: StandaloneInputModule not found.");
                continue;
            }

            Object.DestroyImmediate(standaloneModule);
            eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
            EditorSceneManager.SaveScene(scene);

            log.AppendLine($"+ {sceneName}: StandaloneInputModule → InputSystemUIInputModule replaced.");
        }

        log.AppendLine("=====================================================");
        Debug.Log(log.ToString());

        EditorUtility.DisplayDialog("InputModule Replacement Complete", log.ToString(), "OK");
    }

    // ====================================================================================
    // 9. Japanese Font Setup
    // ====================================================================================

    [MenuItem("Tools/Game Setup/09. Japanese Font Setup (Dynamic TMP FontAsset)")]
    public static void SetupJapaneseFont()
    {
        var fontSourcePath = @"C:\Windows\Fonts\YuGothM.ttc";
        var fontDestPath = "Assets/Game/Fonts/YuGothM.ttc";

        // Check Windows font exists
        if (!System.IO.File.Exists(fontSourcePath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                $"Windows YuGothic font not found: {fontSourcePath}\n\n" +
                "Please use another font or install YuGothic.",
                "OK");
            return;
        }

        // Ensure directory exists
        var fontDir = System.IO.Path.GetDirectoryName(fontDestPath);
        if (!string.IsNullOrEmpty(fontDir) && !System.IO.Directory.Exists(fontDir))
        {
            System.IO.Directory.CreateDirectory(fontDir);
        }

        // Copy font file
        if (!System.IO.File.Exists(fontDestPath))
        {
            System.IO.File.Copy(fontSourcePath, fontDestPath, true);
            AssetDatabase.Refresh();
        }

        // Note: TMP FontAsset creation requires TextMeshPro API
        // This is a placeholder - actual implementation would use TMP_FontAsset.CreateFontAsset()
        Debug.LogWarning("[GameSetupEditor] Font file copied. Please create TMP FontAsset manually:\n" +
                        "Window > TextMeshPro > Font Asset Creator");

        EditorUtility.DisplayDialog(
            "Japanese Font Setup",
            $"Font file copied to: {fontDestPath}\n\n" +
            "Next steps:\n" +
            "1. Select the font in Project view\n" +
            "2. Window > TextMeshPro > Font Asset Creator\n" +
            "3. Generate Dynamic SDF\n" +
            "4. Add to: Window > TextMeshPro > Settings > Fallback Font Asset List",
            "OK");
    }

    // ====================================================================================
    // 10. LocalizationData Creation
    // ====================================================================================

    [MenuItem("Tools/Game Setup/10. Create LocalizationData Asset")]
    public static void CreateLocalizationData()
    {
        var localizationDataPath = "Assets/Game/Localization/LocalizationData.asset";

        if (System.IO.File.Exists(localizationDataPath))
        {
            if (!EditorUtility.DisplayDialog(
                    "Confirm",
                    $"{localizationDataPath} already exists. Overwrite?",
                    "Overwrite", "Cancel"))
            {
                return;
            }
        }

        // Ensure directory exists
        var directory = System.IO.Path.GetDirectoryName(localizationDataPath);
        if (!string.IsNullOrEmpty(directory) && !System.IO.Directory.Exists(directory))
        {
            System.IO.Directory.CreateDirectory(directory);
        }

        // Create LocalizationData ScriptableObject
        var localizationDataType = System.Type.GetType("Game.Localization.LocalizationData");
        if (localizationDataType == null)
        {
            EditorUtility.DisplayDialog(
                "Error",
                "LocalizationData class not found. Please implement it first.",
                "OK");
            return;
        }

        var asset = ScriptableObject.CreateInstance(localizationDataType);

        // Note: This would require reflection to set localization entries
        // For now, just create empty asset
        AssetDatabase.CreateAsset(asset, localizationDataPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "LocalizationData Created",
            $"Created: {localizationDataPath}\n\n" +
            "Please add localization entries:\n" +
            "- GameStart: ゲームスタート / Game Start\n" +
            "- ReturnToTitle: タイトルに戻る / Return to Title\n" +
            "- etc.\n\n" +
            "Then assign to LanguageManager component's 'Localization Data' field.",
            "OK");
    }

    // ====================================================================================
    // 11. TitleScene Language Toggle
    // ====================================================================================

    [MenuItem("Tools/Game Setup/11. Add Language Toggle to TitleScene")]
    public static void AddLanguageToggleToTitle()
    {
        var localizationDataPath = "Assets/Game/Localization/LocalizationData.asset";
        if (!System.IO.File.Exists(localizationDataPath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please create LocalizationData first using Menu 10.",
                "OK");
            return;
        }

        if (!System.IO.File.Exists(TitleScenePath))
        {
            EditorUtility.DisplayDialog("Error", $"TitleScene not found: {TitleScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(TitleScenePath, OpenSceneMode.Single);
        var log = new System.Text.StringBuilder();
        log.AppendLine("[GameSetupEditor] TitleScene Language Toggle Setup Started");
        log.AppendLine("=====================================================");

        bool sceneModified = false;

        // 1. Check/Create LanguageManager
        var languageManagerType = System.Type.GetType("Game.Localization.LanguageManager");
        var existingManager = languageManagerType != null ? Object.FindAnyObjectByType(languageManagerType) : null;

        if (existingManager == null && languageManagerType != null)
        {
            var managerGo = new GameObject("LanguageManager");
            managerGo.AddComponent(languageManagerType);
            log.AppendLine("+ Created LanguageManager object.");
            sceneModified = true;
        }
        else if (existingManager != null)
        {
            log.AppendLine("* LanguageManager already exists (skip).");
        }
        else
        {
            log.AppendLine("! LanguageManager component not found.");
        }

        // 2. Find Canvas
        var canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            log.AppendLine("! Canvas not found. Please create TitleScene first.");
            EditorUtility.DisplayDialog("Error", "Canvas not found.", "OK");
            EditorSceneManager.CloseScene(scene, false);
            return;
        }

        // 3. Check/Create TitleLanguageView
        var titleLanguageViewType = System.Type.GetType("Game.Title.TitleLanguageView");
        var existingView = canvas.GetComponent(titleLanguageViewType);

        if (existingView == null && titleLanguageViewType != null)
        {
            // Create language buttons (simplified version)
            var languagePanel = CreateUIChild(canvas.transform, "LanguagePanel", typeof(RectTransform));
            SetRectTransformAnchored(languagePanel.GetComponent<RectTransform>(), 300, 80, new Vector2(0, -400));

            // Japanese button
            var jpButtonGo = CreateUIChild(languagePanel.transform, "JapaneseButton", typeof(Image), typeof(Button));
            SetRectTransformAnchored(jpButtonGo.GetComponent<RectTransform>(), 140, 60, new Vector2(-75, 0));
            var jpTextGo = CreateUIChild(jpButtonGo.transform, "Text", typeof(TextMeshProUGUI));
            var jpText = jpTextGo.GetComponent<TextMeshProUGUI>();
            jpText.text = "日本語";
            jpText.fontSize = 28;
            jpText.alignment = TextAlignmentOptions.Center;
            jpText.color = Color.black;
            StretchRectTransform(jpTextGo.GetComponent<RectTransform>());

            // English button
            var enButtonGo = CreateUIChild(languagePanel.transform, "EnglishButton", typeof(Image), typeof(Button));
            SetRectTransformAnchored(enButtonGo.GetComponent<RectTransform>(), 140, 60, new Vector2(75, 0));
            var enTextGo = CreateUIChild(enButtonGo.transform, "Text", typeof(TextMeshProUGUI));
            var enText = enTextGo.GetComponent<TextMeshProUGUI>();
            enText.text = "English";
            enText.fontSize = 28;
            enText.alignment = TextAlignmentOptions.Center;
            enText.color = Color.black;
            StretchRectTransform(enTextGo.GetComponent<RectTransform>());

            // Add TitleLanguageView component
            canvas.gameObject.AddComponent(titleLanguageViewType);

            log.AppendLine("+ Created TitleLanguageView and language toggle buttons.");
            sceneModified = true;
        }
        else if (existingView != null)
        {
            log.AppendLine("* TitleLanguageView already exists (skip).");
        }
        else
        {
            log.AppendLine("! TitleLanguageView component not found.");
        }

        // Save scene if modified
        if (sceneModified)
        {
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("\n==> Scene saved with modifications.");
        }

        log.AppendLine("=====================================================");
        Debug.Log(log.ToString());

        EditorUtility.DisplayDialog("TitleScene Language Toggle Complete", log.ToString(), "OK");
    }

    // ====================================================================================
    // 12. GameScene LanguageManager
    // ====================================================================================

    [MenuItem("Tools/Game Setup/12. Add LanguageManager to GameScene")]
    public static void AddLanguageManagerToGameScene()
    {
        var localizationDataPath = "Assets/Game/Localization/LocalizationData.asset";
        if (!System.IO.File.Exists(localizationDataPath))
        {
            EditorUtility.DisplayDialog(
                "Error",
                "Please create LocalizationData first using Menu 10.",
                "OK");
            return;
        }

        if (!System.IO.File.Exists(GameScenePath))
        {
            EditorUtility.DisplayDialog("Error", $"GameScene not found: {GameScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        var log = new System.Text.StringBuilder();
        log.AppendLine("[GameSetupEditor] GameScene LanguageManager Setup Started");
        log.AppendLine("=====================================================");

        // Check/Create LanguageManager
        var languageManagerType = System.Type.GetType("Game.Localization.LanguageManager");
        var existingManager = languageManagerType != null ? Object.FindAnyObjectByType(languageManagerType) : null;

        if (existingManager == null && languageManagerType != null)
        {
            var managerGo = new GameObject("LanguageManager");
            managerGo.AddComponent(languageManagerType);
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("+ Created LanguageManager object.");
        }
        else if (existingManager != null)
        {
            log.AppendLine("* LanguageManager already exists (skip).");
        }
        else
        {
            log.AppendLine("! LanguageManager component not found.");
        }

        log.AppendLine("=====================================================");
        log.AppendLine("\nPlease assign LocalizationData asset to LanguageManager component.");
        Debug.Log(log.ToString());

        EditorUtility.DisplayDialog("GameScene LanguageManager Complete", log.ToString(), "OK");
    }

    // ====================================================================================
    // 13. Fix GameScene Inspector References（現在のヒエラルキー状態を自動設定）
    // ====================================================================================

    [MenuItem("Tools/6. Fix Tools/02. Fix GameScene Inspector References")]
    public static void FixGameSceneInspectorReferences()
    {
        if (!EditorUtility.DisplayDialog(
                "Fix GameScene Inspector References",
                "GameSceneのInspector参照を現在のヒエラルキー状態に合わせて一括設定します。\n\n" +
                "設定内容:\n" +
                "・DemoIntrusionController: enableDemo ON, UIView/GameOverTrigger/PlayerDownStateBlocker\n" +
                "・DemoIntrusionUIView: intrusionText\n" +
                "・PlayerDamageHandler/SystemDebugController: intrusionController\n" +
                "・Player: PlayerDownStateBlocker追加\n" +
                "・PlayerInteract/RogueMover: ブロッカー設定\n" +
                "・SearchController: SearchEndCleanupHandler追加・設定\n\n" +
                "続行しますか？",
                "実行", "キャンセル"))
        {
            return;
        }

        if (!System.IO.File.Exists(GameScenePath))
        {
            EditorUtility.DisplayDialog("Error", $"GameScene not found: {GameScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        var log = new System.Text.StringBuilder();
        log.AppendLine("[GameSetupEditor] GameScene Inspector References Fix Started");
        log.AppendLine("=============================================================");

        var sceneModified = false;

        // --- 型を取得 ---
        var demoIntrusionControllerType = System.Type.GetType("Game.Intrusion.Presentation.Demo.DemoIntrusionController");
        var demoIntrusionUIViewType     = System.Type.GetType("Game.Intrusion.View.Demo.DemoIntrusionUIView");
        var gameOverHandlerType         = System.Type.GetType("Game.GameOver.GameOverHandler");
        var playerDamageHandlerType     = System.Type.GetType("Game.Player.Script.PlayerDamageHandler");
        var systemDebugControllerType   = System.Type.GetType("Game.Tests.SystemDebugController");
        var playerDownStateBlockerType  = System.Type.GetType("Game.Player.PlayerDownStateBlocker");
        var playerInteractType          = System.Type.GetType("Game.Player.Script.PlayerInteract");
        var rogueMoverType              = System.Type.GetType("Game.Player.Script.RogueMover");
        var buildModeMovementBlockerType = System.Type.GetType("Game.BuildMode.BuildModeMovementBlocker");
        var searchControllerType        = System.Type.GetType("Game.Search.SearchController");
        var searchEndCleanupHandlerType = System.Type.GetType("Game.Search.SearchEndCleanupHandler");
        var inventoryControllerType = System.Type.GetType("Game.Inventory.InventoryController");

        // --- インスタンスを検索 ---
        var demoIntrusionController   = demoIntrusionControllerType   != null ? Object.FindFirstObjectByType(demoIntrusionControllerType)   as MonoBehaviour : null;
        var demoIntrusionUIView       = demoIntrusionUIViewType       != null ? Object.FindFirstObjectByType(demoIntrusionUIViewType)       as MonoBehaviour : null;
        var gameOverHandler           = gameOverHandlerType           != null ? Object.FindFirstObjectByType(gameOverHandlerType)           as MonoBehaviour : null;
        var playerDamageHandler       = playerDamageHandlerType       != null ? Object.FindFirstObjectByType(playerDamageHandlerType)       as MonoBehaviour : null;
        var systemDebugController     = systemDebugControllerType     != null ? Object.FindFirstObjectByType(systemDebugControllerType)     as MonoBehaviour : null;
        var playerInteract            = playerInteractType            != null ? Object.FindFirstObjectByType(playerInteractType)            as MonoBehaviour : null;
        var rogueMover                = rogueMoverType                != null ? Object.FindFirstObjectByType(rogueMoverType)                as MonoBehaviour : null;
        var buildModeMovementBlocker  = buildModeMovementBlockerType  != null ? Object.FindFirstObjectByType(buildModeMovementBlockerType)  as MonoBehaviour : null;
        var searchController          = searchControllerType          != null ? Object.FindFirstObjectByType(searchControllerType)          as MonoBehaviour : null;

        // ============================================================
        // Step 1: Player に PlayerDownStateBlocker を追加
        // ============================================================
        MonoBehaviour playerDownStateBlocker = null;
        if (playerDownStateBlockerType != null)
        {
            playerDownStateBlocker = Object.FindFirstObjectByType(playerDownStateBlockerType) as MonoBehaviour;
            if (playerDownStateBlocker == null && rogueMover != null)
            {
                playerDownStateBlocker = rogueMover.gameObject.AddComponent(playerDownStateBlockerType) as MonoBehaviour;
                log.AppendLine("+ PlayerDownStateBlocker added to Player.");
                sceneModified = true;
            }
            else if (playerDownStateBlocker != null)
            {
                log.AppendLine("* PlayerDownStateBlocker already exists (skip).");
            }
            else
            {
                log.AppendLine("! PlayerDownStateBlockerType not found or RogueMover not found.");
            }
        }

        // ============================================================
        // Step 2: DemoIntrusionController の各フィールドを設定
        // ============================================================
        if (demoIntrusionController != null)
        {
            var serializedIntrusionCtrl = new SerializedObject(demoIntrusionController);

            // enableDemo = true
            var enableDemoProp = serializedIntrusionCtrl.FindProperty("enableDemo");
            if (enableDemoProp != null && !enableDemoProp.boolValue)
            {
                enableDemoProp.boolValue = true;
                log.AppendLine("+ DemoIntrusionController.enableDemo = true");
                sceneModified = true;
            }
            else
            {
                log.AppendLine("* DemoIntrusionController.enableDemo already ON (skip).");
            }

            // intrusionUIView
            SetMonoBehaviourReference(serializedIntrusionCtrl, "intrusionUIView", demoIntrusionUIView, log, ref sceneModified,
                "DemoIntrusionController.intrusionUIView");

            // gameOverTriggerObject
            SetMonoBehaviourReference(serializedIntrusionCtrl, "gameOverTriggerObject", gameOverHandler, log, ref sceneModified,
                "DemoIntrusionController.gameOverTriggerObject");

            // playerDownStateBlocker
            SetMonoBehaviourReference(serializedIntrusionCtrl, "playerDownStateBlocker", playerDownStateBlocker, log, ref sceneModified,
                "DemoIntrusionController.playerDownStateBlocker");

            serializedIntrusionCtrl.ApplyModifiedProperties();
        }
        else
        {
            log.AppendLine("! DemoIntrusionController not found in scene.");
        }

        // ============================================================
        // Step 3: DemoIntrusionUIView.intrusionText を自身の TMP にセット
        // ============================================================
        if (demoIntrusionUIView != null)
        {
            var tmpText = demoIntrusionUIView.GetComponent<TextMeshProUGUI>();
            if (tmpText != null)
            {
                var serializedUIView = new SerializedObject(demoIntrusionUIView);
                SetObjectReference(serializedUIView, "intrusionText", tmpText, log, ref sceneModified,
                    "DemoIntrusionUIView.intrusionText");
                serializedUIView.ApplyModifiedProperties();
            }
            else
            {
                log.AppendLine("! TextMeshProUGUI not found on DemoIntrusionUIView GameObject.");
            }
        }
        else
        {
            log.AppendLine("! DemoIntrusionUIView not found.");
        }

        // ============================================================
        // Step 4: PlayerDamageHandler.intrusionController
        // ============================================================
        if (playerDamageHandler != null)
        {
            var serializedPdh = new SerializedObject(playerDamageHandler);
            SetMonoBehaviourReference(serializedPdh, "intrusionController", demoIntrusionController, log, ref sceneModified,
                "PlayerDamageHandler.intrusionController");
            serializedPdh.ApplyModifiedProperties();
        }
        else
        {
            log.AppendLine("! PlayerDamageHandler not found.");
        }

        // ============================================================
        // Step 5: SystemDebugController.intrusionController
        // ============================================================
        if (systemDebugController != null)
        {
            var serializedSdc = new SerializedObject(systemDebugController);
            SetMonoBehaviourReference(serializedSdc, "intrusionController", demoIntrusionController, log, ref sceneModified,
                "SystemDebugController.intrusionController");
            serializedSdc.ApplyModifiedProperties();
        }
        else
        {
            log.AppendLine("! SystemDebugController not found.");
        }

        // ============================================================
        // Step 6: PlayerInteract.interactBlockerObjects
        // ============================================================
        if (playerInteract != null)
        {
            var serializedPI = new SerializedObject(playerInteract);
            var interactBlockersProp = serializedPI.FindProperty("interactBlockerObjects");
            if (interactBlockersProp != null && interactBlockersProp.arraySize == 0
                && buildModeMovementBlocker != null && playerDownStateBlocker != null)
            {
                interactBlockersProp.arraySize = 2;
                interactBlockersProp.GetArrayElementAtIndex(0).objectReferenceValue = buildModeMovementBlocker;
                interactBlockersProp.GetArrayElementAtIndex(1).objectReferenceValue = playerDownStateBlocker;
                serializedPI.ApplyModifiedProperties();
                log.AppendLine("+ PlayerInteract.interactBlockerObjects set [BuildModeMovementBlocker, PlayerDownStateBlocker].");
                sceneModified = true;
            }
            else if (interactBlockersProp != null && interactBlockersProp.arraySize > 0)
            {
                log.AppendLine("* PlayerInteract.interactBlockerObjects already set (skip).");
            }
            else
            {
                log.AppendLine("! BuildModeMovementBlocker or PlayerDownStateBlocker not found for PlayerInteract.");
            }
        }
        else
        {
            log.AppendLine("! PlayerInteract not found.");
        }

        // ============================================================
        // Step 7: RogueMover.movementBlockerObjects
        // ============================================================
        if (rogueMover != null)
        {
            var serializedRm = new SerializedObject(rogueMover);
            var moveBlockersProp = serializedRm.FindProperty("movementBlockerObjects");
            if (moveBlockersProp != null && moveBlockersProp.arraySize == 0
                && buildModeMovementBlocker != null && playerDownStateBlocker != null)
            {
                moveBlockersProp.arraySize = 2;
                moveBlockersProp.GetArrayElementAtIndex(0).objectReferenceValue = buildModeMovementBlocker;
                moveBlockersProp.GetArrayElementAtIndex(1).objectReferenceValue = playerDownStateBlocker;
                serializedRm.ApplyModifiedProperties();
                log.AppendLine("+ RogueMover.movementBlockerObjects set [BuildModeMovementBlocker, PlayerDownStateBlocker].");
                sceneModified = true;
            }
            else if (moveBlockersProp != null && moveBlockersProp.arraySize > 0)
            {
                log.AppendLine("* RogueMover.movementBlockerObjects already set (skip).");
            }
            else
            {
                log.AppendLine("! BuildModeMovementBlocker or PlayerDownStateBlocker not found for RogueMover.");
            }
        }
        else
        {
            log.AppendLine("! RogueMover not found.");
        }

        // ============================================================
        // Step 8: SearchController に SearchEndCleanupHandler を追加・設定
        // ============================================================
        if (searchController != null && searchEndCleanupHandlerType != null)
        {
            var searchEndCleanupHandler = searchController.GetComponent(searchEndCleanupHandlerType) as MonoBehaviour;
            if (searchEndCleanupHandler == null)
            {
                searchEndCleanupHandler = searchController.gameObject.AddComponent(searchEndCleanupHandlerType) as MonoBehaviour;
                log.AppendLine("+ SearchEndCleanupHandler added to SearchController.");
                sceneModified = true;
            }
            else
            {
                log.AppendLine("* SearchEndCleanupHandler already exists on SearchController (skip).");
            }

            var serializedSc = new SerializedObject(searchController);
            SetMonoBehaviourReference(serializedSc, "enemyCleanupHandlerObject", searchEndCleanupHandler, log, ref sceneModified,
                "SearchController.enemyCleanupHandlerObject");
            serializedSc.ApplyModifiedProperties();
        }
        else
        {
            if (searchController == null)
            {
                log.AppendLine("! SearchController not found.");
            }
            if (searchEndCleanupHandlerType == null)
            {
                log.AppendLine("! SearchEndCleanupHandler type not found.");
            }
        }

        // ============================================================
        // Step 9: InventoryController の存在確認（新インベントリシステム）
        // ============================================================
        if (inventoryControllerType != null)
        {
            var inventoryController = Object.FindFirstObjectByType(inventoryControllerType) as MonoBehaviour;
            if (inventoryController != null)
            {
                log.AppendLine($"* InventoryController が存在します: {inventoryController.gameObject.name}");
            }
            else
            {
                log.AppendLine("! InventoryController がシーンに存在しません。シーンに InventoryController を配置してください。");
            }
        }
        else
        {
            log.AppendLine("! InventoryController type not found.");
        }

        // --- 保存 ---
        if (sceneModified)
        {
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("\n==> Scene saved with modifications.");
        }
        else
        {
            log.AppendLine("\n==> No modifications needed. All references are already set.");
        }

        log.AppendLine("=============================================================");
        Debug.Log(log.ToString());

        EditorUtility.DisplayDialog("Fix Complete", log.ToString(), "OK");
    }

    // ============================================================
    // Helper: SerializedProperty に MonoBehaviour 参照をセット
    // ============================================================
    private static void SetMonoBehaviourReference(
        SerializedObject serializedObject,
        string propertyName,
        MonoBehaviour target,
        System.Text.StringBuilder log,
        ref bool sceneModified,
        string label)
    {
        if (target == null)
        {
            log.AppendLine($"! {label}: target is null (skip).");
            return;
        }

        var prop = serializedObject.FindProperty(propertyName);
        if (prop == null)
        {
            log.AppendLine($"! {label}: property not found (skip).");
            return;
        }

        if (prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = target;
            log.AppendLine($"+ {label} set.");
            sceneModified = true;
        }
        else
        {
            log.AppendLine($"* {label} already set (skip).");
        }
    }

    // ============================================================
    // Helper: SerializedProperty に Object 参照をセット
    // ============================================================
    private static void SetObjectReference(
        SerializedObject serializedObject,
        string propertyName,
        Object target,
        System.Text.StringBuilder log,
        ref bool sceneModified,
        string label)
    {
        if (target == null)
        {
            log.AppendLine($"! {label}: target is null (skip).");
            return;
        }

        var prop = serializedObject.FindProperty(propertyName);
        if (prop == null)
        {
            log.AppendLine($"! {label}: property not found (skip).");
            return;
        }

        if (prop.objectReferenceValue == null)
        {
            prop.objectReferenceValue = target;
            log.AppendLine($"+ {label} set.");
            sceneModified = true;
        }
        else
        {
            log.AppendLine($"* {label} already set (skip).");
        }
    }

    // ====================================================================================
    // Helper Methods
    // ====================================================================================

    /// <summary>Create UI child GameObject and attach to parent</summary>
    private static GameObject CreateUIChild(Transform parent, string name, params System.Type[] components)
    {
        var go = new GameObject(name, components);
        go.transform.SetParent(parent, false);

        // Add RectTransform if not present
        if (go.GetComponent<RectTransform>() == null)
        {
            go.AddComponent<RectTransform>();
        }

        return go;
    }

    /// <summary>Set RectTransform anchor to center with fixed size</summary>
    private static void SetRectTransformAnchored(RectTransform rect, float width, float height, Vector2 anchoredPosition)
    {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(width, height);
        rect.anchoredPosition = anchoredPosition;
    }

    /// <summary>Stretch RectTransform to fill parent (for button inner text)</summary>
    private static void StretchRectTransform(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    // ====================================================================================
    // [ALL] Execute All Setup Steps
    // ====================================================================================

    [MenuItem("Tools/Game Setup/[ALL] Execute All Steps")]
    public static void RunAll()
    {
        if (!EditorUtility.DisplayDialog(
                "Execute All Steps",
                "This will execute all setup steps.\nExisting files will be skipped.\n\nContinue?",
                "Execute", "Cancel"))
        {
            return;
        }

        // 1. GameOverScene
        if (!System.IO.File.Exists(GameOverScenePath))
        {
            CreateGameOverScene();
        }
        else
        {
            Debug.Log("[GameSetupEditor] GameOverScene already exists (skip).");
        }

        // 2. TitleScene
        if (!System.IO.File.Exists(TitleScenePath))
        {
            CreateTitleScene();
        }
        else
        {
            Debug.Log("[GameSetupEditor] TitleScene already exists (skip).");
        }

        // 3. GameClearScene
        if (!System.IO.File.Exists(GameClearScenePath))
        {
            CreateGameClearScene();
        }
        else
        {
            Debug.Log("[GameSetupEditor] GameClearScene already exists (skip).");
        }

        // 4. Build Settings
        UpdateBuildSettings();

        // 5. TreasureBox Prefab
        if (!System.IO.File.Exists(TreasureBoxPrefabPath))
        {
            CreateTreasureBoxPrefab();
        }
        else
        {
            Debug.Log("[GameSetupEditor] TreasureBox prefab already exists (skip).");
        }

        EditorUtility.DisplayDialog(
            "Complete",
            "All setup steps executed.\n\nPlease check console for details.",
            "OK");
    }

    // ====================================================================================
    // 7. Pause Menu Setup for GameScene
    // ====================================================================================

    [MenuItem("Tools/Game Setup/07. Add Pause Menu to GameScene")]
    public static void AddPauseMenuToGameScene()
    {
        if (!System.IO.File.Exists(GameScenePath))
        {
            EditorUtility.DisplayDialog("Error", $"GameScene not found: {GameScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        // Check if PauseMenuCanvas already exists
        var existingCanvas = GameObject.Find("PauseMenuCanvas");
        if (existingCanvas != null)
        {
            if (!EditorUtility.DisplayDialog("Confirm",
                "PauseMenuCanvas already exists. Delete and recreate?",
                "Yes", "Cancel"))
            {
                return;
            }
            Object.DestroyImmediate(existingCanvas);
        }

        // Create Pause Menu Canvas
        var canvasGo = new GameObject("PauseMenuCanvas");
        var canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100; // 最前面に表示
        var canvasScaler = canvasGo.AddComponent<CanvasScaler>();
        canvasScaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasScaler.referenceResolution = new Vector2(1920, 1080);
        canvasScaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        // Add PauseMenuView component
        Component pauseMenuView = null;
        var pauseMenuViewType = System.Type.GetType("Game.Pause.PauseMenuView");
        if (pauseMenuViewType != null)
        {
            pauseMenuView = canvasGo.AddComponent(pauseMenuViewType);
        }
        else
        {
            Debug.LogWarning("[GameSetupEditor] PauseMenuView component not found.");
        }

        // Create Pause Menu Panel
        var panelGo = CreateUIChild(canvasGo.transform, "PauseMenuPanel", typeof(Image));
        var panelImage = panelGo.GetComponent<Image>();
        panelImage.color = new Color(0, 0, 0, 200 / 255f);
        StretchRectTransform(panelGo.GetComponent<RectTransform>());

        // 初期状態で非表示にする（重要！）
        panelGo.SetActive(false);
        Debug.Log("[GameSetupEditor] PauseMenuPanel を初期状態で非表示に設定しました");

        // Create Title Text
        var titleTextGo = CreateUIChild(panelGo.transform, "TitleText", typeof(TextMeshProUGUI));
        var titleText = titleTextGo.GetComponent<TextMeshProUGUI>();
        titleText.text = "PAUSED";
        titleText.fontSize = 72;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = Color.white;
        SetRectTransformAnchored(titleTextGo.GetComponent<RectTransform>(), 800, 150, new Vector2(0, 200));

        // Create Resume Button
        var resumeButtonGo = CreateUIChild(panelGo.transform, "ResumeButton", typeof(Image), typeof(Button));
        SetRectTransformAnchored(resumeButtonGo.GetComponent<RectTransform>(), 400, 100, new Vector2(0, 0));
        var resumeButton = resumeButtonGo.GetComponent<Button>();
        resumeButton.interactable = true; // 明示的に有効化
        resumeButton.transition = Selectable.Transition.ColorTint; // カラーティント設定
        var resumeColors = resumeButton.colors;
        resumeColors.normalColor = Color.white;
        resumeColors.highlightedColor = new Color(0.8f, 0.8f, 0.8f);
        resumeColors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        resumeButton.colors = resumeColors;

        var resumeButtonTextGo = CreateUIChild(resumeButtonGo.transform, "Text", typeof(TextMeshProUGUI));
        var resumeButtonText = resumeButtonTextGo.GetComponent<TextMeshProUGUI>();
        resumeButtonText.text = "Resume";
        resumeButtonText.fontSize = 36;
        resumeButtonText.alignment = TextAlignmentOptions.Center;
        resumeButtonText.color = Color.black;
        StretchRectTransform(resumeButtonTextGo.GetComponent<RectTransform>());

        // Create Return to Title Button
        var titleButtonGo = CreateUIChild(panelGo.transform, "ReturnToTitleButton", typeof(Image), typeof(Button));
        SetRectTransformAnchored(titleButtonGo.GetComponent<RectTransform>(), 400, 100, new Vector2(0, -130));
        var titleButton = titleButtonGo.GetComponent<Button>();
        titleButton.interactable = true; // 明示的に有効化
        titleButton.transition = Selectable.Transition.ColorTint; // カラーティント設定
        var titleColors = titleButton.colors;
        titleColors.normalColor = Color.white;
        titleColors.highlightedColor = new Color(0.8f, 0.8f, 0.8f);
        titleColors.pressedColor = new Color(0.6f, 0.6f, 0.6f);
        titleButton.colors = titleColors;

        var titleButtonTextGo = CreateUIChild(titleButtonGo.transform, "Text", typeof(TextMeshProUGUI));
        var titleButtonText = titleButtonTextGo.GetComponent<TextMeshProUGUI>();
        titleButtonText.text = "Return to Title";
        titleButtonText.fontSize = 36;
        titleButtonText.alignment = TextAlignmentOptions.Center;
        titleButtonText.color = Color.black;
        StretchRectTransform(titleButtonTextGo.GetComponent<RectTransform>());

        // Assign to PauseMenuView
        if (pauseMenuView != null)
        {
            var pauseMenuPanelField = pauseMenuView.GetType().GetField("pauseMenuPanel",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (pauseMenuPanelField != null)
            {
                pauseMenuPanelField.SetValue(pauseMenuView, panelGo);
            }

            var resumeButtonField = pauseMenuView.GetType().GetField("resumeButton",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (resumeButtonField != null)
            {
                resumeButtonField.SetValue(pauseMenuView, resumeButtonGo.GetComponent<Button>());
            }

            var returnToTitleButtonField = pauseMenuView.GetType().GetField("returnToTitleButton",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (returnToTitleButtonField != null)
            {
                returnToTitleButtonField.SetValue(pauseMenuView, titleButtonGo.GetComponent<Button>());
            }
        }

        // Add PauseInputHandler
        var pauseInputHandlerGo = new GameObject("PauseInputHandler");
        var pauseInputHandlerType = System.Type.GetType("Game.Pause.PauseInputHandler");
        if (pauseInputHandlerType != null)
        {
            var pauseInputHandler = pauseInputHandlerGo.AddComponent(pauseInputHandlerType);

            // GameInputReaderを見つけて設定
            var gameInputReaderType = System.Type.GetType("Game.Player.Script.GameInputReader");
            var gameInputReader = Object.FindFirstObjectByType(gameInputReaderType);

            if (gameInputReader != null)
            {
                // PauseInputHandlerにGameInputReaderを設定
                var gameInputReaderField = pauseInputHandlerType.GetField("gameInputReader",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (gameInputReaderField != null)
                {
                    gameInputReaderField.SetValue(pauseInputHandler, gameInputReader);
                    Debug.Log("[GameSetupEditor] PauseInputHandler に GameInputReader を設定しました。");
                }

                // GameInputReaderにpauseActionReferenceを設定
                var pauseActionReferenceField = gameInputReaderType.GetField("pauseActionReference",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (pauseActionReferenceField != null)
                {
                    // PlayerAction.inputactionsのPauseアクションを探す
                    var playerActionAssetPath = "Assets/Game/Player/Action/PlayerAction.inputactions";
                    var playerActionAsset = AssetDatabase.LoadAssetAtPath<InputActionAsset>(playerActionAssetPath);

                    if (playerActionAsset != null)
                    {
                        var playerMap = playerActionAsset.FindActionMap("Player");
                        if (playerMap != null)
                        {
                            var pauseAction = playerMap.FindAction("Pause");
                            if (pauseAction != null)
                            {
                                // InputActionReference を作成
                                var referenceAssetPath = "Assets/Game/Player/Action/PauseActionReference.inputactionreference";
                                var existingReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(referenceAssetPath);

                                if (existingReference == null)
                                {
                                    // 新しいInputActionReferenceを作成
                                    existingReference = ScriptableObject.CreateInstance<InputActionReference>();
                                    existingReference.Set(pauseAction);
                                    AssetDatabase.CreateAsset(existingReference, referenceAssetPath);
                                    AssetDatabase.SaveAssets();
                                    Debug.Log($"[GameSetupEditor] PauseActionReference を作成しました: {referenceAssetPath}");
                                }

                                pauseActionReferenceField.SetValue(gameInputReader, existingReference);
                                UnityEditor.EditorUtility.SetDirty((Object)gameInputReader);
                                Debug.Log("[GameSetupEditor] GameInputReader に pauseActionReference を設定しました。");
                            }
                            else
                            {
                                Debug.LogWarning("[GameSetupEditor] PlayerAction の Pause アクションが見つかりません。");
                            }
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"[GameSetupEditor] PlayerAction.inputactions が見つかりません: {playerActionAssetPath}");
                    }
                }
            }
            else
            {
                Debug.LogWarning("[GameSetupEditor] GameInputReader が見つかりません。手動で設定してください。");
            }

            Debug.Log("[GameSetupEditor] PauseInputHandler added to GameScene.");
        }

        // Check if EventSystem exists, if not create one
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
            Debug.Log("[GameSetupEditor] EventSystem with InputSystemUIInputModule created.");
        }
        else
        {
            // EventSystemが存在する場合、InputSystemUIInputModuleが存在するか確認
            if (eventSystem.GetComponent<InputSystemUIInputModule>() == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                Debug.Log("[GameSetupEditor] InputSystemUIInputModule added to existing EventSystem.");
            }
            else
            {
                Debug.Log("[GameSetupEditor] EventSystem with InputSystemUIInputModule already exists.");
            }
        }

        // Save scene
        EditorSceneManager.SaveScene(scene);

        EditorUtility.DisplayDialog(
            "Complete",
            "Pause Menu added to GameScene.\n\nNote: You need to assign pauseActionReference in GameInputReader Inspector.\nDefault pause key recommendation: ESC key.",
            "OK");
    }
    // ====================================================================================
    // Inventory Setup
    // ====================================================================================

    private const string InventoryDisplayParameterPath = "Assets/Game/Inventory/Parameter/InventoryDisplayParameter.asset";
    private const string SwordIconPath  = "Assets/Resources/AileOBJ/weapon-removebg-preview 1.png";
    private const string CpuIconPath    = "Assets/Resources/AileOBJ/cpu-removebg-preview 1.png";
    private const string WeaponDataPath = "Assets/Game/Weapon/Data/ElectricitySword2.asset";

    /// <summary>
    /// InventoryDisplayParameter ScriptableObject を生成してソード・CPUのアイコンを設定する。
    /// </summary>
    [MenuItem("Tools/2. Asset Creation/01. Create InventoryDisplayParameter (Sword + CPU Icons)")]
    public static void CreateInventoryDisplayParameter()
    {
        // 既存チェック
        var existing = AssetDatabase.LoadAssetAtPath<ScriptableObject>(InventoryDisplayParameterPath);
        if (existing != null)
        {
            if (!EditorUtility.DisplayDialog(
                    "確認",
                    $"{InventoryDisplayParameterPath} は既に存在します。上書きして再設定しますか？",
                    "上書き", "キャンセル"))
            {
                return;
            }
        }

        // アイコンをロード（テクスチャをSprite設定でロード）
        var swordSprite = LoadSprite(SwordIconPath);
        var cpuSprite   = LoadSprite(CpuIconPath);

        if (swordSprite == null)
        {
            Debug.LogWarning($"[GameSetupEditor] ソードアイコンが見つかりません: {SwordIconPath}");
        }

        if (cpuSprite == null)
        {
            Debug.LogWarning($"[GameSetupEditor] CPUアイコンが見つかりません: {CpuIconPath}");
        }

        // ScriptableObject 生成
        var parameterType = System.Type.GetType("Game.Inventory.InventoryDisplayParameter, Assembly-CSharp");
        if (parameterType == null)
        {
            EditorUtility.DisplayDialog("エラー", "InventoryDisplayParameter クラスが見つかりません。コンパイルが完了しているか確認してください。", "OK");
            return;
        }

        var asset = existing != null
            ? existing
            : (ScriptableObject)ScriptableObject.CreateInstance(parameterType);

        var serialized = new SerializedObject(asset);

        // SlotCount = 4, OpenCloseCooldown = 0.2
        SetUIntProperty(serialized, "slotCount", 4);
        SetFloatProperty(serialized, "openCloseCooldown", 0.2f);

        // iconEntries: ElectricitySword2 → Sword
        var iconEntriesProp = serialized.FindProperty("iconEntries");
        if (iconEntriesProp != null)
        {
            iconEntriesProp.arraySize = 1;

            // エントリ0: ソード (WeaponData の ScriptableObject 名 = "ElectricitySword2")
            var entry0 = iconEntriesProp.GetArrayElementAtIndex(0);
            entry0.FindPropertyRelative("itemId").stringValue = "ElectricitySword2";
            var icon0Prop = entry0.FindPropertyRelative("icon");
            if (swordSprite != null)
            {
                icon0Prop.objectReferenceValue = swordSprite;
            }
        }

        serialized.ApplyModifiedProperties();

        if (existing == null)
        {
            System.IO.Directory.CreateDirectory("Assets/Game/Inventory/Parameter");
            AssetDatabase.CreateAsset(asset, InventoryDisplayParameterPath);
        }
        else
        {
            EditorUtility.SetDirty(asset);
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[GameSetupEditor] InventoryDisplayParameter を作成/更新しました: {InventoryDisplayParameterPath}");
        EditorUtility.DisplayDialog(
            "完了",
            "InventoryDisplayParameter を作成/更新しました。\n\n" +
            "設定内容:\n" +
            "  • SlotCount = 4\n" +
            "  • ElectricitySword2 → ソードアイコン\n\n" +
            "次のステップ:\n" +
            "「Tools/6. Fix Tools/03. Setup InventoryController in GameScene」を実行してください。",
            "OK");
    }

    /// <summary>
    /// GameScene に InventoryController を GameObjectとして追加し、
    /// InventoryDisplayParameter・GameInputReader・PlayerEquipment を自動アサインする。
    /// </summary>
    [MenuItem("Tools/6. Fix Tools/03. Setup InventoryController in GameScene")]
    public static void SetupInventoryControllerInGameScene()
    {
        if (!EditorUtility.DisplayDialog(
                "確認",
                "GameScene に InventoryController を追加します。\n\n" +
                "設定内容:\n" +
                "  • InventoryController GameObject を追加\n" +
                "  • InventoryDisplayParameter を自動アサイン\n" +
                "  • GameInputReader / PlayerEquipment を自動アサイン\n\n" +
                "続行しますか？",
                "実行", "キャンセル"))
        {
            return;
        }

        if (!System.IO.File.Exists(GameScenePath))
        {
            EditorUtility.DisplayDialog("エラー", $"GameScene が見つかりません: {GameScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        var log = new System.Text.StringBuilder();
        log.AppendLine("[GameSetupEditor] InventoryController Setup Started");
        log.AppendLine("=================================================");

        var sceneModified = false;

        // InventoryDisplayParameter をロード
        var displayParameter = AssetDatabase.LoadAssetAtPath<ScriptableObject>(InventoryDisplayParameterPath);
        if (displayParameter == null)
        {
            log.AppendLine("! InventoryDisplayParameter が見つかりません。先に「Create InventoryDisplayParameter」を実行してください。");
            Debug.LogWarning(log.ToString());
            EditorUtility.DisplayDialog("エラー", "InventoryDisplayParameter が見つかりません。\n先に「Tools/2. Asset Creation/01. Create InventoryDisplayParameter」を実行してください。", "OK");
            return;
        }

        // InventoryController タイプ取得
        var inventoryControllerType = System.Type.GetType("Game.Inventory.InventoryController, Assembly-CSharp");
        if (inventoryControllerType == null)
        {
            EditorUtility.DisplayDialog("エラー", "InventoryController クラスが見つかりません。コンパイルが完了しているか確認してください。", "OK");
            return;
        }

        // 既存の InventoryController を検索
        var existingInventoryController = Object.FindFirstObjectByType(inventoryControllerType) as MonoBehaviour;

        MonoBehaviour inventoryController;
        if (existingInventoryController != null)
        {
            log.AppendLine($"* InventoryController は既に存在します: {existingInventoryController.gameObject.name}");
            inventoryController = existingInventoryController;
        }
        else
        {
            // 新しい InventoryController GameObject を作成
            var go = new GameObject("InventoryController");
            inventoryController = go.AddComponent(inventoryControllerType) as MonoBehaviour;
            log.AppendLine("+ InventoryController GameObject を作成しました");
            sceneModified = true;
        }

        // フィールドを設定
        var serializedIc = new SerializedObject(inventoryController);

        // displayParameter を設定
        var displayParameterProp = serializedIc.FindProperty("displayParameter");
        if (displayParameterProp != null && displayParameterProp.objectReferenceValue == null)
        {
            displayParameterProp.objectReferenceValue = displayParameter;
            log.AppendLine("+ displayParameter をアサインしました");
            sceneModified = true;
        }
        else if (displayParameterProp != null)
        {
            log.AppendLine("* displayParameter は既にアサイン済みです");
        }

        // GameInputReader を検索してアサイン
        var gameInputReaderType = System.Type.GetType("Game.Player.Script.GameInputReader, Assembly-CSharp");
        if (gameInputReaderType != null)
        {
            var gameInputReader = Object.FindFirstObjectByType(gameInputReaderType) as MonoBehaviour;
            var gameInputReaderProp = serializedIc.FindProperty("gameInputReader");
            if (gameInputReaderProp != null && gameInputReader != null && gameInputReaderProp.objectReferenceValue == null)
            {
                gameInputReaderProp.objectReferenceValue = gameInputReader;
                log.AppendLine($"+ gameInputReader をアサインしました: {gameInputReader.gameObject.name}");
                sceneModified = true;
            }
        }

        // PlayerEquipment を検索してアサイン
        var playerEquipmentType = System.Type.GetType("Game.Weapon.PlayerEquipment, Assembly-CSharp");
        if (playerEquipmentType != null)
        {
            var playerEquipment = Object.FindFirstObjectByType(playerEquipmentType) as MonoBehaviour;
            var playerEquipmentProp = serializedIc.FindProperty("playerEquipment");
            if (playerEquipmentProp != null && playerEquipment != null && playerEquipmentProp.objectReferenceValue == null)
            {
                playerEquipmentProp.objectReferenceValue = playerEquipment;
                log.AppendLine($"+ playerEquipment をアサインしました: {playerEquipment.gameObject.name}");
                sceneModified = true;
            }
        }

        serializedIc.ApplyModifiedProperties();

        if (sceneModified)
        {
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("\n==> シーンを保存しました");
        }

        Debug.Log(log.ToString());

        EditorUtility.DisplayDialog(
            "完了",
            "InventoryController のセットアップが完了しました。\n\n" +
            "⚠️ 手動設定が必要な項目:\n" +
            "  • InventoryView（インベントリUI）の作成・アサイン\n" +
            "  • シーン上の DemoPlayerInventoryController コンポーネントの削除\n\n" +
            "詳細はConsoleログを確認してください。",
            "OK");
    }

    // ====================================================================================
    // Enemy Model Update Tool
    // ====================================================================================

    private const string EnemyViewDataFolder = "Assets/Game/EnemyData";

    /// <summary>
    /// EnemyVirusViewData の enemyVirusPrefab をインタラクティブに更新する。
    /// </summary>
    [MenuItem("Tools/5. Dungeon Tools/01. Update Enemy Model Prefab")]
    public static void UpdateEnemyModelPrefab()
    {
        // EnemyVirusViewData アセットを全て検索
        var guids = AssetDatabase.FindAssets("t:ScriptableObject", new[] { EnemyViewDataFolder });
        var viewDataAssets = new List<(string path, ScriptableObject asset)>();

        foreach (var guid in guids)
        {
            var assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(assetPath);
            if (asset == null)
            {
                continue;
            }

            // EnemyVirusViewData かどうかをフィールドで判定
            var serialized = new SerializedObject(asset);
            if (serialized.FindProperty("enemyVirusPrefab") != null)
            {
                viewDataAssets.Add((assetPath, asset));
            }
        }

        if (viewDataAssets.Count == 0)
        {
            EditorUtility.DisplayDialog("エラー", $"{EnemyViewDataFolder} 配下に EnemyVirusViewData アセットが見つかりませんでした。", "OK");
            return;
        }

        // 対象アセット名の一覧を表示して更新するプレハブを選択させる
        var assetNames = new System.Text.StringBuilder();
        foreach (var (path, _) in viewDataAssets)
        {
            assetNames.AppendLine($"  • {System.IO.Path.GetFileNameWithoutExtension(path)}");
        }

        if (!EditorUtility.DisplayDialog(
                "エネミーモデル更新",
                $"以下の EnemyVirusViewData の enemyVirusPrefab を更新します:\n\n{assetNames}\n" +
                "次のダイアログで新しいプレハブを選択してください。",
                "続行", "キャンセル"))
        {
            return;
        }

        // プレハブ選択ダイアログ
        var newPrefabPath = EditorUtility.OpenFilePanelWithFilters(
            "新しいエネミープレハブを選択",
            "Assets/",
            new[] { "Prefab Files", "prefab", "All Files", "*" });

        if (string.IsNullOrEmpty(newPrefabPath))
        {
            return;
        }

        // 絶対パス → アセットパスに変換
        var projectPath = System.IO.Path.GetFullPath(Application.dataPath + "/..");
        if (newPrefabPath.StartsWith(projectPath))
        {
            newPrefabPath = newPrefabPath.Substring(projectPath.Length + 1).Replace('\\', '/');
        }

        var newPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(newPrefabPath);
        if (newPrefab == null)
        {
            EditorUtility.DisplayDialog("エラー", $"プレハブをロードできませんでした:\n{newPrefabPath}", "OK");
            return;
        }

        // 全 EnemyVirusViewData の enemyVirusPrefab を更新
        var updatedCount = 0;
        foreach (var (path, asset) in viewDataAssets)
        {
            var serialized = new SerializedObject(asset);
            var prefabProp = serialized.FindProperty("enemyVirusPrefab");
            if (prefabProp == null)
            {
                continue;
            }

            prefabProp.objectReferenceValue = newPrefab;
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
            updatedCount++;
            Debug.Log($"[GameSetupEditor] {path} の enemyVirusPrefab を更新しました → {newPrefabPath}");
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        EditorUtility.DisplayDialog(
            "完了",
            $"{updatedCount} 件の EnemyVirusViewData を更新しました。\n\n" +
            $"新しいプレハブ: {System.IO.Path.GetFileName(newPrefabPath)}",
            "OK");
    }

    // ====================================================================================
    // Helper Utilities
    // ====================================================================================

    /// <summary>
    /// 指定パスの PNG/画像を Sprite としてロードする。
    /// テクスチャの TextureType が Sprite でない場合は警告を出す。
    /// </summary>
    private static Sprite LoadSprite(string assetPath)
    {
        // まず Sprite として直接ロードを試みる
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (sprite != null)
        {
            return sprite;
        }

        // TextureType が Sprite でない可能性があるため、TextureImporter で確認
        var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
        if (importer == null)
        {
            return null;
        }

        if (importer.textureType != TextureImporterType.Sprite)
        {
            Debug.LogWarning($"[GameSetupEditor] {assetPath} の TextureType が Sprite ではありません。Sprite に変更します。");
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
    }

    private static void SetUIntProperty(SerializedObject serializedObject, string propertyName, uint value)
    {
        var prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            prop.longValue = value;
        }
    }

    private static void SetFloatProperty(SerializedObject serializedObject, string propertyName, float value)
    {
        var prop = serializedObject.FindProperty(propertyName);
        if (prop != null)
        {
            prop.floatValue = value;
        }
    }
}







