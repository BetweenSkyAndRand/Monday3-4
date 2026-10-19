using NUnit.Framework;
using Game.Item;
using Game.Item.UseCase;

namespace Game.Tests.Item.Editor
{
    public class ItemSellPriceCalculatorUseCaseTests
    {
        [TestCase(ItemRarity.N, 100u)]
        [TestCase(ItemRarity.R, 1000u)]
        [TestCase(ItemRarity.SR, 10000u)]
        [TestCase(ItemRarity.UR, 100000u)]
        public void Calculate_MultiplierTen_ScalesByRarityStep(ItemRarity rarity, uint expectedPrice)
        {
            var calculator = new ItemSellPriceCalculatorUseCase(10);

            Assert.AreEqual(expectedPrice, calculator.Calculate(100, rarity, true));
        }

        [Test]
        public void Calculate_CustomMultiplier_UsesMultiplierPerStep()
        {
            var calculator = new ItemSellPriceCalculatorUseCase(3);

            Assert.AreEqual(900u, calculator.Calculate(100, ItemRarity.SR, true));
        }

        [Test]
        public void Calculate_NotSellable_ReturnsZero()
        {
            var calculator = new ItemSellPriceCalculatorUseCase(10);

            Assert.AreEqual(0u, calculator.Calculate(100, ItemRarity.UR, false));
        }

        [Test]
        public void Calculate_SellableWithZeroBasePrice_ReturnsMinimumPrice()
        {
            var calculator = new ItemSellPriceCalculatorUseCase(10);

            Assert.AreEqual(ItemSellPriceCalculatorUseCase.MinimumSellPrice, calculator.Calculate(0, ItemRarity.UR, true));
        }

        [Test]
        public void Calculate_Overflow_ClampsToMaxValue()
        {
            var calculator = new ItemSellPriceCalculatorUseCase(10);

            Assert.AreEqual(uint.MaxValue, calculator.Calculate(uint.MaxValue, ItemRarity.UR, true));
        }

        [Test]
        public void Calculate_ZeroMultiplier_TreatedAsOne()
        {
            var calculator = new ItemSellPriceCalculatorUseCase(0);

            Assert.AreEqual(100u, calculator.Calculate(100, ItemRarity.UR, true));
        }
    }
}