using UnityEditor;
using UnityEngine;

namespace Editor
{
    /// <summary>
    /// Built-inシェーダーを使用しているマテリアルをURPに変換するツール
    /// </summary>
    public static class MaterialConverterTool
    {
        [MenuItem("Tools/3. Material Tools/01. [DANGER] Convert Built-in to URP")]
        public static void ConvertBuiltInToUrp()
        {
            // 確認ダイアログを表示
            if (!EditorUtility.DisplayDialog(
                "マテリアル変換",
                "プロジェクト内のBuilt-inシェーダーを使用しているマテリアルをURPに変換します。\n\n" +
                "この操作は元に戻せない可能性があります。\n" +
                "事前にプロジェクトのバックアップを取ることをお勧めします。\n\n" +
                "続行しますか？",
                "変換する",
                "キャンセル"))
            {
                Debug.Log("[MaterialConverter] 変換がキャンセルされました");
                return;
            }

            // プロジェクト内の全マテリアルを取得
            var materialGuids = AssetDatabase.FindAssets("t:Material");
            var convertedCount = 0;
            var skippedCount = 0;
            var errorCount = 0;

            Debug.Log($"[MaterialConverter] {materialGuids.Length}個のマテリアルをスキャン中...");

            foreach (var guid in materialGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null)
                {
                    continue;
                }

                // シェーダー名を取得
                var shaderName = material.shader.name;

                // Built-inシェーダーかチェック
                if (IsBuiltInShader(shaderName))
                {
                    Debug.Log($"[MaterialConverter] 変換中: {path} (Shader: {shaderName})");

                    try
                    {
                        // URPシェーダーに変換
                        var success = ConvertMaterialToUrp(material, shaderName);

                        if (success)
                        {
                            EditorUtility.SetDirty(material);
                            convertedCount++;
                            Debug.Log($"[MaterialConverter] ✓ 変換成功: {path}");
                        }
                        else
                        {
                            skippedCount++;
                            Debug.LogWarning($"[MaterialConverter] ⚠ スキップ: {path} (変換方法が見つかりません)");
                        }
                    }
                    catch (System.Exception e)
                    {
                        errorCount++;
                        Debug.LogError($"[MaterialConverter] ✗ 変換エラー: {path}\n{e.Message}");
                    }
                }
            }

            // 変更を保存
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 結果を表示
            var resultMessage = $"マテリアル変換が完了しました\n\n" +
                               $"変換成功: {convertedCount}個\n" +
                               $"スキップ: {skippedCount}個\n" +
                               $"エラー: {errorCount}個";

            EditorUtility.DisplayDialog("変換完了", resultMessage, "OK");
            Debug.Log($"[MaterialConverter] {resultMessage}");
        }

        /// <summary>
        /// Built-inシェーダーかどうかを判定
        /// </summary>
        private static bool IsBuiltInShader(string shaderName)
        {
            return shaderName.StartsWith("Standard") ||
                   shaderName.StartsWith("Legacy Shaders/") ||
                   shaderName.StartsWith("Mobile/") ||
                   shaderName.StartsWith("Particles/") ||
                   shaderName == "Unlit/Texture" ||
                   shaderName == "Unlit/Color";
        }

        /// <summary>
        /// マテリアルをURPシェーダーに変換
        /// </summary>
        private static bool ConvertMaterialToUrp(Material material, string oldShaderName)
        {
            Shader newShader;

            // 既存のテクスチャを保存
            var mainTex = material.GetTexture("_MainTex");
            var bumpMap = material.GetTexture("_BumpMap");
            var emissionMap = material.GetTexture("_EmissionMap");
            var metallicGlossMap = material.GetTexture("_MetallicGlossMap");

            // 既存の色を保存
            var color = material.HasProperty("_Color") ? material.GetColor("_Color") : Color.white;
            var emissionColor = material.HasProperty("_EmissionColor") ? material.GetColor("_EmissionColor") : Color.black;

            // 既存の値を保存
            var metallic = material.HasProperty("_Metallic") ? material.GetFloat("_Metallic") : 0f;
            var smoothness = material.HasProperty("_Glossiness") ? material.GetFloat("_Glossiness") : 0.5f;

            // シェーダータイプに応じて変換
            if (oldShaderName.Contains("Standard"))
            {
                newShader = Shader.Find("Universal Render Pipeline/Lit");
            }
            else if (oldShaderName.Contains("Unlit"))
            {
                newShader = Shader.Find("Universal Render Pipeline/Unlit");
            }
            else if (oldShaderName.Contains("Transparent"))
            {
                newShader = Shader.Find("Universal Render Pipeline/Lit");
            }
            else
            {
                // デフォルトはLit
                newShader = Shader.Find("Universal Render Pipeline/Lit");
            }

            if (newShader == null)
            {
                Debug.LogError($"[MaterialConverter] URPシェーダーが見つかりません");
                return false;
            }

            // シェーダーを変更
            material.shader = newShader;

            // テクスチャを再設定（Built-inの_MainTexをURPの_BaseMapに）
            if (mainTex != null)
            {
                material.SetTexture("_BaseMap", mainTex);
            }

            if (bumpMap != null)
            {
                material.SetTexture("_BumpMap", bumpMap);
            }

            if (emissionMap != null)
            {
                material.SetTexture("_EmissionMap", emissionMap);
                material.EnableKeyword("_EMISSION");
            }

            if (metallicGlossMap != null)
            {
                material.SetTexture("_MetallicGlossMap", metallicGlossMap);
            }

            // 色を再設定
            material.SetColor("_BaseColor", color);

            if (emissionColor != Color.black)
            {
                material.SetColor("_EmissionColor", emissionColor);
                material.EnableKeyword("_EMISSION");
            }

            // 値を再設定
            material.SetFloat("_Metallic", metallic);
            material.SetFloat("_Smoothness", smoothness);

            return true;
        }

        [MenuItem("Tools/3. Material Tools/02. [Diagnostic] List Built-in Materials")]
        public static void ListBuiltInMaterials()
        {
            var materialGuids = AssetDatabase.FindAssets("t:Material");
            var builtInMaterials = 0;

            Debug.Log("[MaterialConverter] ====== Built-inシェーダーを使用しているマテリアル ======");

            foreach (var guid in materialGuids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);

                if (material == null)
                {
                    continue;
                }

                var shaderName = material.shader.name;

                if (IsBuiltInShader(shaderName))
                {
                    builtInMaterials++;
                    Debug.Log($"[MaterialConverter] {path}\n  Shader: {shaderName}");
                }
            }

            Debug.Log($"[MaterialConverter] ====== 合計: {builtInMaterials}個のBuilt-inマテリアル ======");

            EditorUtility.DisplayDialog(
                "Built-inマテリアル一覧",
                $"Built-inシェーダーを使用しているマテリアル: {builtInMaterials}個\n\n" +
                "詳細はConsoleウィンドウを確認してください。",
                "OK");
        }
    }
}




