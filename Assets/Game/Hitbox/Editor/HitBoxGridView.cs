using UnityEditor;
using UnityEngine;

namespace DOD.HitBox.Editor
{
    /// <summary>
    /// グリッドの描画とクリック検知を担当する静的ビュー。
    /// ロジックは持たず、操作は Presenter に委譲する。
    /// </summary>
    public static class HitBoxGridView
    {
        private const float CellPx = 36f;
        private const float GridMargin = 20f;
        private const float PlayerIndicatorGap = 6f;

        private static readonly Color ColorEmpty = new Color(0.22f, 0.22f, 0.22f, 1.00f);
        private static readonly Color ColorAxisCol = new Color(0.24f, 0.24f, 0.27f, 1.00f);
        private static readonly Color ColorOccupied = new Color(0.15f, 0.75f, 0.15f, 0.85f);
        private static readonly Color ColorSelected = new Color(0.15f, 0.85f, 0.85f, 0.85f);
        private static readonly Color ColorPlayer = new Color(0.90f, 0.20f, 0.20f, 0.90f);
        private static readonly Color ColorBorder = new Color(0.45f, 0.45f, 0.45f, 1.00f);
        private static readonly Color ColorBorderAxis = new Color(0.80f, 0.80f, 0.25f, 0.60f);

        /// <summary>
        /// グリッドを描画する。
        /// セルがクリックされた場合は true を返す。
        /// </summary>
        public static bool Draw(HitBoxEditorPresenter presenter, Event currentEvent)
        {
            var totalW = HitBoxEditorPresenter.GridCountX * CellPx + GridMargin * 2;
            var totalH = GridMargin + HitBoxEditorPresenter.GridCountY * CellPx + PlayerIndicatorGap + CellPx + GridMargin;

            var area = GUILayoutUtility.GetRect(totalW, totalH, GUILayout.ExpandWidth(false));
            var ox = area.x + GridMargin;
            var oy = area.y + GridMargin;

            var clicked = false;

            for (var gy = 0; gy < HitBoxEditorPresenter.GridCountY; gy++)
            {
                for (var gx = 0; gx < HitBoxEditorPresenter.GridCountX; gx++)
                {
                    var cellRect = new Rect(ox + gx * CellPx, oy + gy * CellPx, CellPx, CellPx);
                    // X: 0始まり絶対座標（gx をそのまま使用）、Y: 上から下へ降順
                    var cx = gx;
                    var cy = HitBoxEditorPresenter.GridMaxY - gy;

                    DrawCell(cellRect, cx, cy, presenter);

                    if (currentEvent.type == EventType.MouseDown && currentEvent.button == 0 && cellRect.Contains(currentEvent.mousePosition))
                    {
                        presenter.ToggleCell(cx, cy);
                        currentEvent.Use();
                        clicked = true;
                    }
                }
            }

            DrawPlayerIndicator(ox, oy);
            return clicked;
        }

        private static void DrawCell(Rect cellRect, int cx, int cy, HitBoxEditorPresenter presenter)
        {
            var occIdx = presenter.FindColliderAt(cx, cy);
            var occupied = occIdx >= 0;
            // グリッド中央列（X=GridHalfX=4）を Y 軸基準列としてハイライト
            var isCenterCol = cx == HitBoxEditorPresenter.GridHalfX;

            Color bg;
            if (occupied && occIdx == presenter.SelectedIndex)
            {
                bg = ColorSelected;
            }
            else if (occupied)
            {
                bg = ColorOccupied;
            }
            else if (isCenterCol)
            {
                bg = ColorAxisCol;
            }
            else
            {
                bg = ColorEmpty;
            }

            EditorGUI.DrawRect(cellRect, bg);
            DrawBorder(cellRect, isCenterCol ? ColorBorderAxis : ColorBorder);

            // 中央列に Y 座標ラベル、最下行に X 座標ラベルを表示する
            if (isCenterCol || cy == HitBoxEditorPresenter.GridMinY)
            {
                var label = isCenterCol ? $"{cy}" : $"{cx}";
                var ls = new GUIStyle(EditorStyles.miniLabel)
                {
                    alignment = TextAnchor.MiddleCenter,
                    fontSize = 8,
                    normal = { textColor = new Color(1f, 1f, 1f, 0.35f) }
                };
                GUI.Label(cellRect, label, ls);
            }
        }

        private static void DrawPlayerIndicator(float ox, float oy)
        {
            // プレイヤーは中央列(X=GridHalfX=4)のグリッド下に位置する → 座標(4,-1)
            var playerX = ox + HitBoxEditorPresenter.GridHalfX * CellPx;
            var playerY = oy + HitBoxEditorPresenter.GridCountY * CellPx + PlayerIndicatorGap;
            var playerRect = new Rect(playerX, playerY, CellPx, CellPx);

            EditorGUI.DrawRect(playerRect, ColorPlayer);
            DrawBorder(playerRect, new Color(1f, 0.4f, 0.4f, 0.9f));

            var pStyle = new GUIStyle(EditorStyles.miniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = 8,
                normal = { textColor = Color.white }
            };
            GUI.Label(playerRect, "P\n(4,-1)", pStyle);
        }

        private static void DrawBorder(Rect r, Color c)
        {
            EditorGUI.DrawRect(new Rect(r.x, r.y, r.width, 1), c);
            EditorGUI.DrawRect(new Rect(r.x, r.yMax - 1, r.width, 1), c);
            EditorGUI.DrawRect(new Rect(r.x, r.y, 1, r.height), c);
            EditorGUI.DrawRect(new Rect(r.xMax - 1, r.y, 1, r.height), c);
        }
    }
}

