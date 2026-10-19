using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace Editor
{
    /// <summary>
    /// PauseMenuシステムの問題を完全に修正するツール。
    /// - PauseMenuCanvasの名前参照を修正
    /// - PauseMenuViewがPauseMenuCanvasに正しくアタッチされているか確認
    /// - pauseMenuPanelフィールドが正しく設定されているか確認
    /// - PauseInputHandlerが存在し、正しく設定されているか確認
    /// - PauseManagerが存在するか確認
    /// </summary>
    public static class PauseMenuCompleteFix
    {
        [MenuItem("Tools/6. Fix Tools/Pause System/02. [DANGER] Complete Fix")]
        public static void CompleteFix()
        {
            var log = new System.Text.StringBuilder();
            log.AppendLine("=== PauseMenu 完全修正ツール ===\n");

            // 現在のシーンを取得
            var scene = EditorSceneManager.GetActiveScene();
            log.AppendLine($"【シーン】{scene.name}\n");

            bool hasIssues = false;
            bool wasFixed = false;

            // 1. PauseMenuCanvasの確認
            log.AppendLine("【1】PauseMenuCanvas の確認:");
            var pauseMenuCanvas = GameObject.Find("PauseMenuCanvas");
            if (pauseMenuCanvas == null)
            {
                log.AppendLine("  ❌ PauseMenuCanvas が見つかりません");
                log.AppendLine("  → Tools/Game Setup/07. Add Pause Menu to GameScene を先に実行してください");
                hasIssues = true;
            }
            else
            {
                log.AppendLine("  ✅ PauseMenuCanvas が存在します");

                // 2. PauseMenuViewの確認と修正
                log.AppendLine("\n【2】PauseMenuView の確認:");
                var pauseMenuViewType = System.Type.GetType("Game.Pause.PauseMenuView");
                var pauseMenuView = pauseMenuCanvas.GetComponent(pauseMenuViewType);

                if (pauseMenuView == null)
                {
                    log.AppendLine("  ❌ PauseMenuView がアタッチされていません");

                    if (pauseMenuViewType != null)
                    {
                        pauseMenuView = pauseMenuCanvas.AddComponent(pauseMenuViewType);
                        log.AppendLine("  ✅ PauseMenuView をアタッチしました");
                        wasFixed = true;
                    }
                }
                else
                {
                    log.AppendLine("  ✅ PauseMenuView が存在します");
                }

                // 3. PauseMenuPanelの確認と設定
                log.AppendLine("\n【3】PauseMenuPanel の確認:");
                var panelTransform = pauseMenuCanvas.transform.Find("PauseMenuPanel");

                if (panelTransform == null)
                {
                    log.AppendLine("  ❌ PauseMenuPanel が見つかりません");
                    hasIssues = true;
                }
                else
                {
                    log.AppendLine("  ✅ PauseMenuPanel が存在します");

                    // PauseMenuViewのフィールドを確認・設定
                    if (pauseMenuView != null)
                    {
                        var pauseMenuPanelField = pauseMenuViewType.GetField("pauseMenuPanel",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                        if (pauseMenuPanelField != null)
                        {
                            var currentValue = pauseMenuPanelField.GetValue(pauseMenuView) as GameObject;

                            if (currentValue == null || !ReferenceEquals(currentValue, panelTransform.gameObject))
                            {
                                pauseMenuPanelField.SetValue(pauseMenuView, panelTransform.gameObject);
                                log.AppendLine("  ✅ pauseMenuPanel フィールドを設定しました");
                                wasFixed = true;
                            }
                            else
                            {
                                log.AppendLine("  ✅ pauseMenuPanel フィールドは正しく設定されています");
                            }
                        }

                        // resumeButtonの確認
                        var resumeButtonTransform = panelTransform.Find("ResumeButton");
                        if (resumeButtonTransform != null)
                        {
                            var resumeButtonField = pauseMenuViewType.GetField("resumeButton",
                                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                            if (resumeButtonField != null)
                            {
                                var currentButton = resumeButtonField.GetValue(pauseMenuView) as Button;
                                var correctButton = resumeButtonTransform.GetComponent<Button>();

                                if (currentButton == null || !ReferenceEquals(currentButton, correctButton))
                                {
                                    resumeButtonField.SetValue(pauseMenuView, correctButton);
                                    log.AppendLine("  ✅ resumeButton フィールドを設定しました");
                                    wasFixed = true;
                                }
                            }
                        }

                        // returnToTitleButtonの確認
                        var returnToTitleButtonTransform = panelTransform.Find("ReturnToTitleButton");
                        if (returnToTitleButtonTransform != null)
                        {
                            var returnToTitleButtonField = pauseMenuViewType.GetField("returnToTitleButton",
                                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                            if (returnToTitleButtonField != null)
                            {
                                var currentButton = returnToTitleButtonField.GetValue(pauseMenuView) as Button;
                                var correctButton = returnToTitleButtonTransform.GetComponent<Button>();

                                if (currentButton == null || !ReferenceEquals(currentButton, correctButton))
                                {
                                    returnToTitleButtonField.SetValue(pauseMenuView, correctButton);
                                    log.AppendLine("  ✅ returnToTitleButton フィールドを設定しました");
                                    wasFixed = true;
                                }
                            }
                        }
                    }

                    // PauseMenuPanelが非アクティブか確認
                    if (panelTransform.gameObject.activeSelf)
                    {
                        panelTransform.gameObject.SetActive(false);
                        log.AppendLine("  ✅ PauseMenuPanel を非アクティブに設定しました");
                        wasFixed = true;
                    }
                }
            }

            // 4. PauseInputHandlerの確認
            log.AppendLine("\n【4】PauseInputHandler の確認:");
            var pauseInputHandlerType = System.Type.GetType("Game.Pause.PauseInputHandler");
            var pauseInputHandler = Object.FindFirstObjectByType(pauseInputHandlerType);

            if (pauseInputHandler == null)
            {
                log.AppendLine("  ❌ PauseInputHandler が見つかりません");

                // PauseInputHandlerを作成
                var player = GameObject.Find("Player");
                if (player != null && pauseInputHandlerType != null)
                {
                    pauseInputHandler = player.AddComponent(pauseInputHandlerType);
                    log.AppendLine("  ✅ PauseInputHandler を Player に追加しました");

                    // GameInputReaderを設定
                    var gameInputReaderType = System.Type.GetType("Game.Player.Script.GameInputReader");
                    var gameInputReader = player.GetComponent(gameInputReaderType);

                    if (gameInputReader != null)
                    {
                        var gameInputReaderField = pauseInputHandlerType.GetField("gameInputReader",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                        if (gameInputReaderField != null)
                        {
                            gameInputReaderField.SetValue(pauseInputHandler, gameInputReader);
                            log.AppendLine("  ✅ GameInputReader を設定しました");
                        }
                    }

                    wasFixed = true;
                }
            }
            else
            {
                log.AppendLine("  ✅ PauseInputHandler が存在します");

                // GameInputReaderが設定されているか確認
                var gameInputReaderField = pauseInputHandlerType.GetField("gameInputReader",
                    System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);

                if (gameInputReaderField != null)
                {
                    var currentReader = gameInputReaderField.GetValue(pauseInputHandler);

                    if (currentReader == null)
                    {
                        log.AppendLine("  ⚠️ GameInputReader が設定されていません");

                        var player = GameObject.Find("Player");
                        if (player != null)
                        {
                            var gameInputReaderType = System.Type.GetType("Game.Player.Script.GameInputReader");
                            var gameInputReader = player.GetComponent(gameInputReaderType);

                            if (gameInputReader != null)
                            {
                                gameInputReaderField.SetValue(pauseInputHandler, gameInputReader);
                                log.AppendLine("  ✅ GameInputReader を設定しました");
                                wasFixed = true;
                            }
                        }
                    }
                    else
                    {
                        log.AppendLine("  ✅ GameInputReader が正しく設定されています");
                    }
                }
            }

            // 5. PauseManagerの確認
            log.AppendLine("\n【5】PauseManager の確認:");
            var pauseManager = GameObject.Find("PauseManager");

            if (pauseManager == null)
            {
                log.AppendLine("  ℹ️ PauseManager GameObject は存在しません（シングルトンが自動生成されます）");
            }
            else
            {
                log.AppendLine("  ✅ PauseManager GameObject が存在します");
            }

            // サマリー
            log.AppendLine("\n=== 結果 ===");

            if (hasIssues)
            {
                log.AppendLine("❌ 修正できない問題が見つかりました。上記のメッセージを確認してください。");
            }
            else if (wasFixed)
            {
                log.AppendLine("✅ 問題を修正しました！");

                // シーンを保存
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
                log.AppendLine("✅ シーンを保存しました");
            }
            else
            {
                log.AppendLine("✅ すべて正常です！問題は見つかりませんでした。");
            }

            // 結果を表示
            Debug.Log(log.ToString());
            EditorUtility.DisplayDialog("PauseMenu 完全修正", log.ToString(), "OK");
        }
    }
}


