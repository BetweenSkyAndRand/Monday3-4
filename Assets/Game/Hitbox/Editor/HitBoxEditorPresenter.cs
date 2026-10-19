using System;
using UnityEditor;
using UnityEngine;
using Game.Weapon;

namespace DOD.HitBox.Editor
{
    /// <summary>
    /// ビュー状態とビジネスロジックを一元管理する Presenter。
    /// View はこのクラスのメソッドを呼び出すだけで、判断ロジックを持たない。
    /// 内部データは WeaponJsonData を使用し、武器パラメーター + HitBox データを一元管理する。
    /// </summary>
    [Serializable]
    public class HitBoxEditorPresenter
    {
        // GridHalfX: グリッド中央列のインデックス（X=0..8 の中心 = 4）
        public const int GridHalfX = 4;
        // Y=0 がグリッド最下行。X: 0..8 / Y: 0..8 の 0 始まり座標系
        public const int GridMinY = 0;
        public const int GridMaxY = 8;
        public const int GridCountX = GridHalfX * 2 + 1;
        public const int GridCountY = GridMaxY - GridMinY + 1;

        [SerializeField] private WeaponJsonData weaponData = new WeaponJsonData();
        [SerializeField] private int selectedIndex = -1;

        // 設定の永続化は HitBoxEditorSettings に委譲する
        private HitBoxEditorSettings settings;

        /// <summary>現在のコライダーデータ（SidePanel / GridView から参照される）。</summary>
        public WeaponJsonData Data => weaponData;
        public int SelectedIndex => selectedIndex;

        public string SavePath
        {
            get => settings != null ? settings.SavePath : string.Empty;
            set { if (settings != null) settings.SavePath = value; }
        }

        /// <summary>
        /// コライダーサイズ。設定アセットに永続化しつつ、weaponData にも反映する。
        /// </summary>
        public float ColliderSize
        {
            get => settings != null ? settings.ColliderSize : 1f;
            set
            {
                if (settings != null) settings.ColliderSize = value;
                weaponData.colliderSize = value;
            }
        }

        // ─── 武器パラメーター ───────────────────────────────────────

        /// <summary>武器名。JSON の weaponName フィールドに対応する。</summary>
        public string WeaponName
        {
            get => weaponData.weaponName;
            set => weaponData.weaponName = value;
        }

        /// <summary>ダメージ量。JSON の damage フィールドに対応する。</summary>
        public int Damage
        {
            get => weaponData.damage;
            set => weaponData.damage = value;
        }

        /// <summary>HitBox を有効化する時間（秒）。JSON の attackDuration フィールドに対応する。</summary>
        public float AttackDuration
        {
            get => weaponData.attackDuration;
            set => weaponData.attackDuration = Mathf.Max(0.01f, value);
        }

        /// <summary>次の攻撃まで待機する時間（秒）。JSON の cooldownTime フィールドに対応する。</summary>
        public float CooldownTime
        {
            get => weaponData.cooldownTime;
            set => weaponData.cooldownTime = Mathf.Max(0f, value);
        }

        /// <summary>コライダーをトリガーにするかどうか。JSON の isTrigger フィールドに対応する。</summary>
        public bool IsTrigger
        {
            get => weaponData.isTrigger;
            set => weaponData.isTrigger = value;
        }

        /// <summary>
        /// 武器のビジュアルモデルパス（Resources フォルダからの相対パス）。
        /// JSON の weaponModelPath フィールドに対応する。
        /// </summary>
        public string WeaponModelPath
        {
            get => weaponData.weaponModelPath;
            set => weaponData.weaponModelPath = value;
        }

        // ─────────────────────────────────────────────────────────────

        /// <summary>
        /// 永続化設定を紐付ける。EditorWindow の OnEnable から呼び出す。
        /// 設定に保存されていた ColliderSize を weaponData にも同期する。
        /// </summary>
        public void Initialize(HitBoxEditorSettings editorSettings)
        {
            this.settings = editorSettings;
            weaponData.colliderSize = ColliderSize;
        }

