using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// 壁透過機能用の透明マテリアルを自動作成するエディター拡張。
/// Tools > Game Setup > Create Wall Transparent Material からアクセス可能。
/// </summary>
public static class WallTransparentMaterialCreator
{
    private const string MaterialPath = "Assets/Material/WallTransparent.mat";

    [MenuItem("Tools/2. Asset Creation/02. Create Wall Transparent Material")]
    public static void CreateMaterial()
    {
        // 既に存在する場合は確認ダイアログを表示
        if (System.IO.File.Exists(MaterialPath))
        {
            if (!EditorUtility.DisplayDialog(
                "確認",
                $"{MaterialPath} は既に存在します。上書きしますか？",
                "上書き", "キャンセル"))
            {
                return;
            }
        }

        // Resources/Material フォルダが存在しない場合は作成
        var resourcesFolder = "Assets/Resources";
        if (!AssetDatabase.IsValidFolder(resourcesFolder))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
            Debug.Log($"[WallTransparentMaterialCreator] フォルダ作成: {resourcesFolder}");
        }

        var folderPath = "Assets/Resources/Material";
        if (!AssetDatabase.IsValidFolder(folderPath))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "Material");
            Debug.Log($"[WallTransparentMaterialCreator] フォルダ作成: {folderPath}");
        }

        // URP/Lit シェーダーを使用してマテリアルを作成
        var shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            EditorUtility.DisplayDialog(
                "エラー",
                "Universal Render Pipeline/Lit シェーダーが見つかりません。\n" +
                "URP がインストールされているか確認してください。",
                "OK");
            return;
        }

        var material = new Material(shader);

        // Surface Type を Transparent に設定
        material.SetFloat("_Surface", 1); // 0 = Opaque, 1 = Transparent
        material.SetFloat("_Blend", 0);   // 0 = Alpha, 1 = Premultiply, 2 = Additive, 3 = Multiply

        // レンダリングモードの設定
        material.SetOverrideTag("RenderType", "Transparent");
        material.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;

        // 基本色（半透明白）
        material.SetColor("_BaseColor", new Color(1f, 1f, 1f, 0.35f));

        // Metallic と Smoothness の設定
        material.SetFloat("_Metallic", 0f);
        material.SetFloat("_Smoothness", 0.5f);

        // キーワード有効化
        material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        material.EnableKeyword("_ALPHAPREMULTIPLY_ON");

        // 既存のアセットを削除してから作成（上書き）
        if (AssetDatabase.LoadAssetAtPath<Material>(MaterialPath) != null)
        {
            AssetDatabase.DeleteAsset(MaterialPath);
        }

        // 保存
        AssetDatabase.CreateAsset(material, MaterialPath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[WallTransparentMaterialCreator] 透明マテリアル作成完了: {MaterialPath}");

        // Inspector で選択状態にする
        Selection.activeObject = material;

        EditorUtility.DisplayDialog(
            "完了",
            $"透明マテリアルを作成しました。\n\n" +
            $"パス: {MaterialPath}\n\n" +
            $"次のステップ:\n" +
            $"1. 壁プレハブに WallTransparencyTarget を追加\n" +
            $"2. Transparent Material に作成したマテリアルを設定\n" +
            $"3. 壁のレイヤーを 'Wall' に設定",
            "OK");
    }
}
#endif

