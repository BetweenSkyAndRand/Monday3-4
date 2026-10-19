using System.Collections;
using System.Collections.Generic;
using DOD.HitBox;
using Game.Weapon;
using UnityEngine;
namespace Game.Player.Script
{
    /// <summary>
    /// 武器の HitBox 生成・破棄・有効/無効の切り替えを担当するクラス。
    /// 攻撃ロジックや入力には関与しない。
    /// </summary>
    public class HitBoxController : MonoBehaviour
    {
        [Tooltip("HitBox を子として配置する Transform（回転する VisualModel など）。未設定時は自動検出。")]
        [SerializeField] private Transform hitBoxAttachParent;
        private SampleHitBox currentHitBox;
        private readonly List<PlayerWeaponHitBox> weaponHitBoxes = new List<PlayerWeaponHitBox>();
        private PlayerEquipment equipment;
        private Transform defaultHitBoxAttachParent;
        private PlayerModelRotator cachedRotator;
        private bool warnedRotatorMissing;
        private void Awake()
        {
            equipment = GetComponent<PlayerEquipment>();
            if (equipment == null)
            {
                UnityEngine.Debug.LogWarning("[HitBoxController] PlayerEquipment が見つかりません。ダメージ処理が動作しない可能性があります。", this);
            }
            cachedRotator = GetComponent<PlayerModelRotator>();
        }
        public void Rebuild(WeaponData weapon)
        {
            StopAllCoroutines();
            if (currentHitBox != null)
            {
                Destroy(currentHitBox.gameObject);
                currentHitBox = null;
                weaponHitBoxes.Clear();
            }
            var hitBoxData = weapon?.GetHitBoxData();
            if (hitBoxData == null)
            {
                return;
            }
            var hitboxObject = new GameObject($"HitBox_{weapon.WeaponName}");
            var parentTransform = ResolveAttachParent();
            defaultHitBoxAttachParent = parentTransform;
            hitboxObject.transform.SetParent(parentTransform, worldPositionStays: false);
            hitboxObject.transform.localPosition = Vector3.zero;
            hitboxObject.transform.localRotation = Quaternion.identity;
            if (parentTransform == transform && !warnedRotatorMissing)
            {
                UnityEngine.Debug.LogWarning(
                    "[HitBoxController] HitBox の親に VisualModel を解決できなかったため Player ルートに配置しました。" +
                    " 攻撃判定がプレイヤーの向きに追従しません。" +
                    " Inspector で hitBoxAttachParent に回転対象 Transform を割り当てるか、" +
                    " PlayerModelRotator + VisualModel 子オブジェクトを正しく設定してください。", this);
                warnedRotatorMissing = true;
            }
            currentHitBox = hitboxObject.AddComponent<SampleHitBox>();
            currentHitBox.Initialize(hitBoxData, weapon.IsTrigger);
            weaponHitBoxes.Clear();
            var colliders = hitboxObject.GetComponentsInChildren<BoxCollider>(includeInactive: true);
            foreach (var boxCollider in colliders)
            {
                var perColliderHitBox = boxCollider.GetComponent<PlayerWeaponHitBox>();
                if (perColliderHitBox == null)
                {
                    perColliderHitBox = boxCollider.gameObject.AddComponent<PlayerWeaponHitBox>();
                }
                perColliderHitBox.Initialize(equipment, equipment != null ? equipment.CurrentAttackPower : weapon.DefaultAttackPower);
                perColliderHitBox.SetActive(false);
                weaponHitBoxes.Add(perColliderHitBox);
            }
            if (weaponHitBoxes.Count == 0)
            {
                UnityEngine.Debug.LogWarning($"[HitBoxController] BoxCollider が 1 つも生成されませんでした。weapon={weapon.WeaponName}", this);
            }
            currentHitBox.SetActive(false);
        }
        private Transform ResolveAttachParent()
        {
            if (hitBoxAttachParent != null)
            {
                return hitBoxAttachParent;
            }
            if (cachedRotator == null)
            {
                cachedRotator = GetComponent<PlayerModelRotator>();
            }
            if (cachedRotator != null && cachedRotator.VisualModel != null)
            {
                hitBoxAttachParent = cachedRotator.VisualModel;
                return hitBoxAttachParent;
            }
            return transform;
        }
        public IEnumerator ActivateFor(float duration, Quaternion attackRotation)
        {
            if (currentHitBox == null)
            {
                yield break;
            }
            AttachHitBoxToPlayerRoot(attackRotation);
            currentHitBox.SetActive(true);
            foreach (var weaponHitBox in weaponHitBoxes)
            {
                if (weaponHitBox != null)
                {
                    weaponHitBox.BeginAttackWindow();
                }
            }
            yield return new WaitForSeconds(duration);
            currentHitBox.SetActive(false);
            foreach (var weaponHitBox in weaponHitBoxes)
            {
                if (weaponHitBox != null)
                {
                    weaponHitBox.EndAttackWindow();
                }
            }
            RestoreDefaultAttachParent();
        }
        private void AttachHitBoxToPlayerRoot(Quaternion attackRotation)
        {
            if (currentHitBox == null)
            {
                return;
            }
            var hitBoxTransform = currentHitBox.transform;
            hitBoxTransform.SetParent(transform, worldPositionStays: false);
            hitBoxTransform.localPosition = Vector3.zero;
            hitBoxTransform.rotation = attackRotation;
            currentHitBox.RefreshScaleCompensation();
        }
        private void RestoreDefaultAttachParent()
        {
            if (currentHitBox == null)
            {
                return;
            }
            var parentTransform = defaultHitBoxAttachParent != null ? defaultHitBoxAttachParent : ResolveAttachParent();
            currentHitBox.transform.SetParent(parentTransform, worldPositionStays: false);
            currentHitBox.transform.localPosition = Vector3.zero;
            currentHitBox.transform.localRotation = Quaternion.identity;
            currentHitBox.RefreshScaleCompensation();
        }
    }
}
