using UnityEditor;
using UnityEngine;

namespace Editor
{
    public static class TestEditorTool
    {
        [MenuItem("Tools/9. Debug & Test/Test Menu Item")]
        public static void TestMenuItem()
        {
            Debug.Log("[TestEditorTool] メニューが正しく動作しています！");
            EditorUtility.DisplayDialog("テスト成功", "エディタツールは正常に動作しています！", "OK");
        }
    }
}

