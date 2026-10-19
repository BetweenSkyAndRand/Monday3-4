using UnityEngine;
using DOD.HitBox;
using Game.Presentation;

namespace Game.Weapon
{
    /// <summary>
    /// 武器 1 種類の設定を表す ScriptableObject。
    /// すべての武器パラメーターは weaponJson（TextAsset）から読み込む。
    /// JSON 1 ファイルで武器の名前・ダメージ・攻撃時間・クールタイム・HitBox を一元管理する。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Game/Weapon/WeaponData")]
    public class WeaponData : ScriptableObject
    {
        [Header("武器設定 JSON")]
        [Tooltip("HitBox Editor で保存した JSON に武器パラメーターを加えたファイルをアサインする")]
        [SerializeField] private TextAsset weaponJson;

        [Header("アイテムマスター")]
        [Tooltip("この武器の ItemParameter。武器名・ItemId・攻撃力・（設定があれば）モデルはここから取得する。ItemParameter.weaponData と相互に設定する。")]
        [SerializeField] private Game.Item.Parameter.ItemParameter itemParameter;

        [Header("武器モデル（直接参照）")]
        [Tooltip("武器の3Dモデルプレハブ。ここで設定した場合はResources経由ではなく直接参照されます（推奨）。未設定の場合はJSONのweaponModelPathからResources.Loadで読み込まれます。")]
        [SerializeField] private GameObject weaponModelPrefab;

        [Header("インベントリアイコン")]
        [Tooltip("インベントリ UI に表示する武器のアイコン Sprite。スクリーンショットや専用アイコン画像を割り当てる。")]
        [SerializeField] private Sprite inventoryIcon;

        [Header("攻撃演出（フォールバック）")]
        [Tooltip("GamePresentationSettings の weaponPresentations に同じ WeaponData のエントリを登録すると、そちらが優先される。\n未登録の場合のみここの値を使用する。")]
        [SerializeField] private GameObject attackVfxPrefab;    // 攻撃開始時のエフェクト
        [SerializeField] private GameObject hitVfxPrefab;       // 敵にヒットした時のエフェクト
        [Tooltip("[非推奨] 新規実装では JSON.attackSeId / WeaponPresentationEntry.attackSeId を優先してください。AudioClip フォールバック専用。")]
        [SerializeField] private AudioClip attackSe;            // 攻撃時のSE（非推奨：seId 移行中の互換用）
        [Tooltip("[非推奨] 新規実装では JSON.hitSeId / WeaponPresentationEntry.hitSeId を優先してください。AudioClip フォールバック専用。")]
        [SerializeField] private AudioClip hitSe;               // ヒット時のSE（非推奨：seId 移行中の互換用）
        [SerializeField] private Vector3 vfxOffset;             // エフェクトの位置オフセット

        // JSON のパースは初回アクセス時に 1 回だけ行い、以降はキャッシュを返す
        private WeaponJsonData cachedData;

        /// <summary>JSON をパースした武器データ。</summary>
        private WeaponJsonData Data
        {
            get
            {
                if (cachedData == null)
                {
                    cachedData = WeaponJsonRepository.LoadFromTextAsset(weaponJson);
                }

                return cachedData;
            }
        }

        public Game.Item.Parameter.ItemParameter ItemParameter => itemParameter;

        /// <summary>表示名。ItemParameter の名前を使い、未設定ならアセット名。</summary>
        public string WeaponName => itemParameter != null && !string.IsNullOrEmpty(itemParameter.ItemName) ? itemParameter.ItemName : name;

        /// <summary>アイテム ID。ItemParameter の ItemId を使い、未設定ならアセット名。</summary>
        public string ItemId => itemParameter != null && !string.IsNullOrEmpty(itemParameter.ItemId) ? itemParameter.ItemId : name;

        /// <summary>レア度 N の攻撃力。レア度指定なしで装備する場合に使う。ItemParameter がなければ 0。</summary>
        public int DefaultAttackPower => itemParameter != null ? itemParameter.GetAttackPower(Game.Item.ItemRarity.N) : 0;
        public float AttackDuration => Data?.attackDuration ?? 0.2f;
        public float CooldownTime => Data?.cooldownTime ?? 0.8f;
        public bool IsTrigger => Data?.isTrigger ?? true;

        /// <summary>
        /// 武器を振るときに Animator.SetTrigger() に渡すトリガー名。
        /// 空または null の場合はアニメ再生をスキップする。
        /// </summary>
        public string AttackAnimationTrigger => Data?.attackAnimationTrigger ?? string.Empty;

        /// <summary>武器モデルのパス（Resources からの相対パス）。</summary>
        public string WeaponModelPath => Data?.weaponModelPath ?? string.Empty;

        /// <summary>武器モデルのスケール倍率（JSON 由来）。デフォルト 1。</summary>
        public float WeaponModelScale => HasItemModel ? itemParameter.WeaponModelScale : (Data?.weaponModelScale ?? 1f);

        private bool HasItemModel => itemParameter != null && itemParameter.WeaponModelPrefab != null;

