using UnityEngine;

namespace Game.Item.Parameter
{
    /// <summary>
    /// 武器のパラメーターを保持するScriptableObject。
    /// 武器エンティティは新インベントリシステムでは WeaponData (Game.Weapon) で管理する。
    /// </summary>
    [CreateAssetMenu(fileName = "WeaponParameter", menuName = "Game/Item/WeaponParameter")]
    public class WeaponParameter : ScriptableObject
    {
        [Header("基本情報")]
        public string weaponId;
        public string weaponName;

        [Header("戦闘パラメーター")]
        public int attackPower = 10;
        public float attackRange = 1.0f;

        [Header("説明")]
        [TextArea(3, 5)]
        public string description;

        [Header("ビジュアル")]
        public Sprite icon;
        public GameObject weaponPrefab;
    }
}
