using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using System.Linq;

public static class PauseSystemForceFixTool
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";

    [MenuItem("Tools/6. Fix Tools/Pause System/03. [DANGER] FORCE FIX")]
    public static void ForceFix()
    {
        if (!System.IO.File.Exists(GameScenePath))
        {
            EditorUtility.DisplayDialog("Error", $"GameScene not found: {GameScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        var log = new System.Text.StringBuilder();
        log.AppendLine("=== FORCE FIX START ===\n");

        bool wasFixed = false;

        // 1. Create/Get pauseActionReference
        log.AppendLine("[1] pauseActionReference:");
        var referenceAssetPath = "Assets/Game/Player/Action/PauseActionReference.inputactionreference";
        var pauseActionReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(referenceAssetPath);

        if (pauseActionReference == null)
        {
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
                        pauseActionReference = ScriptableObject.CreateInstance<InputActionReference>();
                        pauseActionReference.Set(pauseAction);
                        AssetDatabase.CreateAsset(pauseActionReference, referenceAssetPath);
                        AssetDatabase.SaveAssets();
                        log.AppendLine("  OK: PauseActionReference created");
                        wasFixed = true;
                    }
                }
            }
        }
        else
        {
            log.AppendLine("  OK: PauseActionReference exists");
        }

        // 2. Set pauseActionReference to GameInputReader
        log.AppendLine("\n[2] GameInputReader:");
        var gameInputReaderType = System.Type.GetType("Game.Player.Script.GameInputReader");
        var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        GameObject playerObject = null;

        foreach (var go in allGameObjects)
        {
            if (go.name == "Player")
            {
                playerObject = go;
                break;
            }
        }

        if (playerObject != null)
        {
            var gameInputReader = playerObject.GetComponent(gameInputReaderType);
            if (gameInputReader != null)
            {
                var pauseActionReferenceField = gameInputReaderType?.GetField("pauseActionReference",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (pauseActionReferenceField != null && pauseActionReference != null)
                {
                    var currentValue = pauseActionReferenceField.GetValue(gameInputReader);
                    if (currentValue == null)
                    {
                        pauseActionReferenceField.SetValue(gameInputReader, pauseActionReference);
                        EditorUtility.SetDirty(playerObject);
                        log.AppendLine("  OK: pauseActionReference set");
                        wasFixed = true;
                    }
                    else
                    {
                        log.AppendLine("  OK: pauseActionReference already set");
                    }
                }
            }
            else
            {
                log.AppendLine("  ERROR: GameInputReader not found");
            }
        }
        else
        {
            log.AppendLine("  ERROR: Player object not found");
        }

        // 3. Add PauseMenuView to PauseMenuCanvas
        log.AppendLine("\n[3] PauseMenuCanvas:");
        var pauseMenuCanvas = GameObject.Find("PauseMenuCanvas");
        if (pauseMenuCanvas != null)
        {
            var pauseMenuViewType = System.Type.GetType("Game.Pause.PauseMenuView");
            var pauseMenuView = pauseMenuCanvas.GetComponent(pauseMenuViewType);

            if (pauseMenuView == null && pauseMenuViewType != null)
            {
                pauseMenuView = pauseMenuCanvas.AddComponent(pauseMenuViewType);
                log.AppendLine("  OK: PauseMenuView attached");

                var panelTransform = pauseMenuCanvas.transform.Find("PauseMenuPanel");
                if (panelTransform != null)
                {
                    var pauseMenuPanelField = pauseMenuViewType.GetField("pauseMenuPanel",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (pauseMenuPanelField != null)
                    {
                        pauseMenuPanelField.SetValue(pauseMenuView, panelTransform.gameObject);
                        log.AppendLine("  OK: pauseMenuPanel set");
                    }

                    var resumeButtonTransform = panelTransform.Find("ResumeButton");
                    if (resumeButtonTransform != null)
                    {
                        var resumeButtonField = pauseMenuViewType.GetField("resumeButton",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (resumeButtonField != null)
                        {
                            resumeButtonField.SetValue(pauseMenuView, resumeButtonTransform.GetComponent<Button>());
                            log.AppendLine("  OK: resumeButton set");
                        }
                    }

                    var returnToTitleButtonTransform = panelTransform.Find("ReturnToTitleButton");
                    if (returnToTitleButtonTransform != null)
                    {
                        var returnToTitleButtonField = pauseMenuViewType.GetField("returnToTitleButton",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (returnToTitleButtonField != null)
                        {
                            returnToTitleButtonField.SetValue(pauseMenuView, returnToTitleButtonTransform.GetComponent<Button>());
                            log.AppendLine("  OK: returnToTitleButton set");
                        }
                    }
                }

                EditorUtility.SetDirty(pauseMenuCanvas);
                wasFixed = true;
            }
            else if (pauseMenuView != null)
            {
                log.AppendLine("  OK: PauseMenuView already attached");
            }
            else
            {
                log.AppendLine("  ERROR: PauseMenuView type not found");
            }
        }
        else
        {
            log.AppendLine("  ERROR: PauseMenuCanvas not found");
        }

        // 4. Deactivate PauseMenuPanel
        log.AppendLine("\n[4] PauseMenuPanel:");
        if (pauseMenuCanvas != null)
        {
            var panelTransform = pauseMenuCanvas.transform.Find("PauseMenuPanel");
            if (panelTransform != null)
            {
                if (panelTransform.gameObject.activeSelf)
                {
                    panelTransform.gameObject.SetActive(false);
                    log.AppendLine("  OK: PauseMenuPanel deactivated");
                    wasFixed = true;
                }
                else
                {
                    log.AppendLine("  OK: PauseMenuPanel already inactive");
                }
            }
        }

        // 5. Fix PauseInputHandler
        log.AppendLine("\n[5] PauseInputHandler:");
        var pauseInputHandlerType = System.Type.GetType("Game.Pause.PauseInputHandler");
        var pauseInputHandlerGameObjects = allGameObjects.Where(go => go.name == "PauseInputHandler").ToList();

        foreach (var go in pauseInputHandlerGameObjects)
        {
            var component = go.GetComponent(pauseInputHandlerType);
            if (component == null)
            {
                Object.DestroyImmediate(go);
                log.AppendLine($"  OK: Empty PauseInputHandler removed");
                wasFixed = true;
            }
        }

        pauseInputHandlerGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None)
            .Where(go => go.name == "PauseInputHandler").ToList();

        GameObject validPauseInputHandler = null;
        foreach (var go in pauseInputHandlerGameObjects)
        {
            var component = go.GetComponent(pauseInputHandlerType);
            if (component != null)
            {
                if (validPauseInputHandler == null)
                {
                    validPauseInputHandler = go;
                }
                else
                {
                    Object.DestroyImmediate(go);
                    log.AppendLine("  OK: Duplicate PauseInputHandler removed");
                    wasFixed = true;
                }
            }
        }

        if (validPauseInputHandler == null && pauseInputHandlerType != null)
        {
            var pauseInputHandlerGo = new GameObject("PauseInputHandler");
            var pauseInputHandler = pauseInputHandlerGo.AddComponent(pauseInputHandlerType);
            log.AppendLine("  OK: PauseInputHandler created");

            if (playerObject != null)
            {
                var gameInputReader = playerObject.GetComponent(gameInputReaderType);
                if (gameInputReader != null)
                {
                    var gameInputReaderField = pauseInputHandlerType.GetField("gameInputReader",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (gameInputReaderField != null)
                    {
                        gameInputReaderField.SetValue(pauseInputHandler, gameInputReader);
                        EditorUtility.SetDirty(pauseInputHandlerGo);
                        log.AppendLine("  OK: GameInputReader reference set");
                    }
                }
            }

            wasFixed = true;
        }
        else if (validPauseInputHandler != null)
        {
            log.AppendLine("  OK: Valid PauseInputHandler exists");

            var component = validPauseInputHandler.GetComponent(pauseInputHandlerType);
            var gameInputReaderField = pauseInputHandlerType?.GetField("gameInputReader",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (gameInputReaderField != null && playerObject != null)
            {
                var currentReader = gameInputReaderField.GetValue(component);
                if (currentReader == null)
                {
                    var gameInputReader = playerObject.GetComponent(gameInputReaderType);
                    if (gameInputReader != null)
                    {
                        gameInputReaderField.SetValue(component, gameInputReader);
                        EditorUtility.SetDirty(validPauseInputHandler);
                        log.AppendLine("  OK: GameInputReader reference set");
                        wasFixed = true;
                    }
                }
            }
        }

        log.AppendLine("\n" + new string('=', 50));

        if (wasFixed)
        {
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("\nOK: Scene saved!");
            log.AppendLine("\nTEST NOW:");
            log.AppendLine("  1. Start Play mode");
            log.AppendLine("  2. Press ESC key");
            log.AppendLine("  3. Check pause menu appears");

            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("Fix Complete", log.ToString(), "OK");
        }
        else
        {
            log.AppendLine("\nOK: Everything already correct");
            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("Check Complete", "Everything is correct.", "OK");
        }
    }
}

