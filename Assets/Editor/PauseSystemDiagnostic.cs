using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

/// <summary>
/// ポーズシステムの診断・修復ツール
/// GameSceneのポーズシステムの状態をチェックし、問題を自動修復します。
/// </summary>
public static class PauseSystemDiagnostic
{
    private const string GameScenePath = "Assets/Scenes/GameScene.unity";

    [MenuItem("Tools/6. Fix Tools/Pause System/01. [Diagnostic] Diagnose and Fix")]
    public static void DiagnoseAndFix()
    {
        if (!System.IO.File.Exists(GameScenePath))
        {
            EditorUtility.DisplayDialog("Error", $"GameScene not found: {GameScenePath}", "OK");
            return;
        }

        var scene = EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);
        var log = new System.Text.StringBuilder();
        log.AppendLine("=== ポーズシステム診断レポート ===\n");

        bool hasIssues = false;

        // 1. EventSystemのチェック
        log.AppendLine("【1】EventSystem のチェック:");
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            log.AppendLine("  ❌ EventSystem が見つかりません");
            hasIssues = true;
        }
        else
        {
            log.AppendLine("  ✅ EventSystem が存在します");
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                log.AppendLine("  ❌ InputSystemUIInputModule がありません");
                hasIssues = true;
            }
            else
            {
                log.AppendLine("  ✅ InputSystemUIInputModule が存在します");
            }
        }

        // 2. PauseMenuCanvasのチェック
        log.AppendLine("\n【2】PauseMenuCanvas のチェック:");
        var pauseMenuCanvas = GameObject.Find("PauseMenuCanvas");
        if (pauseMenuCanvas == null)
        {
            log.AppendLine("  ❌ PauseMenuCanvas が見つかりません");
            hasIssues = true;
        }
        else
        {
            log.AppendLine("  ✅ PauseMenuCanvas が存在します");

            var pauseMenuViewType = System.Type.GetType("Game.Pause.PauseMenuView");
            var pauseMenuView = pauseMenuCanvas.GetComponent(pauseMenuViewType);
            if (pauseMenuView == null)
            {
                log.AppendLine("  ❌ PauseMenuView コンポーネントがアタッチされていません");
                hasIssues = true;
            }
            else
            {
                log.AppendLine("  ✅ PauseMenuView コンポーネントが存在します");
            }
        }

        // 3. PauseMenuPanelのチェック
        log.AppendLine("\n【3】PauseMenuPanel のチェック:");
        GameObject pauseMenuPanel = null;
        if (pauseMenuCanvas != null)
        {
            var panelTransform = pauseMenuCanvas.transform.Find("PauseMenuPanel");
            if (panelTransform != null)
            {
                pauseMenuPanel = panelTransform.gameObject;
                log.AppendLine("  ✅ PauseMenuPanel が存在します");

                if (pauseMenuPanel.activeSelf)
                {
                    log.AppendLine("  ❌ PauseMenuPanel がアクティブ（表示状態）です - これが最初から表示される原因です！");
                    hasIssues = true;
                }
                else
                {
                    log.AppendLine("  ✅ PauseMenuPanel は非アクティブです");
                }
            }
            else
            {
                log.AppendLine("  ❌ PauseMenuPanel が見つかりません");
                hasIssues = true;
            }
        }

        // 4. PauseInputHandlerのチェック
        log.AppendLine("\n【4】PauseInputHandler のチェック:");
        var pauseInputHandlers = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        var validPauseInputHandlers = 0;
        var emptyPauseInputHandlers = 0;

        foreach (var go in pauseInputHandlers)
        {
            if (go.name == "PauseInputHandler")
            {
                var pauseInputHandlerType = System.Type.GetType("Game.Pause.PauseInputHandler");
                var component = go.GetComponent(pauseInputHandlerType);
                if (component == null)
                {
                    emptyPauseInputHandlers++;
                }
                else
                {
                    validPauseInputHandlers++;
                }
            }
        }

        if (emptyPauseInputHandlers > 0)
        {
            log.AppendLine($"  ❌ PauseInputHandler コンポーネントがアタッチされていない GameObject が {emptyPauseInputHandlers} 個あります");
            hasIssues = true;
        }

        if (validPauseInputHandlers == 0)
        {
            log.AppendLine("  ❌ 有効な PauseInputHandler が見つかりません");
            hasIssues = true;
        }
        else if (validPauseInputHandlers > 1)
        {
            log.AppendLine($"  ⚠️ PauseInputHandler が {validPauseInputHandlers} 個あります（重複の可能性）");
        }
        else
        {
            log.AppendLine("  ✅ PauseInputHandler が正しく存在します");
        }

        // 5. GameInputReaderのチェック
        log.AppendLine("\n【5】GameInputReader のチェック:");
        var gameInputReaderType = System.Type.GetType("Game.Player.Script.GameInputReader");
        var gameInputReader = Object.FindFirstObjectByType(gameInputReaderType);
        if (gameInputReader == null)
        {
            log.AppendLine("  ❌ GameInputReader が見つかりません");
            hasIssues = true;
        }
        else
        {
            log.AppendLine("  ✅ GameInputReader が存在します");

            var pauseActionReferenceField = gameInputReaderType.GetField("pauseActionReference",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (pauseActionReferenceField != null)
            {
                var pauseActionReference = pauseActionReferenceField.GetValue(gameInputReader);
                if (pauseActionReference == null)
                {
                    log.AppendLine("  ❌ pauseActionReference が未設定です");
                    hasIssues = true;
                }
                else
                {
                    log.AppendLine("  ✅ pauseActionReference が設定されています");
                }
            }
        }

        log.AppendLine("\n" + new string('=', 50));

        if (hasIssues)
        {
            log.AppendLine("\n❌ 問題が検出されました。修復を開始します...\n");
            Debug.LogWarning(log.ToString());

            // 修復処理
            FixIssues(scene, log);
        }
        else
        {
            log.AppendLine("\n✅ すべて正常です！");
            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("診断完了", "ポーズシステムは正常に動作しています。", "OK");
        }
    }

    private static void FixIssues(UnityEngine.SceneManagement.Scene scene, System.Text.StringBuilder log)
    {
        bool wasFixed = false;

        // 1. PauseMenuPanelを非アクティブにする
        var pauseMenuCanvas = GameObject.Find("PauseMenuCanvas");
        if (pauseMenuCanvas != null)
        {
            var panelTransform = pauseMenuCanvas.transform.Find("PauseMenuPanel");
            if (panelTransform != null && panelTransform.gameObject.activeSelf)
            {
                panelTransform.gameObject.SetActive(false);
                log.AppendLine("✅ PauseMenuPanel を非アクティブに設定しました");
                wasFixed = true;
            }

            // PauseMenuViewをアタッチ
            var pauseMenuViewType = System.Type.GetType("Game.Pause.PauseMenuView");
            var pauseMenuView = pauseMenuCanvas.GetComponent(pauseMenuViewType);
            if (pauseMenuView == null && pauseMenuViewType != null)
            {
                pauseMenuView = pauseMenuCanvas.AddComponent(pauseMenuViewType);
                log.AppendLine("✅ PauseMenuView をアタッチしました");

                // フィールドを設定
                if (panelTransform != null)
                {
                    var pauseMenuPanelField = pauseMenuViewType.GetField("pauseMenuPanel",
                        System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                    if (pauseMenuPanelField != null)
                    {
                        pauseMenuPanelField.SetValue(pauseMenuView, panelTransform.gameObject);
                    }

                    // ボタンを探して設定
                    var resumeButtonTransform = panelTransform.Find("ResumeButton");
                    if (resumeButtonTransform != null)
                    {
                        var resumeButtonField = pauseMenuViewType.GetField("resumeButton",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                        if (resumeButtonField != null)
                        {
                            resumeButtonField.SetValue(pauseMenuView, resumeButtonTransform.GetComponent<Button>());
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
                        }
                    }

                    EditorUtility.SetDirty(pauseMenuCanvas);
                    log.AppendLine("✅ PauseMenuView のフィールドを設定しました");
                }

                wasFixed = true;
            }
        }

        // 2. 空のPauseInputHandlerを削除し、正しいものを確保
        var allGameObjects = Object.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
        var pauseInputHandlerType = System.Type.GetType("Game.Pause.PauseInputHandler");
        GameObject validPauseInputHandler = null;

        foreach (var go in allGameObjects)
        {
            if (go.name == "PauseInputHandler")
            {
                var component = go.GetComponent(pauseInputHandlerType);
                if (component == null)
                {
                    // 空のGameObjectを削除
                    Object.DestroyImmediate(go);
                    log.AppendLine($"✅ 空の PauseInputHandler GameObject を削除しました");
                    wasFixed = true;
                }
                else if (validPauseInputHandler == null)
                {
                    validPauseInputHandler = go;
                }
                else
                {
                    // 重複を削除
                    Object.DestroyImmediate(go);
                    log.AppendLine($"✅ 重複した PauseInputHandler を削除しました");
                    wasFixed = true;
                }
            }
        }

        // PauseInputHandlerが存在しない場合は作成
        if (validPauseInputHandler == null && pauseInputHandlerType != null)
        {
            var pauseInputHandlerGo = new GameObject("PauseInputHandler");
            var pauseInputHandler = pauseInputHandlerGo.AddComponent(pauseInputHandlerType);
            log.AppendLine("✅ PauseInputHandler を作成しました");

            // GameInputReaderを設定
            var gameInputReaderType = System.Type.GetType("Game.Player.Script.GameInputReader");
            var gameInputReader = Object.FindFirstObjectByType(gameInputReaderType);
            if (gameInputReader != null)
            {
                var gameInputReaderField = pauseInputHandlerType.GetField("gameInputReader",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
                if (gameInputReaderField != null)
                {
                    gameInputReaderField.SetValue(pauseInputHandler, gameInputReader);
                    EditorUtility.SetDirty(pauseInputHandlerGo);
                    log.AppendLine("✅ PauseInputHandler に GameInputReader を設定しました");
                }
            }

            wasFixed = true;
        }

        // 3. pauseActionReferenceを設定
        var gameInputReaderType2 = System.Type.GetType("Game.Player.Script.GameInputReader");
        var gameInputReader2 = Object.FindFirstObjectByType(gameInputReaderType2);
        if (gameInputReader2 != null)
        {
            var pauseActionReferenceField = gameInputReaderType2.GetField("pauseActionReference",
                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

            if (pauseActionReferenceField != null)
            {
                var currentValue = pauseActionReferenceField.GetValue(gameInputReader2);
                if (currentValue == null)
                {
                    // PlayerAction.inputactionsから作成
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
                                var referenceAssetPath = "Assets/Game/Player/Action/PauseActionReference.inputactionreference";
                                var existingReference = AssetDatabase.LoadAssetAtPath<InputActionReference>(referenceAssetPath);

                                if (existingReference == null)
                                {
                                    existingReference = ScriptableObject.CreateInstance<InputActionReference>();
                                    existingReference.Set(pauseAction);
                                    AssetDatabase.CreateAsset(existingReference, referenceAssetPath);
                                    AssetDatabase.SaveAssets();
                                    log.AppendLine($"✅ PauseActionReference を作成しました");
                                }

                                pauseActionReferenceField.SetValue(gameInputReader2, existingReference);
                                EditorUtility.SetDirty((Object)gameInputReader2);
                                log.AppendLine("✅ GameInputReader に pauseActionReference を設定しました");
                                wasFixed = true;
                            }
                        }
                    }
                }
            }
        }

        // 4. EventSystemとInputSystemUIInputModuleを確保
        var eventSystem = Object.FindFirstObjectByType<EventSystem>();
        if (eventSystem == null)
        {
            var eventSystemGo = new GameObject("EventSystem");
            eventSystemGo.AddComponent<EventSystem>();
            eventSystemGo.AddComponent<InputSystemUIInputModule>();
            log.AppendLine("✅ EventSystem を作成しました");
            wasFixed = true;
        }
        else
        {
            var inputModule = eventSystem.GetComponent<InputSystemUIInputModule>();
            if (inputModule == null)
            {
                eventSystem.gameObject.AddComponent<InputSystemUIInputModule>();
                log.AppendLine("✅ InputSystemUIInputModule を追加しました");
                wasFixed = true;
            }
        }

        if (wasFixed)
        {
            EditorSceneManager.SaveScene(scene);
            log.AppendLine("\n✅ 修復が完了し、シーンを保存しました！");
            log.AppendLine("\n🎮 Playモードで動作確認してください：");
            log.AppendLine("  1. 最初はポーズメニューが非表示");
            log.AppendLine("  2. ESCキーでポーズメニューが表示される");
            log.AppendLine("  3. ボタンがクリックできる");

            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("修復完了", log.ToString(), "OK");
        }
    }
}