        /// <summary>指定座標にコライダーがあればインデックスを、なければ -1 を返す。</summary>
        public int FindColliderAt(int cx, int cy)
        {
            var relX = ToRelX(cx);
            var relY = ToRelY(cy);
            for (var i = 0; i < weaponData.colliders.Count; i++)
            {
                // float 比較は誤差を考慮して近似値で行う
                if (Mathf.Approximately(weaponData.colliders[i].x, relX) &&
                    Mathf.Approximately(weaponData.colliders[i].y, relY)) return i;
            }
            return -1;
        }

        /// <summary>
        /// セルをトグルする。
        /// 空なら新規コライダーを追加、占有済みなら削除する。
        /// </summary>
        public void ToggleCell(int cx, int cy)
        {
            var idx = FindColliderAt(cx, cy);
            if (idx >= 0)
            {
                weaponData.colliders.RemoveAt(idx);
                if (selectedIndex >= weaponData.colliders.Count)
                {
                    selectedIndex = weaponData.colliders.Count - 1;
                }
            }
            else
            {
                var newIdx = weaponData.colliders.Count;
                weaponData.colliders.Add(new ColliderData($"Collider_{newIdx:D2}", ToRelX(cx), ToRelY(cy)));
                selectedIndex = weaponData.colliders.Count - 1;
            }
        }

        /// <summary>指定インデックスのコライダーを選択する。再度選択すると解除。</summary>
        public void SelectCollider(int index)
        {
            selectedIndex = selectedIndex == index ? -1 : index;
        }

        /// <summary>指定インデックスのコライダーを削除する。</summary>
        public void RemoveCollider(int index)
        {
            if (index < 0 || index >= weaponData.colliders.Count) return;
            weaponData.colliders.RemoveAt(index);
            if (selectedIndex >= weaponData.colliders.Count)
            {
                selectedIndex = weaponData.colliders.Count - 1;
            }
        }

        /// <summary>指定インデックスのコライダー名を変更する。</summary>
        public void RenameCollider(int index, string newName)
        {
            if (index < 0 || index >= weaponData.colliders.Count) return;
            weaponData.colliders[index].name = newName;
        }

        /// <summary>コライダーのみを全削除し、選択状態をリセットする。武器パラメーターは保持する。</summary>
        public void ClearAll()
        {
            weaponData.colliders.Clear();
            selectedIndex = -1;
        }

        /// <summary>現在のデータを SavePath に WeaponJsonData 形式の JSON として保存する。</summary>
        public void Save()
        {
            if (string.IsNullOrEmpty(SavePath)) return;
            WeaponJsonRepository.Save(weaponData, SavePath);
            AssetDatabase.Refresh();
            Debug.Log($"[HitBox Editor] Saved → {SavePath}");
        }

        /// <summary>
        /// 指定パスの JSON を読み込み、データを置き換える。
        /// 旧形式（HitBoxData のみの JSON）を読み込んだ場合は武器パラメーターがデフォルト値になる。
        /// </summary>
        public void Load(string path)
        {
            var loaded = WeaponJsonRepository.Load(path);
            if (loaded == null) return;
            weaponData = loaded;
            selectedIndex = -1;
            // ColliderSize を settings にも反映して永続化する
            if (settings != null) settings.ColliderSize = weaponData.colliderSize;
            // SavePath 経由で設定に書き戻す（即時永続化される）
            SavePath = path;
            Debug.Log($"[HitBox Editor] Loaded ← {path}");
        }

        /// <summary>グリッド絶対座標 X → プレイヤー原点相対 X。</summary>
        private float ToRelX(int cx) => (cx - GridHalfX);

        /// <summary>グリッド絶対座標 Y → Y のまま返す。</summary>
        private float ToRelY(int cy) => cy;
    }
}

