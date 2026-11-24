using System.Collections.Generic;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class IngredientTransferTests
    {
        private readonly List<Object> _created = new List<Object>();

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
        public void MoveAll_CropsAndIngredients_AreMoved()
        {
            var wheat = Item("crop_wheat", ItemCategory.Crop);
            var flour = Item("ingredient_flour", ItemCategory.Ingredient);
            var from = new ItemInventory(4);
            var to = new ItemInventory(4);
            from.Add(wheat, 3);
            from.Add(flour, 2);

            var moved = IngredientTransfer.MoveAll(from, to);

            Assert.AreEqual(5, moved);
            Assert.AreEqual(3, to.Count(wheat));
            Assert.AreEqual(2, to.Count(flour));
            Assert.AreEqual(0, from.Count(wheat));
        }

        [TestCase(ItemCategory.Tool)]
        [TestCase(ItemCategory.Seed)]
        [TestCase(ItemCategory.Dish)]
        public void MoveAll_OtherCategories_StayInPlace(ItemCategory category)
        {
            var item = Item("other_item", category);
            var from = new ItemInventory(2);
            var to = new ItemInventory(2);
            from.Add(item, 1);

            Assert.AreEqual(0, IngredientTransfer.MoveAll(from, to));

            Assert.AreEqual(1, from.Count(item));
        }

        [Test]
        public void MoveAll_Everything_MovesDishesAndToolsToo()
        {
            var cake = Item("dish_cake", ItemCategory.Dish);
            var hoe = Item("tool_hoe", ItemCategory.Tool);
            var from = new ItemInventory(2);
            var to = new ItemInventory(2);
            from.Add(cake, 2);
            from.Add(hoe, 1);

            Assert.AreEqual(3, IngredientTransfer.MoveAll(from, to, true));

            Assert.AreEqual((2, 1), (to.Count(cake), to.Count(hoe)));
        }

        [Test]
        public void MoveAll_TargetFull_KeepsTheRest()
        {
            var wheat = Item("crop_wheat", ItemCategory.Crop, 5);
            var stone = Item("other_item", ItemCategory.Tool, 5);
            var from = new ItemInventory(2);
            var to = new ItemInventory(2);
            from.Add(wheat, 5);
            to.Add(stone, 5);
            to.Add(wheat, 3);

            var moved = IngredientTransfer.MoveAll(from, to);

            Assert.AreEqual(2, moved);
            Assert.AreEqual(3, from.Count(wheat));
            Assert.AreEqual(5, to.Count(wheat));
        }

        [Test]
        public void MoveAll_EmptyBag_MovesNothing()
        {
            Assert.AreEqual(0, IngredientTransfer.MoveAll(new ItemInventory(2), new ItemInventory(2)));
        }

        [Test]
        public void MoveAll_NullOrSameInventory_MovesNothing()
        {
            var wheat = Item("crop_wheat", ItemCategory.Crop);
            var inventory = new ItemInventory(2);
            inventory.Add(wheat, 1);

            Assert.AreEqual(0, IngredientTransfer.MoveAll(null, inventory));
            Assert.AreEqual(0, IngredientTransfer.MoveAll(inventory, null));
            Assert.AreEqual(0, IngredientTransfer.MoveAll(inventory, inventory));
            Assert.AreEqual(1, inventory.Count(wheat));
        }

        private ItemData Item(string id, ItemCategory category, int maxStack = 99)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            _created.Add(item);
            var serialized = new SerializedObject(item);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("category").enumValueIndex = (int)category;
            serialized.FindProperty("maxStack").intValue = maxStack;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }
    }
}
