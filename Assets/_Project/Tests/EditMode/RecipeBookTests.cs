using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using NUnit.Framework;
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
