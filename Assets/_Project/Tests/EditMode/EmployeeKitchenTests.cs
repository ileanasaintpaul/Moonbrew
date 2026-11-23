using System.Collections.Generic;
using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class EmployeeKitchenTests
    {
        private readonly List<Object> _created = new List<Object>();
        private readonly Dictionary<string, ItemData> _items = new Dictionary<string, ItemData>();
        private ItemData _flour;
        private ItemData _cake;
        private ItemData _slice;
        private ItemData _juice;
        private ItemData _berry;
        private RecipeData _cakeRecipe;
        private RecipeData _juiceRecipe;
        private RecipeData _hardRecipe;
        private ItemInventory _fridge;
        private ItemInventory _display;

        [SetUp]
        public void SetUp()
        {
            _flour = Item("crop_flour");
            _berry = Item("crop_berry");
            _cake = Item("dish_cake");
            _slice = Item("dish_cake_slice");
            _juice = Item("dish_juice");
            _cakeRecipe = Recipe("recipe_cake", _flour, _cake, 1);
            _cakeRecipe.InitPortions(_slice, 6);
            _juiceRecipe = Recipe("recipe_juice", _berry, _juice, 1);
            _hardRecipe = Recipe("recipe_hard", _berry, Item("dish_hard"), 2);
            _fridge = new ItemInventory(4);
            _display = new ItemInventory(2);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                Object.DestroyImmediate(created);
            }

            _created.Clear();
            _items.Clear();
        }

        [Test]
        public void Prepare_DishInDisplay_TakesItFirst()
        {
            _display.Add(_slice, 2);
            _fridge.Add(_slice, 1);

            Assert.IsTrue(Prepare("dish_cake_slice"));

            Assert.AreEqual(1, _display.Count(_slice));
            Assert.AreEqual(1, _fridge.Count(_slice));
        }

        [Test]
        public void Prepare_DishInFridge_TakesIt()
        {
            _fridge.Add(_juice, 1);

            Assert.IsTrue(Prepare("dish_juice"));

            Assert.AreEqual(0, _fridge.Count(_juice));
        }

        [Test]
        public void Prepare_OnlyIngredients_CooksTheDish()
        {
            _fridge.Add(_berry, 2);

            Assert.IsTrue(Prepare("dish_juice"));

            Assert.AreEqual(1, _fridge.Count(_berry));
            Assert.AreEqual(0, _fridge.Count(_juice));
        }

        [Test]
        public void Prepare_CookedCake_PutsTheOtherPortionsInTheDisplay()
        {
            _fridge.Add(_flour, 1);

            Assert.IsTrue(Prepare("dish_cake_slice"));

            Assert.AreEqual(0, _fridge.Count(_flour));
            Assert.AreEqual(5, _display.Count(_slice));
        }

        [Test]
        public void Prepare_OneCakeForTwoCustomers_CooksOnce()
        {
            _fridge.Add(_flour, 1);

            Assert.IsTrue(Prepare("dish_cake_slice", "dish_cake_slice"));

            Assert.AreEqual(4, _display.Count(_slice));
        }

        [Test]
        public void Prepare_OneDishMissing_RemovesNothing()
        {
            _fridge.Add(_juice, 1);

            Assert.IsFalse(Prepare("dish_juice", "dish_juice"));

            Assert.AreEqual(1, _fridge.Count(_juice));
        }

        [Test]
        public void Prepare_TooManyStars_IsRefusedEvenWithIngredients()
        {
            _fridge.Add(_berry, 1);

            Assert.IsFalse(Prepare("dish_hard"));

            Assert.AreEqual(1, _fridge.Count(_berry));
        }

        [Test]
        public void Prepare_UnknownDish_IsRefused()
        {
            Assert.IsFalse(Prepare("dish_unknown"));
        }

        [Test]
        public void Prepare_NoRoomForTheOtherPortions_RemovesNothing()
        {
            var full = new ItemInventory(1);
            full.Add(_juice, 1);
            _fridge.Add(_flour, 1);

            var prepared = EmployeeKitchen.Prepare(new[] { "dish_cake_slice" }, 1, _fridge, full, Recipes(), Find);

            Assert.IsFalse(prepared);
            Assert.AreEqual(1, _fridge.Count(_flour));
        }

        [Test]
        public void Prepare_EmptyOrNull_IsRefused()
        {
            Assert.IsFalse(EmployeeKitchen.Prepare(new string[0], 1, _fridge, _display, Recipes(), Find));
            Assert.IsFalse(EmployeeKitchen.Prepare(null, 1, _fridge, _display, Recipes(), Find));
            Assert.IsFalse(EmployeeKitchen.Prepare(new[] { "dish_juice" }, 1, null, _display, Recipes(), Find));
        }

        private bool Prepare(params string[] dishIds)
        {
            return EmployeeKitchen.Prepare(dishIds, 1, _fridge, _display, Recipes(), Find);
        }

        private RecipeData[] Recipes() => new[] { _cakeRecipe, _juiceRecipe, _hardRecipe };

        private ItemData Find(string id) => _items.TryGetValue(id, out var item) ? item : null;

        private RecipeData Recipe(string id, ItemData ingredient, ItemData result, int stars)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            _created.Add(recipe);
            recipe.Init(id, new[] { new Ingredient(ingredient, 1) }, result, stars, 30, true);
            return recipe;
        }

        private ItemData Item(string id)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            _created.Add(item);
            var serialized = new SerializedObject(item);
            serialized.FindProperty("id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            _items[id] = item;
            return item;
        }
    }
}
