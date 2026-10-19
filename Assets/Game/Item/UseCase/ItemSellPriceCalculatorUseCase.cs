namespace Game.Item.UseCase
{
    /// <summary>
    /// 売却額の計算を行うユースケース。
    /// 売却額 = 基本売却額 × レア度倍率 ^ レア度の段階（基本売却額はレア度Nのときの額）。
    /// 売却可能なアイテムの売却額は最低 MinimumSellPrice とする。
    /// </summary>
    public class ItemSellPriceCalculatorUseCase
    {
        public const uint MinimumSellPrice = 1;

        private readonly uint sellPriceMultiplierPerRarity;

        public ItemSellPriceCalculatorUseCase(uint sellPriceMultiplierPerRarity)
        {
            // 倍率0だと売却額が常に0になってしまうため、設定ミス保護として1に補正する
            this.sellPriceMultiplierPerRarity = sellPriceMultiplierPerRarity == 0 ? 1 : sellPriceMultiplierPerRarity;
        }

        public uint Calculate(uint baseSellPrice, ItemRarity rarity, bool isSellable)
        {
            if (!isSellable)
            {
                return 0;
            }

            var rawPrice = (ulong)baseSellPrice;
            var rarityStep = (int)rarity;
            for (var stepIndex = 0; stepIndex < rarityStep; stepIndex++)
            {
                rawPrice *= sellPriceMultiplierPerRarity;
                if (rawPrice > uint.MaxValue)
                {
                    return uint.MaxValue;
                }
            }

            // 基本売却額の設定漏れ（0）でも、売却可能なアイテムが 0G にならないようにする
            return rawPrice < MinimumSellPrice ? MinimumSellPrice : (uint)rawPrice;
        }
    }
}