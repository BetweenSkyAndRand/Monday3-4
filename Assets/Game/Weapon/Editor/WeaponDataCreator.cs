using System.IO;
using UnityEditor;
using UnityEngine;

namespace Game.Weapon.Editor
{
    /// <summary>
    /// WeaponData アセットを JSON ファイルから自動生成するエディター拡張。
    /// Project ウィンドウで JSON ファイルを右クリック → "Create WeaponData from JSON" で実行する。
    /// </summary>
    public static class WeaponDataCreator
    {
        private const string MenuItemPath = "Assets/Create WeaponData from JSON";

        [MenuItem(MenuItemPath, validate = true)]
        private static bool ValidateCreateWeaponData()
        {
            // 選択中のアセットが .json ファイルのときだけメニューを有効にする
            return Selection.activeObject is TextAsset;
        }

        [MenuItem(MenuItemPath)]
        private static void CreateWeaponData()
        {
            var selectedTextAsset = Selection.activeObject as TextAsset;
            if (selectedTextAsset == null)
            {
                EditorUtility.DisplayDialog("エラー", "JSON ファイルを選択してください。", "OK");
                return;
            }

            // JSON のパース確認
            var parsedData = WeaponJsonRepository.LoadFromTextAsset(selectedTextAsset);
            if (parsedData == null)
            {
                EditorUtility.DisplayDialog("エラー", "JSON のパースに失敗しました。形式を確認してください。", "OK");
                return;
            }

            // 選択した JSON と同じフォルダに WeaponData アセットを作成する
            var jsonPath = AssetDatabase.GetAssetPath(selectedTextAsset);
            var assetDirectory = Path.GetDirectoryName(jsonPath) ?? string.Empty;
            var assetName = Path.GetFileNameWithoutExtension(jsonPath);
            var assetPath = Path.Combine(assetDirectory, $"{assetName}.asset").Replace("\\", "/");

            // 同名のアセットが既に存在する場合は上書き確認
            if (File.Exists(Path.Combine(Application.dataPath, "../", assetPath)))
            {
                var overwrite = EditorUtility.DisplayDialog(
                    "確認",
                    $"{assetPath} は既に存在します。上書きしますか？",
                    "上書き", "キャンセル"
                );
                if (!overwrite)
                {
                    return;
                }
            }

            var weaponData = ScriptableObject.CreateInstance<WeaponData>();
            AssetDatabase.CreateAsset(weaponData, assetPath);

            // SerializedObject 経由で weaponJson フィールドに TextAsset をアサインする
            var serializedWeaponData = new SerializedObject(weaponData);
            var weaponJsonProperty = serializedWeaponData.FindProperty("weaponJson");
            weaponJsonProperty.objectReferenceValue = selectedTextAsset;
            serializedWeaponData.ApplyModifiedProperties();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            // 作成したアセットを Project ウィンドウで選択・ハイライトする
            EditorUtility.FocusProjectWindow();
            Selection.activeObject = weaponData;

            UnityEngine.Debug.Log($"[WeaponData] アセットを作成しました: {assetPath} (武器名: {parsedData.weaponName})");
        }
    }
}

