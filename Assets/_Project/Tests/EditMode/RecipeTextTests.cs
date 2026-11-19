using System;
using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.EditMode
{
    public class RecipeTextTests
    {
        private ItemData _flour;
        private ItemData _berry;
        private RecipeData _cake;

        [SetUp]
        public void SetUp()
        {
            _flour = Named("Flour");
            _berry = Named("Strawberry");
            _cake = ScriptableObject.CreateInstance<RecipeData>();
            _cake.Init("recipe_strawberry_cake", new[] { new Ingredient(_flour, 2), new Ingredient(_berry, 3) }, null, 2, 45, false);
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_flour);
            Object.DestroyImmediate(_berry);
            Object.DestroyImmediate(_cake);
        }

        [Test]
        public void Stars_Count_RepeatsTheStar()
        {
            Assert.AreEqual("★★", RecipeText.Stars(2, "★"));
        }

        [Test]
        public void Stars_ZeroOrNegative_IsEmpty()
        {
            Assert.AreEqual(string.Empty, RecipeText.Stars(0, "★"));
            Assert.AreEqual(string.Empty, RecipeText.Stars(-3, "★"));
        }

        [Test]
        public void Ingredients_SomeMissing_ShowsHaveOutOfNeed()
        {
            var text = RecipeText.Ingredients(_cake, item => item == _flour ? 1 : 7, "{0} {1}/{2}");

            Assert.AreEqual("Flour 1/2, Strawberry 3/3", text);
        }

        [Test]
        public void Ingredients_NullArguments_Throw()
        {
            Assert.Throws<ArgumentNullException>(() => RecipeText.Ingredients(null, item => 0, "{0}"));
            Assert.Throws<ArgumentNullException>(() => RecipeText.Ingredients(_cake, null, "{0}"));
        }

        private static ItemData Named(string displayName)
        {
            var item = ScriptableObject.CreateInstance<ItemData>();
            var serialized = new SerializedObject(item);
            serialized.FindProperty("displayName").stringValue = displayName;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            return item;
        }
    }
}
