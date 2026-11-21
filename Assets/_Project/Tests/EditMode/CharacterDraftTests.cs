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
                Part("body_tunic", AppearanceCategory.Body),
                Part("body_dress", AppearanceCategory.Body),
                Part("hair_bob", AppearanceCategory.Hair),
                Part("hat_cap", AppearanceCategory.Hat),
            };
            _catalog = Create(ScriptableObject.CreateInstance<AppearanceCatalog>());
            _catalog.Init(parts, new[] { Colour("hair_black"), Colour("hair_red") }, new[] { Colour("skin_light"), Colour("skin_dark") }, new[] { Colour("outfit_a"), Colour("outfit_b") }, 6,
                new AppearanceSave { bodyId = "body_dress", hairId = "hair_bob" });
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
            Assert.AreEqual("body_dress", _draft.ChosenId(AppearanceCategory.Body));
            Assert.AreEqual("hair_bob", _draft.ChosenId(AppearanceCategory.Hair));
            Assert.AreEqual(("hair_black", "skin_light", "outfit_a"), (_draft.ColourId(AppearanceColourSlot.Hair), _draft.ColourId(AppearanceColourSlot.Skin), _draft.ColourId(AppearanceColourSlot.Top)));
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
            Assert.IsTrue(_draft.Choose(AppearanceCategory.Body, "body_tunic"));

            Assert.AreEqual(("hat_cap", "body_tunic"), (_draft.Look.hatId, _draft.Look.bodyId));
        }

        [Test]
        public void Choose_UnknownOrWrongCategory_IsRejected()
        {
            Assert.IsFalse(_draft.Choose(AppearanceCategory.Hair, "hair_missing"));
            Assert.IsFalse(_draft.Choose(AppearanceCategory.Hat, "hair_bob"));

            Assert.AreEqual(("hair_bob", ""), (_draft.Look.hairId, _draft.Look.hatId));
        }

        [Test]
        public void Choose_None_TakesAnOptionalPartOffButNotTheBody()
        {
            Assert.IsTrue(_draft.Choose(AppearanceCategory.Hair, ""));
            Assert.IsFalse(_draft.Choose(AppearanceCategory.Body, null));

            Assert.AreEqual(("", "body_dress"), (_draft.Look.hairId, _draft.Look.bodyId));
        }

        [Test]
        public void ChooseColour_HairAndBrowsApart_EachKeepsItsOwn()
        {
            Assert.IsTrue(_draft.ChooseColour(AppearanceColourSlot.Hair, "hair_red"));

            Assert.AreEqual("hair_red", _draft.Look.hairColourId);
            Assert.AreEqual("hair_black", _draft.Look.browColourId);
        }

        [Test]
        public void ChooseColour_TopAndBottomApart_EachKeepsItsOwn()
        {
            Assert.IsTrue(_draft.ChooseColour(AppearanceColourSlot.Bottom, "outfit_b"));
            Assert.IsTrue(_draft.ChooseColour(AppearanceColourSlot.Skin, "skin_dark"));

            Assert.AreEqual(("outfit_a", "outfit_b", "skin_dark"), (_draft.Look.topColourId, _draft.Look.bottomColourId, _draft.Look.skinColourId));
        }

        [Test]
        public void ChooseColour_UnknownNullOrOtherList_IsRejected()
        {
            Assert.IsFalse(_draft.ChooseColour(AppearanceColourSlot.Hair, "outfit_b"));
            Assert.IsFalse(_draft.ChooseColour(AppearanceColourSlot.Brows, null));
            Assert.IsFalse(_draft.ChooseColour(AppearanceColourSlot.Skin, "hair_red"));
            Assert.IsFalse(_draft.ChooseColour(AppearanceColourSlot.Top, "missing"));

            Assert.AreEqual(("hair_black", "hair_black", "skin_light", "outfit_a"), (_draft.Look.hairColourId, _draft.Look.browColourId, _draft.Look.skinColourId, _draft.Look.topColourId));
        }

        [Test]
        public void Look_IsACopy()
        {
            var look = _draft.Look;
            look.bodyId = "changed";

            Assert.AreEqual("body_dress", _draft.ChosenId(AppearanceCategory.Body));
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

        [Test]
        public void Randomize_GivesAValidLook()
        {
            _draft.Randomize(new System.Random(5));

            Assert.IsNotNull(_catalog.FindPart(AppearanceCategory.Body, _draft.Look.bodyId));
            Assert.IsNotNull(_catalog.FindColour(AppearanceColourSlot.Brows, _draft.ColourId(AppearanceColourSlot.Brows)));
            Assert.IsNotNull(_catalog.FindColour(AppearanceColourSlot.Bottom, _draft.ColourId(AppearanceColourSlot.Bottom)));
        }

        [Test]
        public void Randomize_NullRandom_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => _draft.Randomize(null));
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
