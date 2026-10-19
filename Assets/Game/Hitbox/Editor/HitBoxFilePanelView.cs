using System.IO;
using UnityEditor;
using UnityEngine;

namespace DOD.HitBox.Editor
{
    /// <summary>
    /// ファイル操作パネルの描画を担当する静的ビュー。
    /// ロジックは持たず、操作は Presenter に委譲する。
    /// </summary>
    public static class HitBoxFilePanelView
    {
        public static void Draw(HitBoxEditorPresenter presenter)
        {
            EditorGUILayout.Space(4);
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);

            // ─── 武器パラメーター入力欄 ───────────────────────────────
            EditorGUILayout.LabelField("Weapon Parameters", EditorStyles.boldLabel);

            // 武器名
            var newWeaponName = EditorGUILayout.TextField("Weapon Name", presenter.WeaponName);
            if (newWeaponName != presenter.WeaponName)
            {
                presenter.WeaponName = newWeaponName;
            }

            // ダメージ
            var newDamage = EditorGUILayout.IntField("Damage", presenter.Damage);
            if (newDamage != presenter.Damage)
            {
                presenter.Damage = Mathf.Max(0, newDamage);
            }

            // 攻撃時間（HitBox 有効時間）
            var newAttackDuration = EditorGUILayout.FloatField("Attack Duration (s)", presenter.AttackDuration);
            if (!Mathf.Approximately(newAttackDuration, presenter.AttackDuration))
            {
                presenter.AttackDuration = newAttackDuration;
            }

            // クールタイム
            var newCooldownTime = EditorGUILayout.FloatField("Cooldown Time (s)", presenter.CooldownTime);
            if (!Mathf.Approximately(newCooldownTime, presenter.CooldownTime))
            {
                presenter.CooldownTime = newCooldownTime;
            }

            // cooldownTime は attackDuration より長くすべきという警告を表示する
            if (presenter.CooldownTime < presenter.AttackDuration)
            {
                EditorGUILayout.HelpBox(
                    "Cooldown Time は Attack Duration より長く設定することを推奨します。",
                    MessageType.Warning
                );
            }

            // トリガー設定
            var newIsTrigger = EditorGUILayout.Toggle("Is Trigger", presenter.IsTrigger);
            if (newIsTrigger != presenter.IsTrigger)
            {
                presenter.IsTrigger = newIsTrigger;
            }

            EditorGUILayout.Space(4);

            // ─── 武器モデル設定 ─────────────────────────────────────────
            EditorGUILayout.LabelField("Weapon Model", EditorStyles.boldLabel);

            // 武器モデルパス（Resources フォルダからの相対パス）
            var newModelPath = EditorGUILayout.TextField("Model Path (Resources)", presenter.WeaponModelPath);
            if (newModelPath != presenter.WeaponModelPath)
            {
                presenter.WeaponModelPath = newModelPath;
            }

            EditorGUILayout.HelpBox(
                "Resources フォルダからの相対パス（拡張子なし）を入力してください。\n例: Weapons/SwordModel, Weapons/Enemy/ClawModel\n空の場合はモデルなし（コライダーのみ）。",
                MessageType.Info
            );

            EditorGUILayout.Space(4);

            // ─── HitBox設定 ──────────────────────────────────────────
            EditorGUILayout.LabelField("HitBox Settings", EditorStyles.boldLabel);

            // コライダーサイズ入力（size/2 が保存座標へのオフセットに使われる）
            var newSize = EditorGUILayout.FloatField("Collider Size", presenter.ColliderSize);
            if (!Mathf.Approximately(newSize, presenter.ColliderSize))
            {
                presenter.ColliderSize = Mathf.Max(0f, newSize);
            }

            EditorGUILayout.Space(4);

            // ─── ファイル保存/読み込み ────────────────────────────────
            EditorGUILayout.LabelField("File", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            presenter.SavePath = EditorGUILayout.TextField("Save Path", presenter.SavePath);
            if (GUILayout.Button("...", GUILayout.Width(26)))
            {
                var dir = Path.GetDirectoryName(presenter.SavePath) ?? "Assets";
                var file = Path.GetFileNameWithoutExtension(presenter.SavePath);
                var picked = EditorUtility.SaveFilePanel("Save Weapon JSON", dir, file, "json");
                if (!string.IsNullOrEmpty(picked))
                {
                    presenter.SavePath = picked;
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Save JSON"))
            {
                presenter.Save();
            }
            if (GUILayout.Button("Load JSON"))
            {
                var picked = EditorUtility.OpenFilePanel("Load Weapon JSON", "Assets", "json");
                if (!string.IsNullOrEmpty(picked))
                {
                    presenter.Load(picked);
                }
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.EndVertical();
        }
    }
}
