using System;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.EditMode
{
    public class ItemInventoryTests
    {
        private readonly List<Object> _created = new List<Object>();
        private ItemData _wheat;
        private ItemData _flour;

        [SetUp]
        public void SetUp()
        {
            _wheat = CreateItem("crop_wheat", "Wheat", 10);
            _flour = CreateItem("ingredient_flour", "Flour", 5);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
        }

        [Test]
        public void Constructor_ZeroSlots_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemInventory(0));
        }

        [Test]
        public void Constructor_NegativeSlots_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new ItemInventory(-3));
        }

        [Test]
        public void Add_FitsInOneStack_AddsAllToFirstSlot()
        {
            var inventory = new ItemInventory(3);

            var added = inventory.Add(_wheat, 4);

            Assert.AreEqual(4, added);
            Assert.AreEqual(4, inventory.Count(_wheat));
            Assert.AreEqual(_wheat, inventory.Slots[0].Item);
            Assert.IsTrue(inventory.Slots[1].IsEmpty);
        }

        [Test]
        public void Add_MoreThanMaxStack_SpillsIntoNextSlot()
        {
            var inventory = new ItemInventory(3);

            inventory.Add(_wheat, 14);

            Assert.AreEqual(10, inventory.Slots[0].Quantity);
            Assert.AreEqual(4, inventory.Slots[1].Quantity);
        }

        [Test]
        public void Add_SameItemTwice_FillsThePartialStackFirst()
        {
            var inventory = new ItemInventory(3);
            inventory.Add(_wheat, 6);
            inventory.Add(_flour, 1);

            inventory.Add(_wheat, 3);

            Assert.AreEqual(9, inventory.Slots[0].Quantity);
            Assert.AreEqual(_flour, inventory.Slots[1].Item);
            Assert.IsTrue(inventory.Slots[2].IsEmpty);
        }

        [Test]
        public void Add_InventoryFull_ReturnsZeroAndKeepsContents()
        {
            var inventory = new ItemInventory(1);
            inventory.Add(_wheat, 10);

            var added = inventory.Add(_flour, 1);

            Assert.AreEqual(0, added);
            Assert.AreEqual(0, inventory.Count(_flour));
            Assert.AreEqual(10, inventory.Count(_wheat));
        }

        [Test]
        public void Add_OnlyPartFits_AddsWhatFitsAndReturnsIt()
        {
            var inventory = new ItemInventory(2);
            inventory.Add(_flour, 3);

            var added = inventory.Add(_flour, 20);

            Assert.AreEqual(7, added);
            Assert.AreEqual(10, inventory.Count(_flour));
        }

        [Test]
        public void Add_ZeroQuantity_ReturnsZeroWithoutEvent()
        {
            var inventory = new ItemInventory(2);
            var raised = 0;
            inventory.Changed += (item, total) => raised++;

            Assert.AreEqual(0, inventory.Add(_wheat, 0));
            Assert.AreEqual(0, raised);
        }

        [Test]
        public void Add_NegativeQuantity_ReturnsZero()
        {
            var inventory = new ItemInventory(2);

            Assert.AreEqual(0, inventory.Add(_wheat, -5));
            Assert.AreEqual(0, inventory.Count(_wheat));
        }

        [Test]
        public void Add_NullItem_Throws()
        {
            var inventory = new ItemInventory(2);

            Assert.Throws<ArgumentNullException>(() => inventory.Add(null, 1));
        }

        [Test]
        public void Add_Success_RaisesChangedWithNewTotal()
        {
            var inventory = new ItemInventory(2);
            inventory.Add(_wheat, 2);
            ItemData changedItem = null;
            var changedTotal = -1;
            inventory.Changed += (item, total) =>
            {
                changedItem = item;
                changedTotal = total;
            };

            inventory.Add(_wheat, 3);

            Assert.AreEqual(_wheat, changedItem);
            Assert.AreEqual(5, changedTotal);
        }

        [Test]
        public void Remove_EnoughItems_RemovesAndRaisesChanged()
        {
            var inventory = new ItemInventory(2);
            inventory.Add(_wheat, 8);
            var changedTotal = -1;
            inventory.Changed += (item, total) => changedTotal = total;

            var removed = inventory.Remove(_wheat, 3);

            Assert.IsTrue(removed);
            Assert.AreEqual(5, inventory.Count(_wheat));
            Assert.AreEqual(5, changedTotal);
        }

        [Test]
        public void Remove_AcrossStacks_EmptiesTheLastSlotFirst()
        {
            var inventory = new ItemInventory(3);
            inventory.Add(_wheat, 14);

            inventory.Remove(_wheat, 6);

            Assert.AreEqual(8, inventory.Slots[0].Quantity);
            Assert.IsTrue(inventory.Slots[1].IsEmpty);
        }

        [Test]
        public void Remove_AllOfAnItem_FreesTheSlot()
        {
            var inventory = new ItemInventory(1);
            inventory.Add(_wheat, 4);

            inventory.Remove(_wheat, 4);

            Assert.IsTrue(inventory.Slots[0].IsEmpty);
            Assert.AreEqual(5, inventory.SpaceFor(_flour), "The freed slot should take another item.");
        }

        [Test]
        public void Remove_NotEnough_ReturnsFalseAndKeepsEverything()
        {
            var inventory = new ItemInventory(2);
            inventory.Add(_wheat, 2);

            Assert.IsFalse(inventory.Remove(_wheat, 3));
            Assert.AreEqual(2, inventory.Count(_wheat));
        }

        [Test]
        public void Remove_MissingItem_ReturnsFalse()
        {
            var inventory = new ItemInventory(2);

            Assert.IsFalse(inventory.Remove(_flour, 1));
        }

        [Test]
        public void Remove_ZeroOrNegative_ReturnsFalse()
        {
            var inventory = new ItemInventory(2);
            inventory.Add(_wheat, 2);

            Assert.IsFalse(inventory.Remove(_wheat, 0));
            Assert.IsFalse(inventory.Remove(_wheat, -1));
            Assert.AreEqual(2, inventory.Count(_wheat));
        }

        [Test]
        public void Remove_NullItem_Throws()
        {
            var inventory = new ItemInventory(2);

            Assert.Throws<ArgumentNullException>(() => inventory.Remove(null, 1));
        }

        [Test]
        public void Count_NullItem_ReturnsZero()
        {
            Assert.AreEqual(0, new ItemInventory(2).Count(null));
        }

        [Test]
        public void Count_EmptyInventory_ReturnsZero()
        {
            Assert.AreEqual(0, new ItemInventory(2).Count(_wheat));
        }

        [Test]
        public void SpaceFor_EmptyInventory_IsSlotsTimesMaxStack()
        {
            Assert.AreEqual(30, new ItemInventory(3).SpaceFor(_wheat));
        }

        [Test]
        public void SpaceFor_PartialStackAndOtherItem_CountsOnlyUsableRoom()
        {
            var inventory = new ItemInventory(3);
            inventory.Add(_wheat, 7);
            inventory.Add(_flour, 1);

            Assert.AreEqual(13, inventory.SpaceFor(_wheat));
        }

        [Test]
        public void SpaceFor_NullItem_ReturnsZero()
        {
            Assert.AreEqual(0, new ItemInventory(2).SpaceFor(null));
        }

        [Test]
        public void ToSaveThenRestore_SameSlots_KeepsItemsAndPositions()
        {
            var inventory = new ItemInventory(4);
            inventory.Add(_wheat, 14);
            inventory.Add(_flour, 2);
            var save = inventory.ToSave("inventory_player");

            var restored = new ItemInventory(4);
            var skipped = restored.Restore(save, Find);

            Assert.AreEqual(0, skipped);
            Assert.AreEqual("inventory_player", save.id);
            for (var i = 0; i < 4; i++)
            {
                Assert.AreEqual(inventory.Slots[i].Item, restored.Slots[i].Item);
                Assert.AreEqual(inventory.Slots[i].Quantity, restored.Slots[i].Quantity);
            }
        }

        [Test]
        public void Restore_ReplacesCurrentContentsAndRaisesChanged()
        {
            var inventory = new ItemInventory(2);
            inventory.Add(_flour, 3);
            var save = new InventorySave { slots = { new SlotSave { index = 1, itemId = "crop_wheat", quantity = 4 } } };
            var changed = new List<ItemData>();
            inventory.Changed += (item, total) => changed.Add(item);

            inventory.Restore(save, Find);

            Assert.AreEqual(0, inventory.Count(_flour));
            Assert.AreEqual(4, inventory.Slots[1].Quantity);
            CollectionAssert.AreEquivalent(new[] { _flour, _wheat }, changed);
        }

        [Test]
        public void Restore_UnknownItemBadIndexOrQuantity_SkipsThoseSlots()
        {
            var inventory = new ItemInventory(2);
            var save = new InventorySave
            {
                slots =
                {
                    new SlotSave { index = 0, itemId = "removed_item", quantity = 1 },
                    new SlotSave { index = 5, itemId = "crop_wheat", quantity = 1 },
                    new SlotSave { index = 1, itemId = "crop_wheat", quantity = 0 },
                    new SlotSave { index = 1, itemId = "crop_wheat", quantity = 3 },
                },
            };

            var skipped = inventory.Restore(save, Find);

            Assert.AreEqual(3, skipped);
            Assert.AreEqual(3, inventory.Count(_wheat));
        }

        [Test]
        public void Restore_MoreThanMaxStack_IsClamped()
        {
            var inventory = new ItemInventory(1);

            inventory.Restore(new InventorySave { slots = { new SlotSave { index = 0, itemId = "ingredient_flour", quantity = 50 } } }, Find);

            Assert.AreEqual(5, inventory.Count(_flour));
        }

        [Test]
        public void Restore_NullSave_EmptiesTheInventory()
        {
            var inventory = new ItemInventory(2);
            inventory.Add(_wheat, 2);

            Assert.AreEqual(0, inventory.Restore(null, Find));
            Assert.AreEqual(0, inventory.Count(_wheat));
        }

        [Test]
        public void Restore_NullLookup_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new ItemInventory(1).Restore(new InventorySave(), null));
        }

        [Test]
        public void Totals_StacksOfTheSameItem_AreSummedById()
        {
            var inventory = new ItemInventory(3);
            inventory.Add(_wheat, 14);
            inventory.Add(_flour, 1);

            var totals = inventory.Totals();

            Assert.AreEqual(14, totals["crop_wheat"]);
            Assert.AreEqual(1, totals["ingredient_flour"]);
            Assert.IsEmpty(new ItemInventory(1).Totals());
        }

        [Test]
        public void Totals_ItemWithoutId_IsLeftOut()
        {
            var inventory = new ItemInventory(2);
            inventory.Add(CreateItem(null, "Sans ID", 10), 3);

            Assert.IsEmpty(inventory.Totals());
        }

        [Test]
        public void MoveSlot_WholeStack_EmptiesTheSlotAndFillsTheTarget()
        {
            var source = new ItemInventory(2);
            var target = new ItemInventory(2);
            source.Add(_wheat, 4);

            var moved = source.MoveSlot(0, target, 4);

            Assert.AreEqual(4, moved);
            Assert.IsTrue(source.Slots[0].IsEmpty);
            Assert.AreEqual(4, target.Count(_wheat));
        }

        [Test]
        public void MoveSlot_OneItem_LeavesTheRest()
        {
            var source = new ItemInventory(2);
            var target = new ItemInventory(2);
            source.Add(_wheat, 4);

            Assert.AreEqual(1, source.MoveSlot(0, target, 1));

            Assert.AreEqual(3, source.Count(_wheat));
            Assert.AreEqual(1, target.Count(_wheat));
        }

        [Test]
        public void MoveSlot_TakesFromTheClickedSlot()
        {
            var source = new ItemInventory(2);
            var target = new ItemInventory(1);
            source.Add(_wheat, 13);

            source.MoveSlot(0, target, 2);

            Assert.AreEqual(8, source.Slots[0].Quantity);
            Assert.AreEqual(3, source.Slots[1].Quantity);
        }

        [Test]
        public void MoveSlot_TargetAlmostFull_MovesWhatFitsAndKeepsTheRest()
        {
            var source = new ItemInventory(1);
            var target = new ItemInventory(1);
            source.Add(_wheat, 5);
            target.Add(_wheat, 8);

            Assert.AreEqual(2, source.MoveSlot(0, target, 5));

            Assert.AreEqual(3, source.Count(_wheat));
            Assert.AreEqual(10, target.Count(_wheat));
        }

        [Test]
        public void MoveSlot_TargetFull_MovesNothing()
        {
            var source = new ItemInventory(1);
            var target = new ItemInventory(1);
            source.Add(_wheat, 5);
            target.Add(_flour, 1);

            Assert.AreEqual(0, source.MoveSlot(0, target, 5));

            Assert.AreEqual(5, source.Count(_wheat));
        }

        [Test]
        public void MoveSlot_InvalidRequest_MovesNothing()
        {
            var source = new ItemInventory(2);
            var target = new ItemInventory(2);
            source.Add(_wheat, 4);

            Assert.AreEqual(0, source.MoveSlot(-1, target, 1));
            Assert.AreEqual(0, source.MoveSlot(2, target, 1));
            Assert.AreEqual(0, source.MoveSlot(1, target, 1), "An empty slot has nothing to move.");
            Assert.AreEqual(0, source.MoveSlot(0, target, 0));
            Assert.AreEqual(0, source.MoveSlot(0, target, -2));
            Assert.AreEqual(0, source.MoveSlot(0, null, 1));
            Assert.AreEqual(0, source.MoveSlot(0, source, 1));
            Assert.AreEqual(4, source.Count(_wheat));
        }

        [Test]
        public void MoveSlot_Moved_AnnouncesBothInventories()
        {
            var source = new ItemInventory(1);
            var target = new ItemInventory(1);
            source.Add(_wheat, 4);
            var sourceTotal = -1;
            var targetTotal = -1;
            source.Changed += (_, total) => sourceTotal = total;
            target.Changed += (_, total) => targetTotal = total;

            source.MoveSlot(0, target, 1);

            Assert.AreEqual(3, sourceTotal);
            Assert.AreEqual(1, targetTotal);
        }

        [Test]
        public void Covers_EverythingHeld_IsAllTrue()
        {
            var inventory = new ItemInventory(4);
            inventory.Add(_wheat, 1);
            inventory.Add(_flour, 1);

            CollectionAssert.AreEqual(new[] { true, true }, inventory.Covers(new[] { _flour.Id, _wheat.Id }));
        }

        [Test]
        public void Covers_SameItemTwiceButOneHeld_CoversOnlyTheFirst()
        {
            var inventory = new ItemInventory(4);
            inventory.Add(_wheat, 1);

            CollectionAssert.AreEqual(new[] { true, false }, inventory.Covers(new[] { _wheat.Id, _wheat.Id }));
        }

        [Test]
        public void Covers_ChangesNothingInTheInventory()
        {
            var inventory = new ItemInventory(4);
            inventory.Add(_wheat, 2);

            inventory.Covers(new[] { _wheat.Id, _wheat.Id });

            Assert.AreEqual(2, inventory.Count(_wheat));
        }

        [Test]
        public void Covers_EmptyInventoryOrUnknownId_IsFalse()
        {
            var inventory = new ItemInventory(4);

            CollectionAssert.AreEqual(new[] { false, false, false }, inventory.Covers(new[] { _wheat.Id, "unknown", null }));
        }

        [Test]
        public void Covers_NullOrEmptyList_ReturnsEmpty()
        {
            var inventory = new ItemInventory(4);

            Assert.IsEmpty(inventory.Covers(null));
            Assert.IsEmpty(inventory.Covers(new string[0]));
        }

        private ItemData Find(string id)
        {
            return id == _wheat.Id ? _wheat : id == _flour.Id ? _flour : null;
        }

        private ItemData CreateItem(string id, string displayName, int maxStack)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            var serialized = new SerializedObject(item);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.FindProperty("maxStack").intValue = maxStack;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _created.Add(item);
            return item;
        }
    }
}
