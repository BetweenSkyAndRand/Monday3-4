using UnityEngine;
using Game.Interaction;
using Game.Inventory;
using Game.Weapon;

namespace Game.InteractObj
{
    /// <summary>
    /// シーンに配置された武器アイテム。
    /// プレイヤーがインタラクトすると PlayerEquipment.Equip() を呼んで装備させ、自身を Destroy する。
    /// </summary>
    public class WeaponPickup : MonoBehaviour, IInteractableObject
    {
        [SerializeField] private WeaponData weaponData;

        [Header("UI表示設定")]
        [Tooltip("インタラクトテキスト設定 ScriptableObject")]
        [SerializeField] private InteractTextSettings textSettings;

        /// <summary>
        /// 装備先。Inspector でアサインする。
        /// null の場合は Awake でシーン内から検索する（フォールバック）。
        /// </summary>
        [SerializeField] private PlayerEquipment playerEquipment;

        /// <summary>セルサイズ（プレイヤーと同じ値を設定）</summary>
        [SerializeField] private float cellSize = 1f;

        private void Awake()
        {
            // Inspector 未アサインの場合のみシーン検索をフォールバックとして実行する。
            // Interact() での毎回 FindFirstObjectByType 呼び出しを避ける。
            if (playerEquipment == null)
            {
                playerEquipment = FindFirstObjectByType<PlayerEquipment>();
            }
        }

        private void Start()
        {
            // このオブジェクトがいるグリッド座標を占有する
            var gridPos = new Vector2Int(
                Mathf.RoundToInt(transform.position.x / cellSize),
                Mathf.RoundToInt(transform.position.z / cellSize)
            );
            Game.Grid.GridOccupationManager.Occupy(gridPos, gameObject);
        }

        private void OnDestroy()
        {
            // グリッド占有を解放
            Game.Grid.GridOccupationManager.ReleaseByOccupant(gameObject);
        }

        public string GetInteractText()
        {
            //武器データが未設定の場合はインタラクト不可
            if (weaponData == null)
            {
                return string.Empty;
            }

            // textSettings が未設定の場合はフォールバック
            if (textSettings == null)
            {
                return $"Pick Up {weaponData.WeaponName}";
            }

            return string.Format(textSettings.weaponPickupFormat, weaponData.WeaponName);
        }

        public void Interact()
        {
            if (weaponData == null)
            {
                return;
            }

            if (InventoryRuntime.TryGetInventory(out var inventory))
            {
                var addResult = inventory.TryAddWeapon(weaponData);
                if (!addResult.IsSuccess)
                {
                    UnityEngine.Debug.LogWarning($"[WeaponPickup] 武器追加に失敗: {addResult.Error}");
                    return;
                }

                Destroy(gameObject);
                return;
            }

            if (playerEquipment == null)
            {
                UnityEngine.Debug.LogError("[WeaponPickup] PlayerEquipment が見つかりません。");
                return;
            }

            playerEquipment.Equip(weaponData);
            Destroy(gameObject);
        }
    }
}

