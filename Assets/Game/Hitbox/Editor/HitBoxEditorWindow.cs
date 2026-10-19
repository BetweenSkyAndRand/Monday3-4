using UnityEditor;
using UnityEngine;

namespace DOD.HitBox.Editor
{
    /// <summary>
    /// HitBox エディターウィンドウ。
    /// 描画は各 View クラスに委譲し、本クラスは組み立てのみ担当する。
    /// Window > HitBox Editor から開く。
    /// </summary>
    public class HitBoxEditorWindow : EditorWindow
    {
        [SerializeField] private HitBoxEditorPresenter presenter;
        private Vector2 sideScroll;

        [MenuItem("DOD/Tools/HitBox Editor")]
        public static void Open()
        {
            var win = GetWindow<HitBoxEditorWindow>("HitBox Editor");
            win.minSize = new Vector2(540, 560);
        }

        private void OnEnable()
        {
            presenter ??= new HitBoxEditorPresenter();
            // 設定アセットをロード（なければ自動生成）し、Presenter に紐付ける
            var settings = HitBoxEditorSettings.LoadOrCreate();
            presenter.Initialize(settings);
        }

        private void OnGUI()
        {
            EditorGUILayout.BeginHorizontal(GUILayout.ExpandHeight(true));
            HitBoxSidePanelView.Draw(presenter, ref sideScroll);
            bool gridChanged = HitBoxGridView.Draw(presenter, Event.current);
            EditorGUILayout.EndHorizontal();

            HitBoxFilePanelView.Draw(presenter);

            if (gridChanged || GUI.changed)
            {
                Repaint();
            }
        }
    }
}

