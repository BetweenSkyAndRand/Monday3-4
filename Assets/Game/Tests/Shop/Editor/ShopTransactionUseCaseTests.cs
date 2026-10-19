using System.Reflection;
using Game.Inventory;
using Game.Inventory.UseCase;
using Game.Item;
using Game.Item.Parameter;
using Game.Item.UseCase;
using Game.Shop;
using Game.Weapon;
using NUnit.Framework;
using UnityEngine;

namespace Game.Tests.Shop
{
    public class ShopTransactionUseCaseTests
    {
        private InventoryRepository repository;
        private FakeInventory inventory;
        private FakeWallet wallet;
        private ItemParameter potion;
        private ItemParameter material;
        private ItemDatabase itemDatabase;
        private ShopSellTargetUseCase sellTargetUseCase;
        private ShopPurchaseUseCase purchaseUseCase;
        private ShopSellUseCase sellUseCase;

        [SetUp]
        public void SetUp()
        {
            repository = new InventoryRepository(2);
            inventory = new FakeInventory(repository);
            wallet = new FakeWallet(1000);
            potion = CreateItem("potion", ItemCategory.Recovery, 5, 100, 30, true);
            material = CreateItem("junk", ItemCategory.SellMaterial, 5, 0, 0, true);
            itemDatabase = ScriptableObject.CreateInstance<ItemDatabase>();
            SetField(itemDatabase, "items", new[] { potion, material });
            sellTargetUseCase = new ShopSellTargetUseCase(itemDatabase, new ItemSellPriceCalculatorUseCase(10));
            purchaseUseCase = new ShopPurchaseUseCase(inventory, wallet);
            sellUseCase = new ShopSellUseCase(inventory, wallet, sellTargetUseCase);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(potion);
            Object.DestroyImmediate(material);
            Object.DestroyImmediate(itemDatabase);
        }

        [Test]
        public void Purchase_Success_SpendsMoneyAndAddsItem()
        {
            var product = new ShopProductEntity(potion, ItemRarity.N, 100);

            var result = purchaseUseCase.Execute(product, 3);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(700u, wallet.CurrentMoney);
            Assert.AreEqual(3u, repository.GetCurrent().ItemSlots[0].Item.Value.Quantity);
        }

        [Test]
        public void Purchase_InsufficientMoney_ChangesNothing()
        {
            var product = new ShopProductEntity(potion, ItemRarity.N, 400);

            var result = purchaseUseCase.Execute(product, 3);

            Assert.AreEqual(ShopTransactionError.InsufficientMoney, result.Error);
            Assert.AreEqual(1000u, wallet.CurrentMoney);
            Assert.IsFalse(repository.GetCurrent().ItemSlots[0].Item.HasValue);
        }

        [Test]
        public void Purchase_InventoryFull_ChangesNothing()
        {
            var product = new ShopProductEntity(potion, ItemRarity.N, 10);

            var result = purchaseUseCase.Execute(product, 11);

            Assert.AreEqual(ShopTransactionError.InventoryFull, result.Error);
            Assert.AreEqual(1000u, wallet.CurrentMoney);
        }

        [Test]
        public void Purchase_WhenAddFailsAfterSpending_RefundsMoney()
        {
            inventory.FailAdd = true;
            var product = new ShopProductEntity(potion, ItemRarity.N, 100);

            var result = purchaseUseCase.Execute(product, 1);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(1000u, wallet.CurrentMoney);
        }

        [Test]
        public void GetMaxQuantity_IsLimitedByMoneySpaceAndTransactionCap()
        {
            var product = new ShopProductEntity(potion, ItemRarity.N, 300);

            Assert.AreEqual(3u, purchaseUseCase.GetMaxQuantity(product, 99));
            Assert.AreEqual(2u, purchaseUseCase.GetMaxQuantity(product, 2));
        }

        [Test]
        public void Sell_PartialQuantity_RemovesItemsAndAddsMoney()
        {
            inventory.TryAddItem(InventoryItemSpecFactory.FromItemParameter(potion, ItemRarity.N), 4);
            var target = sellTargetUseCase.Build(repository.GetCurrent(), 0).Value;

            var result = sellUseCase.Execute(target, 3);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(1000u + 30u * 3u, wallet.CurrentMoney);
            Assert.AreEqual(1u, repository.GetCurrent().ItemSlots[0].Item.Value.Quantity);
        }

        [Test]
        public void Sell_WhenSlotChangedAfterSelection_Fails()
        {
            inventory.TryAddItem(InventoryItemSpecFactory.FromItemParameter(potion, ItemRarity.N), 2);
            var target = sellTargetUseCase.Build(repository.GetCurrent(), 0).Value;
            inventory.TryRemoveItemAt(0);
            inventory.TryAddItem(InventoryItemSpecFactory.FromItemParameter(material, ItemRarity.N), 2);

            var result = sellUseCase.Execute(target, 1);

            Assert.AreEqual(ShopTransactionError.TargetChanged, result.Error);
            Assert.AreEqual(1000u, wallet.CurrentMoney);
        }

