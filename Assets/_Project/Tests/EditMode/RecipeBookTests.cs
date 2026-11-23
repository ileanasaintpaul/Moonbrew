using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class RecipeBookTests
    {
        private ItemData _flour;
        private ItemData _berry;
        private RecipeData _wheatCake;
        private RecipeData _berryCake;

        [SetUp]
        public void SetUp()
        {
            _flour = ScriptableObject.CreateInstance<ItemData>();
            _berry = ScriptableObject.CreateInstance<ItemData>();
            _wheatCake = ScriptableObject.CreateInstance<RecipeData>();
            _wheatCake.Init("recipe_wheat_cake", new[] { new Ingredient(_flour, 2) }, null, 1, 30, true);
            _berryCake = ScriptableObject.CreateInstance<RecipeData>();
            _berryCake.Init("recipe_strawberry_cake", new[] { new Ingredient(_flour, 2), new Ingredient(_berry, 3) }, null, 2, 45, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_flour);
            Object.DestroyImmediate(_berry);
            Object.DestroyImmediate(_wheatCake);
            Object.DestroyImmediate(_berryCake);
        }

        [Test]
        public void FillMenu_KnownRecipes_AddsTheirDishAndIngredients()
        {
            var menu = new List<MenuDish>();
            var known = Recipe("recipe_known", "dish_known", true);
            var unknown = Recipe("recipe_unknown", "dish_unknown", false);

            new RecipeBook().FillMenu(new[] { known, unknown }, menu);

            Assert.AreEqual(1, menu.Count);
            Assert.AreEqual("dish_known", menu[0].DishId);
            CollectionAssert.AreEqual(new[] { "crop_flour" }, menu[0].IngredientIds);
            DestroyWithItems(known);
            DestroyWithItems(unknown);
        }

        [Test]
        public void FillMenu_LearnedRecipe_IsOnTheMenu()
        {
            var menu = new List<MenuDish>();
            var book = new RecipeBook();
            var learned = Recipe("recipe_learned", "dish_learned", false);
            book.Learn(learned);

            book.FillMenu(new[] { learned }, menu);

            Assert.AreEqual("dish_learned", menu[0].DishId);
            DestroyWithItems(learned);
        }

        [Test]
        public void FillMenu_NullOrNoResult_AddsNothing()
        {
            var menu = new List<MenuDish>();
            var book = new RecipeBook();

            book.FillMenu(null, menu);
            book.FillMenu(new RecipeData[] { null, _wheatCake }, menu);
            book.FillMenu(new[] { _wheatCake }, null);

            Assert.IsEmpty(menu, "A recipe without a result is not a dish.");
        }

        [Test]
        public void FillMenu_TwoRecipesSameDish_ListsItOnce()
        {
            var menu = new List<MenuDish>();
            var first = Recipe("recipe_first", "dish_same", true);
            var second = ScriptableObject.CreateInstance<RecipeData>();
            second.Init("recipe_second", new Ingredient[0], first.Result, 1, 30, true);

            new RecipeBook().FillMenu(new[] { first, second }, menu);

            Assert.AreEqual(1, menu.Count);
            Object.DestroyImmediate(second);
            DestroyWithItems(first);
        }

        [Test]
        public void FillMenu_MaxStars_LeavesHarderRecipesOut()
        {
            var easy = ScriptableObject.CreateInstance<RecipeData>();
            easy.Init("recipe_easy", new Ingredient[0], Item("dish_easy"), 1, 30, true);
            var hard = ScriptableObject.CreateInstance<RecipeData>();
            hard.Init("recipe_hard", new Ingredient[0], Item("dish_hard"), 2, 30, true);
            var menu = new List<MenuDish>();

            new RecipeBook().FillMenu(new[] { easy, hard }, menu, 1);

            Assert.AreEqual(1, menu.Count);
            Assert.AreEqual("dish_easy", menu[0].DishId);
        }

        [Test]
        public void FillMenu_ZeroStars_AddsNothing()
        {
            var easy = ScriptableObject.CreateInstance<RecipeData>();
            easy.Init("recipe_easy", new Ingredient[0], Item("dish_easy"), 1, 30, true);
            var menu = new List<MenuDish>();

            new RecipeBook().FillMenu(new[] { easy }, menu, 0);

            Assert.IsEmpty(menu);
        }

        private static RecipeData Recipe(string id, string dishId, bool known)
        {
            var recipe = ScriptableObject.CreateInstance<RecipeData>();
            recipe.Init(id, new[] { new Ingredient(Item("crop_flour"), 1) }, Item(dishId), 1, 30, known);
            return recipe;
        }

        private static ItemData Item(string id)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            var serialized = new SerializedObject(item);
            serialized.FindProperty("id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }

        private static void DestroyWithItems(RecipeData recipe)
        {
            Object.DestroyImmediate(recipe.Ingredients[0].Item);
            Object.DestroyImmediate(recipe.Result);
            Object.DestroyImmediate(recipe);
        }

        [Test]
        public void IsKnown_StartingRecipe_IsKnown()
        {
            Assert.IsTrue(new RecipeBook().IsKnown(_wheatCake));
        }

        [Test]
        public void IsKnown_RecipeToBuy_IsUnknownUntilLearned()
        {
            var book = new RecipeBook();

            Assert.IsFalse(book.IsKnown(_berryCake));
            Assert.IsTrue(book.Learn(_berryCake));
            Assert.IsTrue(book.IsKnown(_berryCake));
        }

        [Test]
        public void Learn_AlreadyKnownOrNull_ReturnsFalse()
        {
            var book = new RecipeBook();
            book.Learn(_berryCake);

            Assert.IsFalse(book.Learn(_berryCake));
            Assert.IsFalse(book.Learn(_wheatCake), "A starting recipe is known already.");
            Assert.IsFalse(book.Learn(null));
            Assert.IsFalse(book.IsKnown(null));
        }

        [Test]
        public void ToSaveThenRestore_LearnedRecipes_AreKnownAgain()
        {
            var book = new RecipeBook();
            book.Learn(_berryCake);

            var restored = new RecipeBook();
            restored.Restore(book.ToSave());

            Assert.IsTrue(restored.IsKnown(_berryCake));
            CollectionAssert.AreEqual(new[] { "recipe_strawberry_cake" }, book.ToSave(), "Starting recipes are not saved.");
        }

        [Test]
        public void Restore_NullOrEmptyIds_LeavesOnlyStartingRecipes()
        {
            var book = new RecipeBook();
            book.Learn(_berryCake);

            book.Restore(null);
            Assert.IsFalse(book.IsKnown(_berryCake));
            book.Restore(new[] { string.Empty, null });
            Assert.IsEmpty(book.ToSave());
        }

        [Test]
        public void HasIngredients_EverythingThere_IsTrue()
        {
            Assert.IsTrue(_berryCake.HasIngredients(item => item == _flour ? 2 : 5));
        }

        [Test]
        public void HasIngredients_OneMissing_IsFalse()
        {
            Assert.IsFalse(_berryCake.HasIngredients(item => item == _flour ? 2 : 2));
            Assert.IsFalse(_berryCake.HasIngredients(item => 0));
        }

        [Test]
        public void HasIngredients_NullCount_IsFalse()
        {
            Assert.IsFalse(_wheatCake.HasIngredients(null));
        }

        [Test]
        public void ToJob_Recipe_CarriesIdOutputAndTime()
        {
            var job = _berryCake.ToJob();

            Assert.AreEqual(("recipe_strawberry_cake", 1, 45), (job.Id, job.OutputPerBatch, job.Minutes));
        }
    }
}
