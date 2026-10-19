using UnityEngine;
using Game.Drop;
using Game.Presentation;

namespace Game.Enemy
{
    [CreateAssetMenu(menuName = "Game/Enemy/EnemyVirusViewData", fileName = "EnemyVirusViewData")]
    public sealed class EnemyVirusViewData : ScriptableObject
    {
        [SerializeField] private string uid;
        [SerializeField] private EnemyVirusType enemyVirusType;
        [SerializeField] private GameObject enemyVirusPrefab;
        [SerializeField] private Vector3 modelLocalOffset;
        [SerializeField] private Vector3 modelLocalScale = Vector3.one;
        [SerializeField] private RuntimeAnimatorController animatorController;
        [Header("演出アセット（フォールバック）")]
        [Tooltip("GamePresentationSettings の enemyPresentations に同じ UID のエントリを登録すると、そちらが優先される。\n未登録の場合のみここの値を使用する。")]
        [SerializeField] private GameObject spawnVfxPrefab;
        [SerializeField] private GameObject hitVfxPrefab;
        [SerializeField] private GameObject deadVfxPrefab;
        [SerializeField] private GameObject weaponModelPrefab;
        [Tooltip("[非推奨] spawnSeId を優先してください。AudioClip フォールバック専用。")]
        [SerializeField] private AudioClip spawnSe;
        [Tooltip("[非推奨] attackSeId を優先してください。AudioClip フォールバック専用。")]
        [SerializeField] private AudioClip attackSe;
        [Tooltip("[非推奨] deadSeId を優先してください。AudioClip フォールバック専用。")]
        [SerializeField] private AudioClip deadSe;
        [Tooltip("[非推奨] damageSeId を優先してください。AudioClip フォールバック専用。")]
        [SerializeField] private AudioClip damageSe;
        [Header("SE ID（GamePresentationSettings.sePresentations 参照）")]
        [Tooltip("空文字の場合は AudioClip フィールドにフォールバックする。設定すると AudioManager.PlayConfiguredSe(seId) 経由で再生される。")]
        [SerializeField] private string spawnSeId = "";
        [SerializeField] private string attackSeId = "";
        [SerializeField] private string deadSeId = "";
        [SerializeField] private string damageSeId = "";
        [SerializeField] private Vector3 hpBarWorldOffset;
        [Header("ドロップ")]
        [Tooltip("この敵の種類のドロップテーブル。未設定なら何も落とさない。")]
        [SerializeField] private EnemyDropTableParameter dropTable;

        public string Uid => uid;
        public EnemyVirusType EnemyVirusType => enemyVirusType;
        public GameObject EnemyVirusPrefab => enemyVirusPrefab;
        public Vector3 ModelLocalOffset => modelLocalOffset;
        public Vector3 ModelLocalScale => modelLocalScale;
        public RuntimeAnimatorController AnimatorController => animatorController;
        public GameObject SpawnVfxPrefab => ResolveSpawnVfxPrefab();
        public GameObject HitVfxPrefab => ResolveHitVfxPrefab();
        public GameObject DeadVfxPrefab => ResolveDeadVfxPrefab();
        public GameObject WeaponModelPrefab => ResolveWeaponModelPrefab();
        public AudioClip SpawnSe => ResolveSpawnSe();
        public AudioClip AttackSe => ResolveAttackSe();
        public AudioClip DeadSe => ResolveDeadSe();
        public AudioClip DamageSe => ResolveDamageSe();
        public Vector3 HpBarWorldOffset => hpBarWorldOffset;
        public EnemyDropTableParameter DropTable => dropTable;

        // ─── ID ベース SE 解決（GamePresentationSettings.enemyPresentations を優先） ───
        public string SpawnSeId => ResolveSpawnSeId();
        public string AttackSeId => ResolveAttackSeId();
        public string DeadSeId => ResolveDeadSeId();
        public string DamageSeId => ResolveDamageSeId();

        private GameObject ResolveSpawnVfxPrefab()
        {
            if (TryGetPresentation(out var presentation) && presentation.spawnVfxPrefab != null)
            {
                return presentation.spawnVfxPrefab;
            }

            return spawnVfxPrefab;
        }

        private GameObject ResolveHitVfxPrefab()
        {
            if (TryGetPresentation(out var presentation) && presentation.hitVfxPrefab != null)
            {
                return presentation.hitVfxPrefab;
            }

            return hitVfxPrefab;
        }

        private GameObject ResolveDeadVfxPrefab()
        {
            if (TryGetPresentation(out var presentation) && presentation.deadVfxPrefab != null)
            {
                return presentation.deadVfxPrefab;
            }

            return deadVfxPrefab;
        }

        private GameObject ResolveWeaponModelPrefab()
        {
            if (TryGetPresentation(out var presentation) && presentation.weaponModelPrefab != null)
            {
                return presentation.weaponModelPrefab;
            }

            return weaponModelPrefab;
        }

        private AudioClip ResolveSpawnSe()
        {
            if (TryGetPresentation(out var presentation) && presentation.spawnSe != null)
            {
                return presentation.spawnSe;
            }

            return spawnSe;
        }

        private AudioClip ResolveAttackSe()
        {
            if (TryGetPresentation(out var presentation) && presentation.attackSe != null)
            {
                return presentation.attackSe;
            }

            return attackSe;
        }

        private AudioClip ResolveDeadSe()
        {
            if (TryGetPresentation(out var presentation) && presentation.deadSe != null)
            {
                return presentation.deadSe;
            }

            return deadSe;
        }

        private AudioClip ResolveDamageSe()
        {
            if (TryGetPresentation(out var presentation) && presentation.damageSe != null)
            {
                return presentation.damageSe;
            }

            return damageSe;
        }

        private string ResolveSpawnSeId()
        {
            if (TryGetPresentation(out var presentation) && !string.IsNullOrEmpty(presentation.spawnSeId))
            {
                return presentation.spawnSeId;
            }

            return spawnSeId ?? string.Empty;
        }

        private string ResolveAttackSeId()
        {
            if (TryGetPresentation(out var presentation) && !string.IsNullOrEmpty(presentation.attackSeId))
            {
                return presentation.attackSeId;
            }

            return attackSeId ?? string.Empty;
        }

        private string ResolveDeadSeId()
        {
            if (TryGetPresentation(out var presentation) && !string.IsNullOrEmpty(presentation.deadSeId))
            {
                return presentation.deadSeId;
            }

            return deadSeId ?? string.Empty;
        }

        private string ResolveDamageSeId()
        {
            if (TryGetPresentation(out var presentation) && !string.IsNullOrEmpty(presentation.damageSeId))
            {
                return presentation.damageSeId;
            }

            return damageSeId ?? string.Empty;
        }

        private bool TryGetPresentation(out EnemyPresentationEntry presentation)
        {
            var settings = GamePresentationSettings.Instance;
            if (settings != null && settings.TryGetEnemyPresentation(uid, out presentation))
            {
                return true;
            }

            presentation = default;
            return false;
        }
    }
}

