using UnityEngine;

namespace Game.Weapon
{
    /// <summary>
    /// Player/Enemy に武器を持たせるコンポーネント。
    /// 武器のビジュアルを子オブジェクトとして生成・管理する。
    /// </summary>
    public class WeaponHolder : MonoBehaviour
    {
        [Header("設定")]
        [SerializeField] private Transform weaponAttachPoint;

        [Header("武器ビジュアルのオーバーライド（任意）")]
        [Tooltip("WeaponData にビジュアルプレハブが含まれない場合のフォールバック用プレハブ")]
        [SerializeField] private GameObject weaponModelPrefab;

        [Header("配置補正（任意・WeaponData 未設定時のフォールバック）")]
        [Tooltip("WeaponData の WeaponModelScale が 1（未指定）の場合に使うスケール。")]
        [SerializeField] private float fallbackWeaponScale = 1f;
        [Tooltip("WeaponData の WeaponModelOffset がゼロの場合に使うローカル位置オフセット。")]
        [SerializeField] private Vector3 fallbackWeaponOffset = Vector3.zero;
        [Tooltip("WeaponData の WeaponModelRotation がゼロの場合に使うローカル回転（度）。")]
        [SerializeField] private Vector3 fallbackWeaponRotation = Vector3.zero;

        private GameObject currentWeaponObject;

        private void Awake()
        {
            // weaponAttachPoint が未設定の場合は自分自身を使用
            if (weaponAttachPoint == null)
            {
                weaponAttachPoint = transform;
            }
        }

        /// <summary>
        /// 武器を装備する（ビジュアルを生成）。
        /// </summary>
        /// <param name="weaponData">装備する武器データ</param>
        public void EquipWeapon(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                ClearCurrentWeapon();
                UnityEngine.Debug.Log("[WeaponHolder] 武器なし状態になりました");
                return;
            }

            // 素手（名前が "fist"）の場合はビジュアルなし
            if (weaponData.name == "fist")
            {
                ClearCurrentWeapon();
                UnityEngine.Debug.Log($"[WeaponHolder] {weaponData.WeaponName} を装備（ビジュアルなし）");
                return;
            }

            var prefabToUse = weaponData.GetWeaponModelPrefab();
            // JSON で指定された補正値（スケール・オフセット・回転）を優先し、未指定なら fallback を使う。
            var scale = !Mathf.Approximately(weaponData.WeaponModelScale, 0f)
                ? weaponData.WeaponModelScale
                : fallbackWeaponScale;
            // weaponData.WeaponModelScale==1 はデフォルト値の可能性もあるため、JSONの値が 1 以外であれば優先。
            if (Mathf.Approximately(weaponData.WeaponModelScale, 1f) && !Mathf.Approximately(fallbackWeaponScale, 1f))
            {
                scale = fallbackWeaponScale;
            }

            var offset = weaponData.WeaponModelOffset != Vector3.zero
                ? weaponData.WeaponModelOffset
                : fallbackWeaponOffset;

            var rotation = weaponData.WeaponModelRotation != Vector3.zero
                ? weaponData.WeaponModelRotation
                : fallbackWeaponRotation;

            EquipPrefab(prefabToUse != null ? prefabToUse : weaponModelPrefab, weaponData.WeaponName, scale, offset, rotation);
        }

        public void EquipPrefab(GameObject prefabToUse, string displayName)
        {
            EquipPrefab(prefabToUse, displayName, fallbackWeaponScale, fallbackWeaponOffset, fallbackWeaponRotation);
        }

        /// <summary>
        /// 武器プレハブを weaponAttachPoint の子として配置する。
        /// 親（VisualModel など）に lossyScale が掛かっている場合の見た目巨大化を防ぐため、
        /// 親のワールドスケールを打ち消したうえで weaponScale を適用する。
        /// </summary>
        public void EquipPrefab(GameObject prefabToUse, string displayName, float weaponScale, Vector3 weaponOffset, Vector3 weaponRotationEuler)
        {
            ClearCurrentWeapon();

            if (prefabToUse == null)
            {
                UnityEngine.Debug.Log($"[WeaponHolder] {displayName} を装備（ビジュアルなし）");
                return;
            }

            currentWeaponObject = Instantiate(prefabToUse, weaponAttachPoint);
            currentWeaponObject.transform.localPosition = weaponOffset;
            currentWeaponObject.transform.localRotation = Quaternion.Euler(weaponRotationEuler);
            ApplyInverseParentScale(currentWeaponObject.transform, weaponScale);

            UnityEngine.Debug.Log($"[WeaponHolder] {displayName} のビジュアルを生成しました");
        }

        /// <summary>
        /// 武器を非装備にする。
        /// </summary>
        public void UnequipWeapon()
        {
            ClearCurrentWeapon();
        }

        private void ClearCurrentWeapon()
        {
            if (currentWeaponObject != null)
            {
                Destroy(currentWeaponObject);
                currentWeaponObject = null;
            }
        }

        /// <summary>
        /// weaponAttachPoint のワールドスケールを打ち消したうえで weaponScale を掛ける。
        /// 結果として武器は親のスケールに影響されず、weaponScale 単位で表示される。
        /// </summary>
        private void ApplyInverseParentScale(Transform target, float weaponScale)
        {
            if (target == null)
            {
                return;
            }

            var parent = target.parent;
            var parentScale = parent != null ? parent.lossyScale : Vector3.one;
            var safeX = Mathf.Approximately(parentScale.x, 0f) ? 1f : parentScale.x;
            var safeY = Mathf.Approximately(parentScale.y, 0f) ? 1f : parentScale.y;
            var safeZ = Mathf.Approximately(parentScale.z, 0f) ? 1f : parentScale.z;
            target.localScale = new Vector3(weaponScale / safeX, weaponScale / safeY, weaponScale / safeZ);
        }
    }
}
