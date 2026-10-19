using Game.Inventory;
using Game.Inventory.UseCase;
using Game.Item;
using NUnit.Framework;

namespace Game.Tests.Inventory
{
    public class InventoryMutationUseCaseTests
    {
        private InventoryRepository repository;
        private InventoryMutationUseCase mutationUseCase;

        [SetUp]
        public void SetUp()
        {
            repository = new InventoryRepository(3);
            mutationUseCase = new InventoryMutationUseCase(repository, null);
        }

        private static InventoryItemSpec CreateSpec(string itemId, InventoryItemType itemType, int maxStackSize)
        {
            return new InventoryItemSpec(itemId, itemId, itemType, maxStackSize, null);
        }

        [Test]
        public void TryAddItem_StacksIntoExistingSlotUpToMaxStack()
        {
            var spec = CreateSpec("potion", InventoryItemType.Recovery, 3);

            Assert.IsTrue(mutationUseCase.TryAddItem(spec, 2).IsSuccess);
            Assert.IsTrue(mutationUseCase.TryAddItem(spec, 1).IsSuccess);

            var slots = repository.GetCurrent().ItemSlots;
            Assert.AreEqual(3u, slots[0].Item.Value.Quantity);
            Assert.IsFalse(slots[1].Item.HasValue);
        }

        [Test]
        public void TryAddItem_OverflowGoesToNextEmptySlot()
        {
            var spec = CreateSpec("material", InventoryItemType.SellMaterial, 5);

            Assert.IsTrue(mutationUseCase.TryAddItem(spec, 7).IsSuccess);

            var slots = repository.GetCurrent().ItemSlots;
            Assert.AreEqual(5u, slots[0].Item.Value.Quantity);
            Assert.AreEqual(2u, slots[1].Item.Value.Quantity);
        }

        [Test]
        public void TryAddItem_WhenNotAllFit_FailsWithoutChangingInventory()
        {
            var spec = CreateSpec("material", InventoryItemType.SellMaterial, 5);

            var result = mutationUseCase.TryAddItem(spec, 16);

            Assert.IsFalse(result.IsSuccess);
            Assert.AreEqual(InventoryUseCaseError.InventoryFull, result.Error);
            foreach (var slot in repository.GetCurrent().ItemSlots)
            {
                Assert.IsFalse(slot.Item.HasValue);
            }
        }

        [Test]
        public void TryAddItem_WeaponWithQuantityOtherThanOne_Fails()
        {
            var spec = CreateSpec("sword", InventoryItemType.Weapon, 1);

            var result = mutationUseCase.TryAddItem(spec, 2);

            Assert.AreEqual(InventoryUseCaseError.InvalidQuantity, result.Error);
        }

        [Test]
        public void TryAddItem_ZeroQuantity_Fails()
        {
            var spec = CreateSpec("potion", InventoryItemType.Recovery, 3);

            Assert.AreEqual(InventoryUseCaseError.InvalidQuantity, mutationUseCase.TryAddItem(spec, 0).Error);
        }

        [Test]
        public void TryAddItem_DifferentItemIdsDoNotStack()
        {
            mutationUseCase.TryAddItem(CreateSpec("a", InventoryItemType.Other, 5), 1);
            mutationUseCase.TryAddItem(CreateSpec("b", InventoryItemType.Other, 5), 1);

            var slots = repository.GetCurrent().ItemSlots;
            Assert.AreEqual("a", slots[0].Item.Value.ItemId);
            Assert.AreEqual("b", slots[1].Item.Value.ItemId);
        }

        [Test]
        public void TryAddItem_DifferentRaritiesDoNotStack()
        {
            var normalSpec = new InventoryItemSpec("potion", "potion", InventoryItemType.Recovery, 3, null, ItemRarity.N);
            var rareSpec = new InventoryItemSpec("potion", "potion", InventoryItemType.Recovery, 3, null, ItemRarity.R);

            mutationUseCase.TryAddItem(normalSpec, 1);
            mutationUseCase.TryAddItem(rareSpec, 1);
            mutationUseCase.TryAddItem(normalSpec, 1);

            var slots = repository.GetCurrent().ItemSlots;
            Assert.AreEqual(2u, slots[0].Item.Value.Quantity);
            Assert.AreEqual(ItemRarity.N, slots[0].Item.Value.Rarity);
            Assert.AreEqual(1u, slots[1].Item.Value.Quantity);
            Assert.AreEqual(ItemRarity.R, slots[1].Item.Value.Rarity);
        }

