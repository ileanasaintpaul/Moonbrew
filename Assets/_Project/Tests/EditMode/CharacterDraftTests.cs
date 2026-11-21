using System;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Player;
using NUnit.Framework;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.EditMode
{
    public class CharacterDraftTests
    {
        private readonly List<Object> _created = new List<Object>();
        private AppearanceCatalog _catalog;
        private CharacterDraft _draft;

        [SetUp]
        public void SetUp()
        {
            var parts = new[]
            {
                Part("top_tunic", AppearanceCategory.Top),
                Part("top_apron", AppearanceCategory.Top),
                Part("bottom_skirt", AppearanceCategory.Bottom),
                Part("hair_bob", AppearanceCategory.Hair),
                Part("hat_cap", AppearanceCategory.Hat),
            };
            _catalog = Create(ScriptableObject.CreateInstance<AppearanceCatalog>());
            _catalog.Init(parts, new[] { Colour("skin_light"), Colour("skin_dark") }, new[] { Colour("hair_black"), Colour("hair_red") },
                new[] { Colour("outfit_original"), Colour("outfit_blue") }, 6, new AppearanceSave { topId = "top_apron", bottomId = "bottom_skirt", hairId = "hair_bob" });
            _draft = new CharacterDraft(_catalog);
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
        public void New_StartsWithTheDefaultLookAndNoName()
        {
            Assert.AreEqual("top_apron", _draft.ChosenId(AppearanceCategory.Top));
            Assert.AreEqual("hair_bob", _draft.ChosenId(AppearanceCategory.Hair));
            Assert.AreEqual(("skin_light", "hair_black", "outfit_original"), (_draft.ColourId(AppearanceColourSlot.Skin), _draft.ColourId(AppearanceColourSlot.Beard), _draft.ColourId(AppearanceColourSlot.Top)));
            Assert.AreEqual("", _draft.Name);
            Assert.IsFalse(_draft.CanConfirm);
        }

        [Test]
        public void New_NullCatalog_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new CharacterDraft(null));
        }

        [Test]
        public void Choose_PartOfTheCategory_IsWorn()
        {
            Assert.IsTrue(_draft.Choose(AppearanceCategory.Hat, "hat_cap"));
            Assert.IsTrue(_draft.Choose(AppearanceCategory.Top, "top_tunic"));

            Assert.AreEqual(("hat_cap", "top_tunic"), (_draft.Look.hatId, _draft.Look.topId));
        }

        [Test]
        public void Choose_UnknownOrWrongCategory_IsRejected()
        {
            Assert.IsFalse(_draft.Choose(AppearanceCategory.Hair, "hair_missing"));
            Assert.IsFalse(_draft.Choose(AppearanceCategory.Hat, "hair_bob"));

            Assert.AreEqual(("hair_bob", ""), (_draft.Look.hairId, _draft.Look.hatId));
        }

        [Test]
        public void Choose_None_TakesAnOptionalPartOffButNotTheTopOrBottom()
        {
            Assert.IsTrue(_draft.Choose(AppearanceCategory.Hair, ""));
            Assert.IsFalse(_draft.Choose(AppearanceCategory.Top, null));
            Assert.IsFalse(_draft.Choose(AppearanceCategory.Bottom, ""));

            Assert.AreEqual(("", "top_apron", "bottom_skirt"), (_draft.Look.hairId, _draft.Look.topId, _draft.Look.bottomId));
        }

        [Test]
        public void ChooseColour_HairBrowsAndBeardApart_EachKeepsItsOwn()
        {
            Assert.IsTrue(_draft.ChooseColour(AppearanceColourSlot.Beard, "hair_red"));

            Assert.AreEqual(("hair_black", "hair_black", "hair_red"), (_draft.Look.hairColourId, _draft.Look.browColourId, _draft.Look.beardColourId));
        }

        [Test]
        public void ChooseColour_TopAndBottomApart_EachKeepsItsOwn()
        {
            Assert.IsTrue(_draft.ChooseColour(AppearanceColourSlot.Bottom, "outfit_blue"));
            Assert.IsTrue(_draft.ChooseColour(AppearanceColourSlot.Skin, "skin_dark"));

            Assert.AreEqual(("outfit_original", "outfit_blue", "skin_dark"), (_draft.Look.topColourId, _draft.Look.bottomColourId, _draft.Look.skinColourId));
        }

        [Test]
        public void ChooseColour_UnknownNullOrOtherList_IsRejected()
        {
            Assert.IsFalse(_draft.ChooseColour(AppearanceColourSlot.Hair, "outfit_blue"));
            Assert.IsFalse(_draft.ChooseColour(AppearanceColourSlot.Brows, null));
            Assert.IsFalse(_draft.ChooseColour(AppearanceColourSlot.Skin, "hair_red"));
            Assert.IsFalse(_draft.ChooseColour(AppearanceColourSlot.Top, "missing"));

            Assert.AreEqual(("hair_black", "skin_light", "outfit_original"), (_draft.Look.hairColourId, _draft.Look.skinColourId, _draft.Look.topColourId));
        }

        [Test]
        public void SetShape_Value_IsKept()
        {
            _draft.SetShape(BodyShape.Type, 75);
            _draft.SetShape(BodyShape.Size, -20);

            Assert.AreEqual((75, -20, 0), (_draft.Look.bodyType, _draft.Look.bodySize, _draft.Look.musculature));
            Assert.AreEqual(75, _draft.ShapeValue(BodyShape.Type));
        }

        [Test]
        public void SetShape_BeyondTheLimits_IsKeptWithinThem()
        {
            _draft.SetShape(BodyShape.Musculature, 1000);
            _draft.SetShape(BodyShape.Size, -1000);

            Assert.AreEqual((AppearanceCatalog.ShapeLimit, -AppearanceCatalog.ShapeLimit), (_draft.Look.musculature, _draft.Look.bodySize));
        }

        [Test]
        public void Look_IsACopy()
        {
            var look = _draft.Look;
            look.topId = "changed";

            Assert.AreEqual("top_apron", _draft.ChosenId(AppearanceCategory.Top));
        }

        [Test]
        public void Rename_Name_CanConfirm()
        {
            _draft.Rename("Rose");

            Assert.AreEqual("Rose", _draft.Name);
            Assert.IsTrue(_draft.CanConfirm);
        }

        [Test]
        public void Rename_TooLong_IsCutToTheLongestName()
        {
            _draft.Rename("Rosemarie");

            Assert.AreEqual("Rosema", _draft.Name);
        }

        [Test]
        public void Rename_NullOrBlank_CannotConfirm()
        {
            _draft.Rename(null);
            Assert.AreEqual("", _draft.Name);
            Assert.IsFalse(_draft.CanConfirm);

            _draft.Rename("   ");
            Assert.IsFalse(_draft.CanConfirm);
        }

        private AppearancePart Part(string id, AppearanceCategory category)
        {
            var part = Create(ScriptableObject.CreateInstance<AppearancePart>());
            part.Init(id, id, category);
            return part;
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
