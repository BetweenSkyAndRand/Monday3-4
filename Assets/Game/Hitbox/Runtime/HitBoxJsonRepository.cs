using System.IO;
using System.Text;
using UnityEngine;

namespace DOD.HitBox
{
    /// <summary>
    /// HitBoxData の JSON 保存・読み込みを担当するリポジトリ。
    /// 情報の保存以外の責務は持たない。
    /// </summary>
    public static class HitBoxJsonRepository
    {
        /// <summary>HitBoxData を指定パスに整形 JSON として保存する。</summary>
        public static void Save(HitBoxData data, string filePath)
        {
            var json = JsonUtility.ToJson(data, prettyPrint: true);
            File.WriteAllText(filePath, json, Encoding.UTF8);
        }

        /// <summary>
        /// 指定パスの JSON から HitBoxData を読み込む。
        /// ファイルが存在しない場合は null を返す。
        /// </summary>
        public static HitBoxData Load(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            var json = File.ReadAllText(filePath, Encoding.UTF8);
            return JsonUtility.FromJson<HitBoxData>(json);
        }
    }
}

