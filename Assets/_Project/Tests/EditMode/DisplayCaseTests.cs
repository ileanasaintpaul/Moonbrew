using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class DisplayCaseTests
    {
        private ItemData _cake;
        private ItemData _slice;
        private ItemData _drink;
        private RecipeData _cakeRecipe;
        private RecipeData _drinkRecipe;
        private RecipeData[] _recipes;
        private ItemInventory _owner;
        private ItemInventory _display;

        [SetUp]
        public void SetUp()
        {
            _cake = ScriptableObject.CreateInstance<ItemData>();
            _slice = ScriptableObject.CreateInstance<ItemData>();
            _drink = ScriptableObject.CreateInstance<ItemData>();
            _cakeRecipe = ScriptableObject.CreateInstance<RecipeData>();
            _cakeRecipe.Init("recipe_cake", new Ingredient[0], _cake, 1, 30, true);
            _cakeRecipe.InitPortions(_slice, 6);
            _drinkRecipe = ScriptableObject.CreateInstance<RecipeData>();
            _drinkRecipe.Init("recipe_drink", new Ingredient[0], _drink, 1, 20, true);
            _recipes = new[] { _cakeRecipe, _drinkRecipe };
            _owner = new ItemInventory(10);
            _display = new ItemInventory(5);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_cake);
            Object.DestroyImmediate(_slice);
            Object.DestroyImmediate(_drink);
            Object.DestroyImmediate(_cakeRecipe);
            Object.DestroyImmediate(_drinkRecipe);
        }

        [Test]
        public void Place_Cake_BecomesItsPortions()
        {
            _owner.Add(_cake, 1);

            var placed = DisplayCase.Place(_owner, 0, _display, _recipes);

            Assert.IsTrue(placed);
            Assert.AreEqual(0, _owner.Count(_cake));
            Assert.AreEqual(6, _display.Count(_slice));
        }

        [Test]
        public void Place_PortionsSetInTheRecipe_GivesThatMany()
        {
            _cakeRecipe.InitPortions(_slice, 8);
            _owner.Add(_cake, 1);

            DisplayCase.Place(_owner, 0, _display, _recipes);

            Assert.AreEqual(8, _display.Count(_slice));
        }

        [Test]
        public void Place_TwoCakesInOneSlot_PlacesOnlyOne()
        {
            _owner.Add(_cake, 2);

            DisplayCase.Place(_owner, 0, _display, _recipes);

            Assert.AreEqual(1, _owner.Count(_cake));
            Assert.AreEqual(6, _display.Count(_slice));
        }

        [Test]
        public void Place_Drink_IsRefused()
        {
            _owner.Add(_drink, 1);

            Assert.IsFalse(DisplayCase.Place(_owner, 0, _display, _recipes));
            Assert.AreEqual(1, _owner.Count(_drink));
        }

        [Test]
        public void Place_NoRoomForEveryPortion_KeepsTheCake()
        {
            var full = new ItemInventory(1);
            full.Add(_drink, 1);
            _owner.Add(_cake, 1);

            Assert.IsFalse(DisplayCase.Place(_owner, 0, full, _recipes));
            Assert.AreEqual(1, _owner.Count(_cake));
            Assert.AreEqual(0, full.Count(_slice));
        }

        [TestCase(-1)]
        [TestCase(1)]
        [TestCase(10)]
        public void Place_EmptyOrInvalidSlot_IsRefused(int slot)
        {
            _owner.Add(_cake, 1);

            Assert.IsFalse(DisplayCase.Place(_owner, slot, _display, _recipes));
            Assert.AreEqual(1, _owner.Count(_cake));
        }

        [Test]
        public void Place_NullArguments_AreRefused()
        {
            _owner.Add(_cake, 1);

            Assert.IsFalse(DisplayCase.Place(null, 0, _display, _recipes));
            Assert.IsFalse(DisplayCase.Place(_owner, 0, null, _recipes));
            Assert.IsFalse(DisplayCase.Place(_owner, 0, _display, null));
            Assert.AreEqual(1, _owner.Count(_cake));
        }

        [Test]
        public void Take_SixTimes_ServesEveryPortionThenNothing()
        {
            _owner.Add(_cake, 1);
            DisplayCase.Place(_owner, 0, _display, _recipes);

            for (var i = 1; i <= 6; i++)
            {
                Assert.IsTrue(DisplayCase.Take(_display, 0, _owner));
                Assert.AreEqual(i, _owner.Count(_slice));
                Assert.AreEqual(6 - i, _display.Count(_slice));
            }

            Assert.IsFalse(DisplayCase.Take(_display, 0, _owner));
            Assert.AreEqual(6, _owner.Count(_slice));
        }

        [Test]
        public void Take_OwnerFull_LeavesThePortion()
        {
            var full = new ItemInventory(1);
            full.Add(_drink, 1);
            _display.Add(_slice, 6);

            Assert.IsFalse(DisplayCase.Take(_display, 0, full));
            Assert.AreEqual(6, _display.Count(_slice));
        }

        [TestCase(-1)]
        [TestCase(5)]
        public void Take_InvalidSlot_IsRefused(int slot)
        {
            _display.Add(_slice, 6);

            Assert.IsFalse(DisplayCase.Take(_display, slot, _owner));
            Assert.AreEqual(6, _display.Count(_slice));
        }

        [Test]
        public void Take_NullInventories_AreRefused()
        {
            _display.Add(_slice, 6);

            Assert.IsFalse(DisplayCase.Take(null, 0, _owner));
            Assert.IsFalse(DisplayCase.Take(_display, 0, null));
            Assert.AreEqual(6, _display.Count(_slice));
        }

        [Test]
        public void ServedItem_Cake_IsItsPortion()
        {
            Assert.AreEqual(_slice, _cakeRecipe.ServedItem);
        }

        [Test]
        public void ServedItem_Drink_IsTheDrink()
        {
            Assert.AreEqual(_drink, _drinkRecipe.ServedItem);
        }
    }
}
