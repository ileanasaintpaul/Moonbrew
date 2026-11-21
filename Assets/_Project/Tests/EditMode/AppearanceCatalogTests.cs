using System;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.EditMode
{
    public class AppearanceCatalogTests
    {
        private readonly List<Object> _created = new List<Object>();
        private AppearanceCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            _catalog = Catalog(new AppearanceSave { bodyId = "body_dress", hairId = "hair_bob", hairColourId = "hair_red", topColourId = "outfit_b" });
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
        public void PartsOf_Category_ListsOnlyThatCategoryInOrder()
        {
            var bodies = _catalog.PartsOf(AppearanceCategory.Body);

            CollectionAssert.AreEqual(new[] { "body_tunic", "body_dress" }, bodies.ConvertAll(part => part.Id));
        }

        [Test]
        public void PartsOf_EmptyCatalog_IsEmpty()
        {
            var empty = Create(ScriptableObject.CreateInstance<AppearanceCatalog>());
            empty.Init(null, null, null, null);

            Assert.IsEmpty(empty.PartsOf(AppearanceCategory.Hair));
        }

        [Test]
        public void FindPart_KnownId_FindsIt()
        {
            Assert.AreEqual("hair_bob", _catalog.FindPart(AppearanceCategory.Hair, "hair_bob").Id);
        }

        [Test]
        public void FindPart_WrongCategoryOrMissing_IsNull()
        {
            Assert.IsNull(_catalog.FindPart(AppearanceCategory.Hat, "hair_bob"));
            Assert.IsNull(_catalog.FindPart(AppearanceCategory.Hair, "hair_missing"));
            Assert.IsNull(_catalog.FindPart(AppearanceCategory.Hair, null));
            Assert.IsNull(_catalog.FindPart(AppearanceCategory.Hair, ""));
        }

        [Test]
        public void FindColour_EachSlot_LooksInItsOwnList()
        {
            Assert.AreEqual("hair_red", _catalog.FindColour(AppearanceColourSlot.Hair, "hair_red").Id);
            Assert.AreEqual("hair_red", _catalog.FindColour(AppearanceColourSlot.Brows, "hair_red").Id);
            Assert.AreEqual("skin_dark", _catalog.FindColour(AppearanceColourSlot.Skin, "skin_dark").Id);
            Assert.AreEqual("outfit_b", _catalog.FindColour(AppearanceColourSlot.Top, "outfit_b").Id);
            Assert.AreEqual("outfit_b", _catalog.FindColour(AppearanceColourSlot.Bottom, "outfit_b").Id);
        }

        [Test]
        public void FindColour_UnknownNullOrOtherList_IsNull()
        {
            Assert.IsNull(_catalog.FindColour(AppearanceColourSlot.Hair, "outfit_a"));
            Assert.IsNull(_catalog.FindColour(AppearanceColourSlot.Skin, "hair_red"));
            Assert.IsNull(_catalog.FindColour(AppearanceColourSlot.Top, null));
            Assert.IsNull(_catalog.FindColour(AppearanceColourSlot.Bottom, ""));
        }

        [Test]
        public void SetColourId_EachSlot_ChangesOnlyItsOwnField()
        {
            var look = new AppearanceSave();

            AppearanceCatalog.SetColourId(look, AppearanceColourSlot.Brows, "hair_red");
            AppearanceCatalog.SetColourId(look, AppearanceColourSlot.Bottom, "outfit_b");

            Assert.AreEqual(("", "hair_red", "", "", "outfit_b"), (look.hairColourId, look.browColourId, look.skinColourId, look.topColourId, look.bottomColourId));
            Assert.AreEqual("outfit_b", AppearanceCatalog.ColourId(look, AppearanceColourSlot.Bottom));
            Assert.AreEqual("", AppearanceCatalog.ColourId(look, AppearanceColourSlot.Top));
        }

        [Test]
        public void IsOptional_OnlyTheBodyIsRequired()
        {
            Assert.IsFalse(AppearanceCatalog.IsOptional(AppearanceCategory.Body));
            Assert.IsTrue(AppearanceCatalog.IsOptional(AppearanceCategory.Hair));
            Assert.IsTrue(AppearanceCatalog.IsOptional(AppearanceCategory.Accessory));
        }

        [Test]
        public void Normalize_ValidLook_KeepsIt()
        {
            var look = new AppearanceSave
            {
                bodyId = "body_tunic",
                hairId = "",
                beardId = "beard_full",
                hatId = "hat_cap",
                accessoryId = "",
                hairColourId = "hair_black",
                browColourId = "hair_red",
                skinColourId = "skin_dark",
                topColourId = "outfit_b",
                bottomColourId = "outfit_a",
            };

            var normal = _catalog.Normalize(look);

            Assert.AreEqual(("body_tunic", "", "beard_full", "hat_cap"), (normal.bodyId, normal.hairId, normal.beardId, normal.hatId));
            Assert.AreEqual(("hair_black", "hair_red", "skin_dark", "outfit_b", "outfit_a"),
                (normal.hairColourId, normal.browColourId, normal.skinColourId, normal.topColourId, normal.bottomColourId));
            Assert.AreNotSame(look, normal);
        }

        [Test]
        public void Normalize_NullOrNoBody_GivesTheDefaultLook()
        {
            foreach (var look in new[] { null, new AppearanceSave(), new AppearanceSave { hairId = "hair_spiky" } })
            {
                var normal = _catalog.Normalize(look);

                Assert.AreEqual(("body_dress", "hair_bob", "hair_red", "outfit_b"), (normal.bodyId, normal.hairId, normal.hairColourId, normal.topColourId));
                Assert.AreEqual(("hair_black", "skin_light", "outfit_a"), (normal.browColourId, normal.skinColourId, normal.bottomColourId), "Unset in the default look: the first ones.");
            }
        }

        [Test]
        public void Normalize_UnknownIds_FallBackToTheFirstBodyAndColoursAndToNone()
        {
            var look = new AppearanceSave
            {
                bodyId = "body_gone",
                hairId = "hair_gone",
                beardId = "hair_bob",
                hatId = "hat_gone",
                hairColourId = "gone",
                browColourId = "outfit_b",
                skinColourId = "hair_red",
                topColourId = "gone",
                bottomColourId = "skin_dark",
            };

            var normal = _catalog.Normalize(look);

            Assert.AreEqual(("body_tunic", "", "", ""), (normal.bodyId, normal.hairId, normal.beardId, normal.hatId));
            Assert.AreEqual(("hair_black", "hair_black", "skin_light", "outfit_a", "outfit_a"),
                (normal.hairColourId, normal.browColourId, normal.skinColourId, normal.topColourId, normal.bottomColourId));
        }

        [Test]
        public void Normalize_EmptyCatalog_GivesEmptyIds()
        {
            var empty = Create(ScriptableObject.CreateInstance<AppearanceCatalog>());
            empty.Init(null, null, null, null);

            var normal = empty.Normalize(null);

            Assert.AreEqual(("", "", "", ""), (normal.bodyId, normal.hairColourId, normal.skinColourId, normal.bottomColourId));
        }

        [Test]
        public void Random_SameSeed_GivesTheSameValidLook()
        {
            var first = _catalog.Random(new System.Random(3));
            var second = _catalog.Random(new System.Random(3));

            Assert.AreEqual(first.bodyId, second.bodyId);
            Assert.AreEqual(first.hairId, second.hairId);
            Assert.IsNotNull(_catalog.FindPart(AppearanceCategory.Body, first.bodyId));
            foreach (AppearanceColourSlot slot in Enum.GetValues(typeof(AppearanceColourSlot)))
            {
                Assert.AreEqual(AppearanceCatalog.ColourId(first, slot), AppearanceCatalog.ColourId(second, slot));
                Assert.IsNotNull(_catalog.FindColour(slot, AppearanceCatalog.ColourId(first, slot)), slot.ToString());
            }
        }

        [Test]
        public void Random_ManyTries_SometimesLeavesAnOptionalPartOffButNeverTheBody()
        {
            var random = new System.Random(11);
            var hatless = 0;
            for (var i = 0; i < 100; i++)
            {
                var look = _catalog.Random(random);
                Assert.IsNotEmpty(look.bodyId);
                hatless += look.hatId == "" ? 1 : 0;
            }

            Assert.Greater(hatless, 0);
            Assert.Less(hatless, 100);
        }

        [Test]
        public void Random_NullRandom_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _catalog.Random(null));
        }

        private AppearanceCatalog Catalog(AppearanceSave defaultLook)
        {
            var parts = new[]
            {
                Part("body_tunic", AppearanceCategory.Body),
                Part("hair_bob", AppearanceCategory.Hair),
                Part("hair_spiky", AppearanceCategory.Hair),
                Part("body_dress", AppearanceCategory.Body),
                Part("beard_full", AppearanceCategory.Beard),
                Part("hat_cap", AppearanceCategory.Hat),
            };
            var catalog = Create(ScriptableObject.CreateInstance<AppearanceCatalog>());
            catalog.Init(parts, new[] { Colour("hair_black"), Colour("hair_red") }, new[] { Colour("skin_light"), Colour("skin_dark") }, new[] { Colour("outfit_a"), Colour("outfit_b") }, 8, defaultLook);
            return catalog;
        }

        private AppearancePart Part(string id, AppearanceCategory category)
        {
            var part = Create(ScriptableObject.CreateInstance<AppearancePart>());
            part.Init(id, id, category, category == AppearanceCategory.Body ? id : null);
            return part;
        }

        private AppearanceColour Colour(string id)
        {
            var colour = Create(ScriptableObject.CreateInstance<AppearanceColour>());
            colour.Init(id, id, Color.white, null);
            return colour;
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