        /// <summary>武器モデルの localPosition オフセット（JSON 由来）。柄を手元に合わせるためのオフセット。</summary>
        public Vector3 WeaponModelOffset => HasItemModel ? itemParameter.WeaponModelOffset : (Data != null ? Data.weaponModelOffset : Vector3.zero);

        /// <summary>武器モデルの localEulerAngles（度、JSON 由来）。プレハブ向きの補正用。</summary>
        public Vector3 WeaponModelRotation => HasItemModel ? itemParameter.WeaponModelRotation : (Data != null ? Data.weaponModelRotation : Vector3.zero);

        /// <summary>インベントリ UI に表示するアイコン Sprite。Inspector で直接アサインする。</summary>
        public Sprite InventoryIcon => ResolveInventoryIcon();

        // 攻撃演出のプロパティ
        public GameObject AttackVfxPrefab => ResolveAttackVfxPrefab();
        public GameObject HitVfxPrefab => ResolveHitVfxPrefab();
        public AudioClip AttackSe => ResolveAttackSe();
        public AudioClip HitSe => ResolveHitSe();
        public Vector3 VfxOffset => ResolveVfxOffset();

        /// <summary>
        /// 攻撃 SE の ID。GamePresentationSettings.sePresentations から解決される。
        /// 解決順: GamePresentationSettings.weaponPresentations.attackSeId -> JSON attackSeId -> 空文字
        /// 空文字の場合は AttackSe（AudioClip 直）にフォールバックする呼び出し側を期待する。
        /// </summary>
        public string AttackSeId => ResolveAttackSeId();

        /// <summary>
        /// ヒット SE の ID。GamePresentationSettings.sePresentations から解決される。
        /// 解決順: GamePresentationSettings.weaponPresentations.hitSeId -> JSON hitSeId -> 空文字
        /// </summary>
        public string HitSeId => ResolveHitSeId();

        /// <summary>
        /// 武器モデルのプレハブを取得する。
        /// 解決順:
        ///   1. weaponModelPrefab（Inspector で直接アサイン）← 最優先・推奨
        ///   2. GamePresentationSettings.weaponPresentations に明示登録された Prefab
        ///   3. Resources.Load（JSONのweaponModelPath）
        ///   4. (Editor のみ) AssetDatabase によるフォールバック検索
        /// </summary>
        public GameObject GetWeaponModelPrefab()
        {
            if (HasItemModel)
            {
                return itemParameter.WeaponModelPrefab;
            }

            // 1) Inspector で直接アサインされたプレハブ（最優先・ビルド対応）
            if (weaponModelPrefab != null)
            {
                return weaponModelPrefab;
            }

            // 2) GamePresentationSettings 経由
            if (TryGetPresentation(out var presentation) && presentation.weaponModelPrefab != null)
            {
                return presentation.weaponModelPrefab;
            }

            // 3) JSON の weaponModelPath が空なら null
            if (string.IsNullOrEmpty(WeaponModelPath))
            {
                return null;
            }

            // 4) Resources.Load
            var resourcesKey = NormalizeResourcesPath(WeaponModelPath);
            var loadedFromResources = Resources.Load<GameObject>(resourcesKey);
            if (loadedFromResources != null)
            {
                return loadedFromResources;
            }

#if UNITY_EDITOR
            // 2) Editor 時のみ AssetDatabase 経由でフォールバック検索する。
            //    Assets/Resources/ 配下に置かれていない開発用プレハブもエディタ上では参照できる。
            //    ビルドには含まれないため、本番ビルドでは必ず Resources/ 配下に配置すること。
            var editorPrefab = TryLoadPrefabFromAssetDatabase_EditorOnly(WeaponModelPath);
            if (editorPrefab != null)
            {
                UnityEngine.Debug.LogWarning($"[WeaponData] '{name}' のモデルを AssetDatabase 経由で解決しました。\n" +
                    $"パス: {WeaponModelPath}\n" +
                    "ビルド時は Resources.Load では解決できないため、'Assets/Resources/' 配下へ移動するか、" +
                    "GamePresentationSettings.weaponPresentations にプレハブを直接登録してください。", this);
                return editorPrefab;
            }
#endif

            UnityEngine.Debug.LogWarning($"[WeaponData] '{name}' のモデルが見つかりません。" +
                $"weaponModelPath='{WeaponModelPath}'（Resources解決キー='{resourcesKey}'）。" +
                "JSON の weaponModelPath、もしくはアセットの配置パスを確認してください。", this);
            return null;
        }


        private GameObject ResolveAttackVfxPrefab()
        {
            if (TryGetPresentation(out var presentation) && presentation.attackVfxPrefab != null)
            {
                return presentation.attackVfxPrefab;
            }

            // JSON で指定された Resources パスを試みる
            var jsonPath = Data?.attackVfxPath;
            if (!string.IsNullOrEmpty(jsonPath))
            {
                var loaded = Resources.Load<GameObject>(NormalizeResourcesPath(jsonPath));
                if (loaded != null)
                {
                    return loaded;
                }
            }

            return attackVfxPrefab;
        }

