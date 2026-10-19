#if UNITY_EDITOR
using UnityEngine;

namespace Game.Weapon.Editor
{
    /// <summary>
    /// WeaponData の Editor 専用ヘルパークラス。
    /// AssetDatabase を使用したプレハブ検索などの Editor 限定処理を担当する。
    /// ビルド時には含まれない。
    /// </summary>
    public static class WeaponDataEditorHelper
    {
        /// <summary>
        /// Editor のみで使用するフォールバック解決。
        /// 1. パスをそのまま AssetDatabase で読み込みを試す
        /// 2. 失敗したら、ファイル名で AssetDatabase.FindAssets を実行
        /// </summary>
        public static GameObject TryLoadPrefabFromAssetDatabase(string weaponModelPath)
        {
            // パス直指定でロードできるか試す
            var directCandidates = new[]
            {
                $"Assets/{weaponModelPath}.prefab",
                $"Assets/{weaponModelPath}",
                weaponModelPath.EndsWith(".prefab") ? weaponModelPath : weaponModelPath + ".prefab",
            };

            foreach (var candidate in directCandidates)
            {
                var direct = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(candidate);
                if (direct != null)
                {
                    return direct;
                }
            }

            // ファイル名でプロジェクト全体を検索する
            var fileName = System.IO.Path.GetFileNameWithoutExtension(weaponModelPath);
            if (string.IsNullOrEmpty(fileName))
            {
                return null;
            }

            var guids = UnityEditor.AssetDatabase.FindAssets($"\"{fileName}\" t:GameObject");
            foreach (var guid in guids)
            {
                var assetPath = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var assetName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
                // ファイル名完全一致のものを優先（部分一致を避ける）
                if (string.Equals(assetName, fileName, System.StringComparison.Ordinal))
                {
                    var found = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
                    if (found != null)
                    {
                        return found;
                    }
                }
            }

            return null;
        }
    }
}
#endif