        [Test]
        public void TryAddItem_WeaponKeepsRarityAndAttackPower()
        {
            var spec = new InventoryItemSpec("sword", "sword", InventoryItemType.Weapon, 1, null, ItemRarity.SR, 45);

            mutationUseCase.TryAddItem(spec, 1);

            var item = repository.GetCurrent().ItemSlots[0].Item.Value;
            Assert.AreEqual(ItemRarity.SR, item.Rarity);
            Assert.AreEqual(45, item.EffectiveAttackPower);
        }

        [Test]
        public void TryRemoveItemAt_RemovesWholeStack()
        {
            mutationUseCase.TryAddItem(CreateSpec("potion", InventoryItemType.Recovery, 3), 3);

            var result = mutationUseCase.TryRemoveItemAt(0);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(repository.GetCurrent().ItemSlots[0].Item.HasValue);
        }

        [Test]
        public void TryRemoveItemAt_RemovesOnlyRequestedQuantity()
        {
            mutationUseCase.TryAddItem(CreateSpec("potion", InventoryItemType.Recovery, 5), 4);
            var originalItem = repository.GetCurrent().ItemSlots[0].Item.Value;

            var result = mutationUseCase.TryRemoveItemAt(0, 2);

            var remainingItem = repository.GetCurrent().ItemSlots[0].Item.Value;
            Assert.IsTrue(result.IsSuccess);
            Assert.AreEqual(2u, remainingItem.Quantity);
            Assert.AreEqual(originalItem.InstanceId, remainingItem.InstanceId);
            Assert.IsFalse(repository.GetCurrent().ItemSlots[1].Item.HasValue);
        }

        [Test]
        public void TryRemoveItemAt_RemovingEntireQuantityClearsSlot()
        {
            mutationUseCase.TryAddItem(CreateSpec("potion", InventoryItemType.Recovery, 5), 2);

            var result = mutationUseCase.TryRemoveItemAt(0, 2);

            Assert.IsTrue(result.IsSuccess);
            Assert.IsFalse(repository.GetCurrent().ItemSlots[0].Item.HasValue);
        }

        [Test]
        public void TryRemoveItemAt_ZeroOrExcessQuantityFailsWithoutChangingStack()
        {
            mutationUseCase.TryAddItem(CreateSpec("potion", InventoryItemType.Recovery, 5), 2);

            var zeroResult = mutationUseCase.TryRemoveItemAt(0, 0);
            var excessResult = mutationUseCase.TryRemoveItemAt(0, 3);

            Assert.AreEqual(InventoryUseCaseError.InvalidQuantity, zeroResult.Error);
            Assert.AreEqual(InventoryUseCaseError.InsufficientQuantity, excessResult.Error);
            Assert.AreEqual(2u, repository.GetCurrent().ItemSlots[0].Item.Value.Quantity);
        }

        [Test]
        public void GetAddableQuantity_CountsStackSpaceAndEmptySlots()
        {
            var spec = CreateSpec("potion", InventoryItemType.Recovery, 3);
            mutationUseCase.TryAddItem(spec, 2);

            Assert.AreEqual(7u, mutationUseCase.GetAddableQuantity(spec));
            Assert.AreEqual(6u, mutationUseCase.GetAddableQuantity(CreateSpec("other", InventoryItemType.Other, 3)));
            Assert.AreEqual(2u, mutationUseCase.GetAddableQuantity(CreateSpec("sword", InventoryItemType.Weapon, 1)));
        }

        [Test]
        public void TryRemoveItemAt_EmptyOrInvalidSlot_Fails()
        {
            Assert.AreEqual(InventoryUseCaseError.ItemNotFound, mutationUseCase.TryRemoveItemAt(0).Error);
            Assert.AreEqual(InventoryUseCaseError.InvalidSlotIndex, mutationUseCase.TryRemoveItemAt(9).Error);
            Assert.AreEqual(InventoryUseCaseError.InvalidSlotIndex, mutationUseCase.TryRemoveItemAt(9, 1).Error);
        }
    }
}