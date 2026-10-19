using UnityEngine;
using Game.Weapon;

namespace Game.Enemy
{
    public sealed class EnemyVirusWeaponComponent : MonoBehaviour
    {
        [SerializeField] private WeaponHolder weaponHolder;

        public WeaponEntity WeaponEntity { get; private set; }

        public void Initialize(WeaponEntity weaponEntity, EnemyVirusViewData enemyViewData = null)
        {
            WeaponEntity = weaponEntity;

            EnsureWeaponHolder();

            if (weaponHolder == null)
            {
                return;
            }

            var weaponModelPrefab = enemyViewData != null ? enemyViewData.WeaponModelPrefab : null;
            if (weaponModelPrefab == null)
            {
                weaponHolder.UnequipWeapon();
                return;
            }

            var displayName = enemyViewData != null ? enemyViewData.Uid : gameObject.name;
            weaponHolder.EquipPrefab(weaponModelPrefab, displayName);
        }

        private void EnsureWeaponHolder()
        {
            if (weaponHolder != null)
            {
                return;
            }

            weaponHolder = GetComponentInChildren<WeaponHolder>();
            if (weaponHolder != null)
            {
                return;
            }

            var attachRoot = transform.Find("VisualModel");
            if (attachRoot == null)
            {
                attachRoot = transform;
            }

            weaponHolder = attachRoot.gameObject.AddComponent<WeaponHolder>();
        }
    }
}