        [Test]
        public void Sell_ZeroBasePrice_SellsForMinimumPrice()
        {
            inventory.TryAddItem(InventoryItemSpecFactory.FromItemParameter(material, ItemRarity.N), 2);
            var target = sellTargetUseCase.Build(repository.GetCurrent(), 0).Value;

            var result = sellUseCase.Execute(target, 2);

            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2u * ItemSellPriceCalculatorUseCase.MinimumSellPrice, result.TotalPrice);
        }

        [Test]
        public void Sell_QuantityOverStack_Fails()
        {
            inventory.TryAddItem(InventoryItemSpecFactory.FromItemParameter(potion, ItemRarity.N), 2);
            var target = sellTargetUseCase.Build(repository.GetCurrent(), 0).Value;

            Assert.AreEqual(ShopTransactionError.InvalidQuantity, sellUseCase.Execute(target, 3).Error);
        }

        [Test]
        public void Sell_NotSellableItem_Fails()
        {
            SetField(potion, "isSellable", false);
            inventory.TryAddItem(InventoryItemSpecFactory.FromItemParameter(potion, ItemRarity.N), 1);
            var target = sellTargetUseCase.Build(repository.GetCurrent(), 0).Value;

            Assert.AreEqual(ShopTransactionError.NotSellable, sellUseCase.Execute(target, 1).Error);
        }

        private static ItemParameter CreateItem(
            string itemId,
            ItemCategory category,
            int maxStackSize,
            uint purchasePrice,
            uint baseSellPrice,
            bool isSellable)
        {
            var itemParameter = ScriptableObject.CreateInstance<ItemParameter>();
            SetField(itemParameter, "itemId", itemId);
            SetField(itemParameter, "itemName", itemId);
            SetField(itemParameter, "category", category);
            SetField(itemParameter, "maxStackSize", maxStackSize);
            SetField(itemParameter, "purchasePrice", purchasePrice);
            SetField(itemParameter, "baseSellPrice", baseSellPrice);
            SetField(itemParameter, "isSellable", isSellable);
            return itemParameter;
        }

        private static void SetField(object target, string fieldName, object value)
        {
            target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance).SetValue(target, value);
        }

        private class FakeWallet : IShopWallet
        {
            public uint CurrentMoney { get; private set; }

            public FakeWallet(uint initialMoney)
            {
                CurrentMoney = initialMoney;
            }

            public void AddMoney(uint amount)
            {
                CurrentMoney += amount;
            }

            public bool TrySpendMoney(uint amount)
            {
                if (amount > CurrentMoney)
                {
                    return false;
                }

                CurrentMoney -= amount;
                return true;
            }
        }

        private class FakeInventory : IInventory
        {
            private readonly InventoryRepository inventoryRepository;
            private readonly InventoryMutationUseCase mutationUseCase;

            public bool FailAdd { get; set; }

            public FakeInventory(InventoryRepository inventoryRepository)
            {
                this.inventoryRepository = inventoryRepository;
                mutationUseCase = new InventoryMutationUseCase(inventoryRepository, null);
            }

            public InventoryUseCaseResult TryAddWeapon(WeaponData weaponData)
            {
                return mutationUseCase.TryAddWeapon(weaponData);
            }

            public InventoryUseCaseResult TryAddItem(InventoryItemSpec itemSpec, uint quantity)
            {
                if (FailAdd)
                {
                    return InventoryUseCaseResult.Failed(InventoryUseCaseError.InventoryFull, "inventory.error.full");
                }

                return mutationUseCase.TryAddItem(itemSpec, quantity);
            }

            public uint GetAddableQuantity(InventoryItemSpec itemSpec)
            {
                return mutationUseCase.GetAddableQuantity(itemSpec);
            }

            public InventoryUseCaseResult TryConsumeWeapon(int itemSlotIndex)
            {
                return mutationUseCase.TryConsumeWeapon(itemSlotIndex);
            }

            public InventoryUseCaseResult TryRemoveItemAt(int itemSlotIndex)
            {
                return mutationUseCase.TryRemoveItemAt(itemSlotIndex);
            }

            public InventoryUseCaseResult TryRemoveItemAt(int itemSlotIndex, uint quantity)
            {
                return mutationUseCase.TryRemoveItemAt(itemSlotIndex, quantity);
            }

            public InventoryEntity GetCurrentInventory()
            {
                return inventoryRepository.GetCurrent();
            }

            public int GetActiveItemSlotIndex()
            {
                return inventoryRepository.GetCurrent().ActiveSlotIndex;
            }
        }
    }
}
