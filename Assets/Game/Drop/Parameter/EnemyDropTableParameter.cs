using System.Collections.Generic;
using UnityEngine;

namespace Game.Drop
{
    /// <summary>
    /// 敵の種類ごとのドロップテーブル。2段階抽選：①「なし」＋レア度プールから1つ → ②当たったレア度のアイテムから1つ。
    /// 敵強化によるドロップ率の補正は「なし」以外のプールの重みにだけ掛かる。
    /// </summary>
    [CreateAssetMenu(menuName = "Game/Drop/EnemyDropTable", fileName = "EnemyDropTable")]
    public sealed class EnemyDropTableParameter : ScriptableObject
    {
        [Tooltip("何もドロップしない重み。")]
        [Min(0f)]
        [SerializeField] private float noneWeight = 100f;
        [SerializeField] private List<DropRarityPool> rarityPools = new List<DropRarityPool>();

        public float NoneWeight => noneWeight;
        public IReadOnlyList<DropRarityPool> RarityPools => rarityPools;
    }
}