using Moonbrew.Cafe;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class CustomerDataTests
    {
        private ItemData _flour;
        private ItemData _berry;
        private ItemData _juice;
        private ItemData _wheatCake;
        private ItemData _berryCake;
        private CustomerData _miller;

        [SetUp]
        public void SetUp()
        {
            _flour = ScriptableObject.CreateInstance<ItemData>();
            _berry = ScriptableObject.CreateInstance<ItemData>();
            _juice = ScriptableObject.CreateInstance<ItemData>();
            _wheatCake = ScriptableObject.CreateInstance<ItemData>();
            _berryCake = ScriptableObject.CreateInstance<ItemData>();
            _miller = ScriptableObject.CreateInstance<CustomerData>();
            _miller.Init("villager_basile", CustomerKind.Villager, new[] { _flour }, new[] { _juice });
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_flour);
            Object.DestroyImmediate(_berry);
            Object.DestroyImmediate(_juice);
            Object.DestroyImmediate(_wheatCake);
            Object.DestroyImmediate(_berryCake);
            Object.DestroyImmediate(_miller);
        }

        [Test]
        public void TasteOf_DishWithALovedIngredient_IsLoved()
        {
            Assert.AreEqual(Taste.Loved, _miller.TasteOf(_wheatCake, new[] { _flour }));
            Assert.AreEqual(Taste.Loved, _miller.TasteOf(_berryCake, new[] { _flour, _berry }));
        }

        [Test]
        public void TasteOf_DislikedDish_WinsOverItsIngredients()
        {
            Assert.AreEqual(Taste.Disliked, _miller.TasteOf(_juice, new[] { _flour }));
        }

        [Test]
        public void TasteOf_OneDislikedIngredient_SpoilsTheDish()
        {
            var picky = ScriptableObject.CreateInstance<CustomerData>();
            picky.Init("villager_picky", CustomerKind.Villager, new[] { _flour }, new[] { _berry });

            Assert.AreEqual(Taste.Disliked, picky.TasteOf(_berryCake, new[] { _flour, _berry }));
            Object.DestroyImmediate(picky);
        }

        [Test]
        public void TasteOf_NothingKnown_IsNeutral()
        {
            Assert.AreEqual(Taste.Neutral, _miller.TasteOf(_berryCake, new[] { _berry }));
        }

        [Test]
        public void TasteOf_NullDishOrIngredients_IsHandled()
        {
            Assert.AreEqual(Taste.Neutral, _miller.TasteOf(null, new[] { _flour }));
            Assert.AreEqual(Taste.Neutral, _miller.TasteOf(_wheatCake, null));
            Assert.AreEqual(Taste.Neutral, _miller.TasteOf(_wheatCake, new ItemData[] { null }));
        }

        [Test]
        public void TasteOf_EmptyIngredients_UsesOnlyTheDish()
        {
            Assert.AreEqual(Taste.Disliked, _miller.TasteOf(_juice, new ItemData[0]));
            Assert.AreEqual(Taste.Neutral, _miller.TasteOf(_wheatCake, new ItemData[0]));
        }

        [Test]
        public void Find_KnownId_ReturnsTheCustomer()
        {
            var catalog = ScriptableObject.CreateInstance<CustomerCatalog>();
            catalog.Init(new[] { _miller });

            Assert.AreSame(_miller, catalog.Find("villager_basile"));
            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void Find_UnknownEmptyOrNullId_ReturnsNull()
        {
            var catalog = ScriptableObject.CreateInstance<CustomerCatalog>();
            catalog.Init(new[] { _miller, null });

            Assert.IsNull(catalog.Find("villager_nobody"));
            Assert.IsNull(catalog.Find(string.Empty));
            Assert.IsNull(catalog.Find(null));
            Object.DestroyImmediate(catalog);
        }
    }
}
