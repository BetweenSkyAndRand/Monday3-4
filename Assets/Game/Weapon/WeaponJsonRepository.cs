using System.IO;
using System.Text;
using UnityEngine;

namespace Game.Weapon
{
    /// <summary>
    /// WeaponJsonData の JSON 保存・読み込みを担当するリポジトリ。
    /// 情報の保存以外の責務は持たない。
    /// </summary>
    public static class WeaponJsonRepository
    {
        /// <summary>WeaponJsonData を指定パスに整形 JSON として保存する。</summary>
        public static void Save(WeaponJsonData data, string filePath)
        {
            var directoryPath = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directoryPath) && !Directory.Exists(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        /// <summary>
        /// 指定パスの JSON から WeaponJsonData を読み込む。
        /// ファイルが存在しない場合は null を返す。
        /// </summary>
        public static WeaponJsonData Load(string filePath)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonUtility.FromJson<WeaponJsonData>(json);
        }

        /// <summary>
        /// TextAsset から WeaponJsonData を読み込む（ランタイム用）。
        /// textAsset が null の場合は null を返す。
        /// </summary>
        public static WeaponJsonData LoadFromTextAsset(TextAsset textAsset)
        {
            if (textAsset == null)
            {
                return null;
            }

            return JsonUtility.FromJson<WeaponJsonData>(textAsset.text);
        }
    }
}


