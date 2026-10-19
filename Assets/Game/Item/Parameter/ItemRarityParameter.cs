using UnityEngine;

namespace Game.Item.Parameter
{
    /// <summary>
    /// レア度に関する共通設定。売却額 = 基本売却額 × 倍率 ^ レア度の段階。
    /// アイコン枠の Sprite もここで管理する。
    /// </summary>
    [CreateAssetMenu(fileName = "ItemRarityParameter", menuName = "Game/Item/ItemRarityParameter")]
    public class ItemRarityParameter : ScriptableObject
    {
        [Tooltip("レア度が1段階上がるごとに売却額へかかる倍率。")]
        [SerializeField] private uint sellPriceMultiplierPerRarity = 10;

        [Header("アイコン枠（アイコンの上に重ねて表示する）")]
        [SerializeField] private Sprite frameN;
        [SerializeField] private Sprite frameR;
        [SerializeField] private Sprite frameSR;
        [SerializeField] private Sprite frameUR;

        public uint SellPriceMultiplierPerRarity => sellPriceMultiplierPerRarity;

        /// <summary>レア度に対応するアイコン枠。未設定の場合は null（枠なし）。</summary>
        public Sprite GetFrameSprite(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.N:
                    return frameN;
                case ItemRarity.R:
                    return frameR;
                case ItemRarity.SR:
                    return frameSR;
                case ItemRarity.UR:
                    return frameUR;
                default:
                    return null;
            }
        }
    }
}