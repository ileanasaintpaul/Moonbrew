using System;
using System.Collections.Generic;
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
            _wheat = CreateItem("crop_wheat", "Blé", 10);
            _flour = CreateItem("ingredient_flour", "Farine", 5);
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
