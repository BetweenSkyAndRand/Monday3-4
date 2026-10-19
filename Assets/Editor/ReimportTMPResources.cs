using UnityEditor;
using UnityEngine;
using System.IO;

namespace Editor
{
    /// <summary>
    /// TextMeshProリソースを完全に再インポートするツール
    /// </summary>
    public static class ReimportTMPResources
    {
        [MenuItem("Tools/Fix TMP/1. Delete and Reimport TMP Resources")]
        public static void DeleteAndReimportTMP()
        {
            if (!EditorUtility.DisplayDialog("TextMeshPro リソース再インポート",
                "TextMeshProのリソースを削除して再インポートします。\n" +
                "これにより、破損したフォントアセットが修復されます。\n\n" +
                "続行しますか？",
                "はい", "キャンセル"))
            {
                return;
            }

            Debug.Log("=== TextMeshPro リソース再インポート開始 ===");

            // 1. 既存のTMPリソースフォルダを削除
            var tmpResourcesPath = "Assets/TextMesh Pro";
            if (Directory.Exists(tmpResourcesPath))
            {
                Debug.Log($"既存のフォルダを削除: {tmpResourcesPath}");
                AssetDatabase.DeleteAsset(tmpResourcesPath);
                AssetDatabase.Refresh();
            }

            // 2. TMP Settingsも削除
            var tmpSettingsPath = "ProjectSettings/TMP Settings.asset";
            if (File.Exists(tmpSettingsPath))
            {
                Debug.Log($"TMP Settingsを削除: {tmpSettingsPath}");
                File.Delete(tmpSettingsPath);
            }

            AssetDatabase.Refresh();

            Debug.Log("削除完了。次に Window > TextMeshPro > Import TMP Essential Resources を実行してください。");

            // TMPインポートウィンドウを開く
            EditorApplication.ExecuteMenuItem("Window/TextMeshPro/Import TMP Essential Resources");
        }

        [MenuItem("Tools/6. Fix Tools/TMP/02. [Diagnostic] Check TMP Font Asset")]
        public static void CheckFontAsset()
        {
            Debug.Log("=== TextMeshPro フォントアセット確認 ===");

            var fontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontPath);

            if (fontAsset == null)
            {
                Debug.LogError($"フォントアセットが見つかりません: {fontPath}");
                Debug.LogError("Window > TextMeshPro > Import TMP Essential Resources を実行してください");
                return;
            }

            Debug.Log($"✓ フォントアセット: {fontAsset.name}");
            Debug.Log($"  - GUID: {AssetDatabase.AssetPathToGUID(fontPath)}");

            if (fontAsset.material == null)
            {
                Debug.LogError("  ✗ マテリアルがnullです！");
            }
            else
            {
                Debug.Log($"  ✓ マテリアル: {fontAsset.material.name}");
                Debug.Log($"    - Shader: {(fontAsset.material.shader != null ? fontAsset.material.shader.name : "NULL")}");
            }

            if (fontAsset.atlasTexture == null)
            {
                Debug.LogError("  ✗ Atlasテクスチャがnullです！");
            }
            else
            {
                Debug.Log($"  ✓ Atlas: {fontAsset.atlasTexture.width}x{fontAsset.atlasTexture.height}");
            }

            // TMP Settingsの確認
            var defaultFont = TMPro.TMP_Settings.defaultFontAsset;
            if (defaultFont == null)
            {
                Debug.LogWarning("TMP Settings: デフォルトフォントが設定されていません");
            }
            else
            {
                Debug.Log($"✓ TMP Settings デフォルトフォント: {defaultFont.name}");
            }

            Debug.Log("=== 確認完了 ===");
        }

        [MenuItem("Tools/6. Fix Tools/TMP/03. [DANGER] Fix All Scene Font References")]
        public static void FixSceneFontReferences()
        {
            Debug.Log("=== シーンのフォント参照を修正 ===");

            var fontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
            var fontAsset = AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(fontPath);

            if (fontAsset == null)
            {
                Debug.LogError("LiberationSans SDFが見つかりません。先にImportを実行してください。");
                return;
            }

            var correctGuid = AssetDatabase.AssetPathToGUID(fontPath);
            Debug.Log($"正しいフォントGUID: {correctGuid}");

            var scenePaths = new[]
            {
                "Assets/Scenes/TitleScene.unity",
                "Assets/Scenes/GameScene.unity",
                "Assets/Scenes/GameOverScene.unity",
                "Assets/Scenes/GameClearScene.unity"
            };

            foreach (var scenePath in scenePaths)
            {
                if (!File.Exists(scenePath))
                {
                    continue;
                }

                var scene = UnityEditor.SceneManagement.EditorSceneManager.OpenScene(scenePath,
                    UnityEditor.SceneManagement.OpenSceneMode.Single);

                var textComponents = Object.FindObjectsByType<TMPro.TextMeshProUGUI>(FindObjectsSortMode.None);
                var fixedCount = 0;

                foreach (var text in textComponents)
                {
                    if (text.font == null || text.font.material == null)
                    {
                        text.font = fontAsset;
                        fixedCount++;
                        EditorUtility.SetDirty(text);
                    }
                }

                if (fixedCount > 0)
                {
                    UnityEditor.SceneManagement.EditorSceneManager.SaveScene(scene);
                    Debug.Log($"✓ {Path.GetFileName(scenePath)}: {fixedCount}個のテキストを修正");
                }
                else
                {
                    Debug.Log($"○ {Path.GetFileName(scenePath)}: 修正不要");
                }
            }

            Debug.Log("=== 修正完了 ===");
        }
    }
}