        private GameObject ResolveHitVfxPrefab()
        {
            if (TryGetPresentation(out var presentation) && presentation.hitVfxPrefab != null)
            {
                return presentation.hitVfxPrefab;
            }

            var jsonPath = Data?.hitVfxPath;
            if (!string.IsNullOrEmpty(jsonPath))
            {
                var loaded = Resources.Load<GameObject>(NormalizeResourcesPath(jsonPath));
                if (loaded != null)
                {
                    return loaded;
                }
            }

            return hitVfxPrefab;
        }

        private AudioClip ResolveAttackSe()
        {
            if (TryGetPresentation(out var presentation) && presentation.attackSe != null)
            {
                return presentation.attackSe;
            }

            var jsonPath = Data?.attackSePath;
            if (!string.IsNullOrEmpty(jsonPath))
            {
                var loaded = Resources.Load<AudioClip>(NormalizeResourcesPath(jsonPath));
                if (loaded != null)
                {
                    return loaded;
                }
            }

            return attackSe;
        }

        private AudioClip ResolveHitSe()
        {
            if (TryGetPresentation(out var presentation) && presentation.hitSe != null)
            {
                return presentation.hitSe;
            }

            var jsonPath = Data?.hitSePath;
            if (!string.IsNullOrEmpty(jsonPath))
            {
                var loaded = Resources.Load<AudioClip>(NormalizeResourcesPath(jsonPath));
                if (loaded != null)
                {
                    return loaded;
                }
            }

            return hitSe;
        }

        private string ResolveAttackSeId()
        {
            if (TryGetPresentation(out var presentation) && !string.IsNullOrEmpty(presentation.attackSeId))
            {
                return presentation.attackSeId;
            }

            return Data?.attackSeId ?? string.Empty;
        }

        private string ResolveHitSeId()
        {
            if (TryGetPresentation(out var presentation) && !string.IsNullOrEmpty(presentation.hitSeId))
            {
                return presentation.hitSeId;
            }

            return Data?.hitSeId ?? string.Empty;
        }

        private Vector3 ResolveVfxOffset()
        {
            if (TryGetPresentation(out var presentation))
            {
                return presentation.vfxOffset;
            }

            // JSON で vfxOffset を指定している場合は優先する
            if (Data != null && Data.vfxOffset != Vector3.zero)
            {
                return Data.vfxOffset;
            }

            return vfxOffset;
        }

        /// <summary>
        /// インベントリアイコンの解決。
        /// 解決順: JSON inventoryIconPath -> Inspector アサイン -> null
        /// </summary>
        private Sprite ResolveInventoryIcon()
        {
            var jsonPath = Data?.inventoryIconPath;
            if (!string.IsNullOrEmpty(jsonPath))
            {
                var loaded = Resources.Load<Sprite>(NormalizeResourcesPath(jsonPath));
                if (loaded != null)
                {
                    return loaded;
                }
            }

            return inventoryIcon;
        }

        private bool TryGetPresentation(out WeaponPresentationEntry presentation)
        {
            var settings = GamePresentationSettings.Instance;
            if (settings != null && settings.TryGetWeaponPresentation(this, out presentation))
            {
                return true;
            }

            presentation = default;
            return false;
        }

        private static string NormalizeResourcesPath(string resourcePath)
        {
            if (string.IsNullOrEmpty(resourcePath))
            {
                return string.Empty;
            }

            var normalizedPath = resourcePath.Replace("\\", "/");
            const string resourcesPrefix = "Resources/";
            if (normalizedPath.StartsWith(resourcesPrefix))
            {
                normalizedPath = normalizedPath.Substring(resourcesPrefix.Length);
            }

            const string prefabSuffix = ".prefab";
            if (normalizedPath.EndsWith(prefabSuffix))
            {
                normalizedPath = normalizedPath.Substring(0, normalizedPath.Length - prefabSuffix.Length);
            }

            return normalizedPath;
        }

        /// <summary>
        /// JSON に含まれる HitBox コライダー情報を HitBoxData に変換して返す。
        /// HitBoxController.Rebuild() から呼び出される。
        /// </summary>
        public HitBoxData GetHitBoxData()
        {
            if (Data == null)
            {
                return null;
            }

            // WeaponJsonData の colliderSize を HitBoxData の colliserSize（既存フィールド名）にマッピングする
            return new HitBoxData
            {
                colliserSize = Data.colliderSize,
                colliders = Data.colliders
            };
        }

        /// <summary>
        /// エディターでの JSON 差し替えに備え、キャッシュを破棄する。
        /// </summary>
        private void OnValidate()
        {
            cachedData = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Editor のみで使用するフォールバック解決。
        /// 1. パスをそのまま AssetDatabase で読み込みを試す
        /// 2. 失敗したら、ファイル名で AssetDatabase.FindAssets を実行
        /// </summary>
        private static GameObject TryLoadPrefabFromAssetDatabase_EditorOnly(string weaponModelPath)
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
#endif
    }
}
