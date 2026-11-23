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
            _catalog = Catalog(new AppearanceSave { topId = "top_apron", bottomId = "bottom_skirt", hairId = "hair_bob", hairColourId = "hair_red", topColourId = "outfit_blue", bodyType = 60 });
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
            var tops = _catalog.PartsOf(AppearanceCategory.Top);

            CollectionAssert.AreEqual(new[] { "top_tunic", "top_apron" }, tops.ConvertAll(part => part.Id));
        }

        [Test]
        public void ThumbnailLook_Part_WearsOnlyThatOptionalPartWithNeutralBody()
        {
            var look = _catalog.ThumbnailLook(AppearanceCategory.Beard, "beard_any");

            Assert.AreEqual("beard_any", look.beardId);
            Assert.AreEqual("", look.hairId);
            Assert.AreEqual(0, look.bodyType);
        }

        [Test]
        public void ThumbnailLook_NullPart_LeavesTheCategoryEmpty()
        {
            Assert.AreEqual("", _catalog.ThumbnailLook(AppearanceCategory.Hair, null).hairId);
        }

        [Test]
        public void ThumbnailLook_WithAThumbnailColour_PaintsHairBrowsAndBeard()
        {
            var hair = new List<AppearanceColour>(_catalog.HairColours);
            var catalog = Create(ScriptableObject.CreateInstance<AppearanceCatalog>());
            catalog.Init(new List<AppearancePart>(_catalog.Parts).ToArray(), new List<AppearanceColour>(_catalog.SkinColours).ToArray(), hair.ToArray(), new List<AppearanceColour>(_catalog.OutfitColours).ToArray(), thumbnailColour: hair[hair.Count - 1]);

            var look = catalog.ThumbnailLook(AppearanceCategory.Hair, "hair_bob");

            Assert.AreEqual(hair[hair.Count - 1].Id, look.hairColourId);
            Assert.AreEqual(hair[hair.Count - 1].Id, look.browColourId);
            Assert.AreEqual(hair[hair.Count - 1].Id, look.beardColourId);
        }

        [Test]
        public void ThumbnailLook_DoesNotChangeTheDefaultLook()
        {
            _catalog.ThumbnailLook(AppearanceCategory.Hair, "");

            Assert.AreEqual("hair_bob", _catalog.Normalize(null).hairId);
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
            Assert.AreEqual("skin_dark", _catalog.FindColour(AppearanceColourSlot.Skin, "skin_dark").Id);
            Assert.AreEqual("hair_red", _catalog.FindColour(AppearanceColourSlot.Hair, "hair_red").Id);
            Assert.AreEqual("hair_red", _catalog.FindColour(AppearanceColourSlot.Brows, "hair_red").Id);
            Assert.AreEqual("hair_red", _catalog.FindColour(AppearanceColourSlot.Beard, "hair_red").Id);
            Assert.AreEqual("outfit_blue", _catalog.FindColour(AppearanceColourSlot.Top, "outfit_blue").Id);
            Assert.AreEqual("outfit_blue", _catalog.FindColour(AppearanceColourSlot.Bottom, "outfit_blue").Id);
        }

        [Test]
        public void FindColour_UnknownNullOrOtherList_IsNull()
        {
            Assert.IsNull(_catalog.FindColour(AppearanceColourSlot.Hair, "outfit_blue"));
            Assert.IsNull(_catalog.FindColour(AppearanceColourSlot.Skin, "hair_red"));
            Assert.IsNull(_catalog.FindColour(AppearanceColourSlot.Top, null));
            Assert.IsNull(_catalog.FindColour(AppearanceColourSlot.Bottom, ""));
        }

        [Test]
        public void SetPartId_EachCategory_ChangesOnlyItsOwnField()
        {
            var look = new AppearanceSave();

            AppearanceCatalog.SetPartId(look, AppearanceCategory.Brows, "brows_3");
            AppearanceCatalog.SetPartId(look, AppearanceCategory.Bottom, "bottom_skirt");

            Assert.AreEqual(("", "bottom_skirt", "brows_3", ""), (look.topId, look.bottomId, look.browsId, look.beardId));
            Assert.AreEqual("brows_3", AppearanceCatalog.PartId(look, AppearanceCategory.Brows));
        }

        [Test]
        public void SetColourId_EachSlot_ChangesOnlyItsOwnField()
        {
            var look = new AppearanceSave();

            AppearanceCatalog.SetColourId(look, AppearanceColourSlot.Beard, "hair_red");
            AppearanceCatalog.SetColourId(look, AppearanceColourSlot.Bottom, "outfit_blue");

            Assert.AreEqual(("", "", "hair_red", "", "outfit_blue"), (look.hairColourId, look.browColourId, look.beardColourId, look.topColourId, look.bottomColourId));
            Assert.AreEqual("outfit_blue", AppearanceCatalog.ColourId(look, AppearanceColourSlot.Bottom));
        }

        [Test]
        public void SetShapeValue_BeyondTheLimits_IsKeptWithinThem()
        {
            var look = new AppearanceSave();

            AppearanceCatalog.SetShapeValue(look, BodyShape.Size, 500);
            AppearanceCatalog.SetShapeValue(look, BodyShape.Musculature, -500);
            AppearanceCatalog.SetShapeValue(look, BodyShape.Type, 0);

            Assert.AreEqual((0, AppearanceCatalog.ShapeLimit, -AppearanceCatalog.ShapeLimit), (look.bodyType, look.bodySize, look.musculature));
            Assert.AreEqual(AppearanceCatalog.ShapeLimit, AppearanceCatalog.ShapeValue(look, BodyShape.Size));
        }

        [Test]
        public void IsOptional_OnlyTheTopAndBottomAreRequired()
        {
            Assert.IsFalse(AppearanceCatalog.IsOptional(AppearanceCategory.Top));
            Assert.IsFalse(AppearanceCatalog.IsOptional(AppearanceCategory.Bottom));
            Assert.IsTrue(AppearanceCatalog.IsOptional(AppearanceCategory.Hair));
            Assert.IsTrue(AppearanceCatalog.IsOptional(AppearanceCategory.Hat));
        }

        [Test]
        public void Normalize_ValidLook_KeepsIt()
        {
            var look = new AppearanceSave
            {
                topId = "top_tunic",
                bottomId = "bottom_skirt",
                hairId = "",
                browsId = "brows_3",
                beardId = "beard_full",
                hatId = "hat_cap",
                bodyType = -40,
                bodySize = 20,
                musculature = 90,
                skinColourId = "skin_dark",
                hairColourId = "hair_black",
                browColourId = "hair_red",
                beardColourId = "hair_red",
                topColourId = "outfit_blue",
                bottomColourId = "outfit_original",
            };

            var normal = _catalog.Normalize(look);

            Assert.AreEqual(("top_tunic", "bottom_skirt", "", "brows_3", "beard_full", "hat_cap"), (normal.topId, normal.bottomId, normal.hairId, normal.browsId, normal.beardId, normal.hatId));
            Assert.AreEqual((-40, 20, 90), (normal.bodyType, normal.bodySize, normal.musculature));
            Assert.AreEqual(("skin_dark", "hair_black", "hair_red", "hair_red", "outfit_blue", "outfit_original"),
                (normal.skinColourId, normal.hairColourId, normal.browColourId, normal.beardColourId, normal.topColourId, normal.bottomColourId));
            Assert.AreNotSame(look, normal);
        }

        [Test]
        public void Normalize_NullOrNoTop_GivesTheDefaultLook()
        {
            foreach (var look in new[] { null, new AppearanceSave(), new AppearanceSave { hairId = "hair_spiky" } })
            {
                var normal = _catalog.Normalize(look);

                Assert.AreEqual(("top_apron", "bottom_skirt", "hair_bob", 60), (normal.topId, normal.bottomId, normal.hairId, normal.bodyType));
                Assert.AreEqual(("hair_red", "outfit_blue"), (normal.hairColourId, normal.topColourId));
                Assert.AreEqual(("skin_light", "hair_original", "outfit_original"), (normal.skinColourId, normal.browColourId, normal.bottomColourId), "Unset in the default look: the first ones.");
            }
        }

        [Test]
        public void Normalize_UnknownIds_FallBackToTheFirstTopBottomAndColoursAndToNone()
        {
            var look = new AppearanceSave
            {
                topId = "top_gone",
                bottomId = "hair_bob",
                hairId = "hair_gone",
                beardId = "hair_bob",
                hatId = "hat_gone",
                bodySize = 900,
                skinColourId = "hair_red",
                hairColourId = "gone",
                browColourId = "outfit_blue",
                beardColourId = "skin_dark",
                topColourId = "gone",
                bottomColourId = "hair_red",
            };

            var normal = _catalog.Normalize(look);

            Assert.AreEqual(("top_tunic", "bottom_skirt", "", "", ""), (normal.topId, normal.bottomId, normal.hairId, normal.beardId, normal.hatId));
            Assert.AreEqual(AppearanceCatalog.ShapeLimit, normal.bodySize);
            Assert.AreEqual(("skin_light", "hair_original", "hair_original", "hair_original", "outfit_original", "outfit_original"),
                (normal.skinColourId, normal.hairColourId, normal.browColourId, normal.beardColourId, normal.topColourId, normal.bottomColourId));
        }

        [Test]
        public void FindPronouns_KnownId_FindsIt()
        {
            Assert.AreEqual("pronouns_he", _catalog.FindPronouns("pronouns_he").Id);
        }

        [Test]
        public void FindPronouns_UnknownOrEmpty_GivesTheFirst()
        {
            Assert.AreEqual("pronouns_she", _catalog.FindPronouns("pronouns_gone").Id);
            Assert.AreEqual("pronouns_she", _catalog.FindPronouns(null).Id);
        }

        [Test]
        public void FindPronouns_NoChoices_IsNull()
        {
            var empty = Create(ScriptableObject.CreateInstance<AppearanceCatalog>());
            empty.Init(null, null, null, null);

            Assert.IsNull(empty.FindPronouns("pronouns_she"));
        }

        [Test]
        public void Normalize_EmptyCatalog_GivesEmptyIds()
        {
            var empty = Create(ScriptableObject.CreateInstance<AppearanceCatalog>());
            empty.Init(null, null, null, null);

            var normal = empty.Normalize(null);

            Assert.AreEqual(("", "", "", ""), (normal.topId, normal.bottomId, normal.hairColourId, normal.skinColourId));
        }

        private AppearanceCatalog Catalog(AppearanceSave defaultLook)
        {
            var parts = new[]
            {
                Part("top_tunic", AppearanceCategory.Top),
                Part("hair_bob", AppearanceCategory.Hair),
                Part("hair_spiky", AppearanceCategory.Hair),
                Part("top_apron", AppearanceCategory.Top),
                Part("bottom_skirt", AppearanceCategory.Bottom),
                Part("brows_3", AppearanceCategory.Brows),
                Part("beard_full", AppearanceCategory.Beard),
                Part("hat_cap", AppearanceCategory.Hat),
            };
            var catalog = Create(ScriptableObject.CreateInstance<AppearanceCatalog>());
            catalog.Init(parts, new[] { Colour("skin_light"), Colour("skin_dark") }, new[] { Colour("hair_original"), Colour("hair_black"), Colour("hair_red") },
                new[] { Colour("outfit_original"), Colour("outfit_blue") }, 8, defaultLook, new[] { Pronouns("pronouns_she"), Pronouns("pronouns_he") });
            return catalog;
        }

        private AppearancePart Part(string id, AppearanceCategory category)
        {
            var part = Create(ScriptableObject.CreateInstance<AppearancePart>());
            part.Init(id, id, category);
            return part;
        }

        private PronounSet Pronouns(string id)
        {
            var set = Create(ScriptableObject.CreateInstance<PronounSet>());
            set.Init(id, id);
            return set;
        }

        private AppearanceColour Colour(string id)
        {
            var colour = Create(ScriptableObject.CreateInstance<AppearanceColour>());
            colour.Init(id, id, Color.white);
            return colour;
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
