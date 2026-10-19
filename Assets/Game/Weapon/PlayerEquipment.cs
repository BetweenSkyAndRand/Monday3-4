using Game.Debug;
using Game.Inventory;
using System;
using UnityEngine;

namespace Game.Weapon
{
    /// <summary>
    /// プレイヤーが現在装備している武器を管理するクラス。
    /// 武器の切り替えを行い、変更を外部（PlayerAttack 等）へイベントで通知する。
    /// 装備の判断ロジックのみを持ち、攻撃処理には関与しない。
    /// </summary>
    public class PlayerEquipment : MonoBehaviour
    {
        [SerializeField] private WeaponData initialWeapon;

        [Header("ビジュアル設定")]
        [Tooltip("武器のビジュアルを表示する WeaponHolder。未設定の場合は自動検索。")]
        [SerializeField] private WeaponHolder weaponHolder;

        [Header("イベント購読設定")]
        [Tooltip("パッチ効果イベントを購読する InventoryController を指定。")]
        [SerializeField] private InventoryController inventoryController;

        /// <summary>武器が切り替わったときに通知する。引数は新しい WeaponData。null の場合は武器なし状態。</summary>
        public event Action<WeaponData> OnWeaponChanged;

        public WeaponData CurrentWeapon { get; private set; }

        /// <summary>
        /// 装備中の武器の有効攻撃力。アイテムのレア度で決まる値で、レア度指定のない武器は WeaponData.DefaultAttackPower になる。
        /// </summary>
        public int CurrentAttackPower { get; private set; }

        /// <summary>
        /// 初期武器データ（Inspectorでアサインされたもの）。
        /// プレイヤー死亡時のリスポーン処理が、初期武器の復元先として参照する。
        /// </summary>
        public WeaponData InitialWeapon => initialWeapon;

        private void Awake()
        {
            // WeaponHolder の自動検索
            if (weaponHolder == null)
            {
                weaponHolder = GetComponentInChildren<WeaponHolder>();
                if (weaponHolder == null)
                {
                    var attachRoot = transform.Find("VisualModel");
                    if (attachRoot == null)
                    {
                        attachRoot = transform;
                    }

                    weaponHolder = attachRoot.gameObject.AddComponent<WeaponHolder>();
                    GameLogger.LogWarning(LogCategory.Player, "WeaponHolder が見つからなかったため自動追加しました。", this);
                }
            }

            // InventoryController のイベント購読は不要になったため削除
        }

        private void Start()
        {
            // 初期武器が設定されていれば装備する
            if (initialWeapon != null)
            {
                Equip(initialWeapon);
            }
        }

        /// <summary>
        /// 武器を切り替える。null を渡すと武器なし状態になる。
        /// attackPower に負の値を渡すと WeaponData.DefaultAttackPower を使う。
        /// </summary>
        public void Equip(WeaponData weapon, int attackPower = -1)
        {
            CurrentWeapon = weapon;
            CurrentAttackPower = weapon == null ? 0 : (attackPower >= 0 ? attackPower : weapon.DefaultAttackPower);

            // WeaponHolder に武器のビジュアル更新を通知
            if (weaponHolder != null)
            {
                weaponHolder.EquipWeapon(weapon);
            }

            OnWeaponChanged?.Invoke(weapon);

            if (weapon != null)
            {
                GameLogger.Log(LogCategory.Player, $"{weapon.WeaponName} を装備しました");
            }
            else
            {
                GameLogger.Log(LogCategory.Player, "武器を解除しました");
            }
        }
    }
}

