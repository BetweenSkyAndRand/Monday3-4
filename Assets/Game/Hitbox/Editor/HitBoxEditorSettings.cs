using System.IO;
using UnityEditor;
using UnityEngine;

namespace DOD.HitBox.Editor
{
    /// <summary>
    /// HitBox エディターの設定を永続化する ScriptableObject。
    /// アセットとして保存されるため、エディターを再起動しても設定が保持される。
    /// </summary>
    public class HitBoxEditorSettings : ScriptableObject
    {
        // アセットの保存先パス（固定）
        private static readonly string AssetPath = "Assets/Game/Hitbox/Data/HitBoxEditorSettings.asset";

        [SerializeField] private string savePath = "Assets/HitBox.json";
        [SerializeField] private float colliderSize = 1f;

        /// <summary>
        /// JSON 保存先パス。変更のたびにアセットを即時保存する。
        /// </summary>
        public string SavePath
        {
            get => savePath;
            set
            {
                if (savePath == value) return;
                savePath = value;
                // 変更を即時アセットに反映する
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// コライダーサイズ。変更のたびにアセットを即時保存する。
        /// </summary>
        public float ColliderSize
        {
            get => colliderSize;
            set
            {
                if (Mathf.Approximately(colliderSize, value)) return;
                colliderSize = value;
                EditorUtility.SetDirty(this);
                AssetDatabase.SaveAssets();
            }
        }

        /// <summary>
        /// 設定アセットをロードする。存在しない場合は自動生成する。
        /// </summary>
        public static HitBoxEditorSettings LoadOrCreate()
        {
            var settings = AssetDatabase.LoadAssetAtPath<HitBoxEditorSettings>(AssetPath);
            if (settings != null) return settings;

            // フォルダが存在しない場合は作成する
            var dir = Path.GetDirectoryName(AssetPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
                AssetDatabase.Refresh();
            }

            settings = CreateInstance<HitBoxEditorSettings>();
            AssetDatabase.CreateAsset(settings, AssetPath);
            AssetDatabase.SaveAssets();
            Debug.Log($"[HitBox Editor] 設定アセットを作成しました → {AssetPath}");
            return settings;
        }
    }
}
