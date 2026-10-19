using UnityEditor;
using UnityEngine;

namespace DOD.HitBox.Editor
{
    /// <summary>
    /// コライダーリストの描画を担当する静的ビュー。
    /// ロジックは持たず、操作は Presenter に委譲する。
    /// </summary>
    public static class HitBoxSidePanelView
    {
        public static void Draw(HitBoxEditorPresenter presenter, ref Vector2 scroll)
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(160), GUILayout.ExpandHeight(true));
            GUILayout.Label("Colliders", EditorStyles.boldLabel);

            scroll = EditorGUILayout.BeginScrollView(scroll, GUILayout.ExpandHeight(true));
            for (var i = 0; i < presenter.Data.colliders.Count; i++)
            {
                var col = presenter.Data.colliders[i];
                var isSel = i == presenter.SelectedIndex;

                EditorGUILayout.BeginHorizontal();

                GUI.backgroundColor = isSel ? new Color(0.3f, 0.9f, 0.9f) : Color.white;
                if (GUILayout.Button($"{col.name} ({col.x},{col.y})", EditorStyles.miniButton, GUILayout.ExpandWidth(true)))
                {
                    presenter.SelectCollider(i);
                }
                GUI.backgroundColor = Color.white;

                if (GUILayout.Button("x", EditorStyles.miniButton, GUILayout.Width(22)))
                {
                    presenter.RemoveCollider(i);
                    GUIUtility.ExitGUI();
                    return;
                }

                EditorGUILayout.EndHorizontal();

                if (isSel)
                {
                    var renamed = EditorGUILayout.TextField(col.name);
                    if (renamed != col.name)
                    {
                        presenter.RenameCollider(i, renamed);
                    }
                }
            }
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space(4);
            GUILayout.Label($"Count: {presenter.Data.colliders.Count}", EditorStyles.miniLabel);

            if (GUILayout.Button("Clear All"))
            {
                presenter.ClearAll();
            }

            EditorGUILayout.EndVertical();
        }
    }
}

