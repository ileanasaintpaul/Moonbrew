using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Npc;
using Moonbrew.Player;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    // The words of a villager's order, the friendship it builds, and the numbers behind the portrait's moods.
    public class DialogueTests
    {
        private readonly List<Object> _created = new List<Object>();
        private VillagerCatalog _catalog;

        [SetUp]
        public void SetUp()
        {
            var rose = Create(ScriptableObject.CreateInstance<VillagerData>());
            rose.Init("villager_rose", "Rose", new[] { new DialogueLine(Speaker.Villager, "Hello {owner}, one {dish}.", "happy", "wave") });
            var silent = Create(ScriptableObject.CreateInstance<VillagerData>());
            silent.Init("villager_silent", "Silent", new DialogueLine[0]);
            _catalog = Create(ScriptableObject.CreateInstance<VillagerCatalog>());
            _catalog.Init(new[] { rose, silent });
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
        public void FindSpeaker_VillagerAtTheTable_FindsThemAndTheirDish()
        {
            var found = DialogueScript.FindSpeaker(new[] { "customer_knight", "villager_rose" }, new[] { "dish_juice", "dish_cake" }, _catalog, out var villager, out var dishId);

            Assert.IsTrue(found);
            Assert.AreEqual(("villager_rose", "dish_cake"), (villager.Id, dishId));
        }

        [Test]
        public void FindSpeaker_OnlyPassingCustomersOrSilentVillagers_FindsNobody()
        {
            Assert.IsFalse(DialogueScript.FindSpeaker(new[] { "customer_knight", "villager_silent" }, new[] { "a", "b" }, _catalog, out _, out _));
        }

        [Test]
        public void FindSpeaker_NoCustomersOrCatalog_FindsNobody()
        {
            Assert.IsFalse(DialogueScript.FindSpeaker(null, null, _catalog, out _, out _));
            Assert.IsFalse(DialogueScript.FindSpeaker(new[] { "villager_rose" }, null, null, out _, out _));
        }

        [Test]
        public void Fill_OwnerAndDish_AreWrittenIn()
        {
            Assert.AreEqual("Hello Ana, one Cake.", DialogueScript.Fill("Hello {owner}, one {dish}.", "Ana", "Cake"));
        }

        [Test]
        public void Fill_NullText_IsEmpty()
        {
            Assert.AreEqual(string.Empty, DialogueScript.Fill(null, "Ana", "Cake"));
        }

        [TestCase(0f, 0)]
        [TestCase(0.1f, 4)]
        [TestCase(10f, 10)]
        [TestCase(float.MaxValue, 10)]
        [TestCase(-1f, 0)]
        public void Revealed_AfterSomeTime_ShowsThatManyLetters(float seconds, int letters)
        {
            Assert.AreEqual(letters, DialogueScript.Revealed("0123456789", seconds, 40f));
        }

        [Test]
        public void Relationships_Add_SumsPerVillager()
        {
            var relationships = new Relationships();

            Assert.IsTrue(relationships.Add("villager_rose", 1));
            Assert.IsTrue(relationships.Add("villager_rose", 2));

            Assert.AreEqual(3, relationships.PointsWith("villager_rose"));
            Assert.AreEqual(0, relationships.PointsWith("villager_basile"));
        }

        [Test]
        public void Relationships_InvalidAdd_IsRefused()
        {
            var relationships = new Relationships();

            Assert.IsFalse(relationships.Add("", 1));
            Assert.IsFalse(relationships.Add("villager_rose", 0));
            Assert.IsFalse(relationships.Add("villager_rose", -3));
            Assert.AreEqual(0, relationships.PointsWith(null));
        }

        [Test]
        public void Relationships_SaveAndRestore_RoundTrip()
        {
            var relationships = new Relationships();
            relationships.Add("villager_rose", 4);
            var restored = new Relationships();

            restored.Restore(relationships.ToSave());

            Assert.AreEqual(4, restored.PointsWith("villager_rose"));
        }

        [Test]
        public void Relationships_RestoreBadEntries_SkipsThem()
        {
            var relationships = new Relationships();

            relationships.Restore(new[] { null, new RelationSave { villagerId = "", points = 3 }, new RelationSave { villagerId = "villager_rose", points = -2 } });

            Assert.AreEqual(0, relationships.PointsWith("villager_rose"));
        }

        [Test]
        public void MoodTargets_KnownMuscles_AreSetAndClamped()
        {
            var names = new[] { "Head Nod Down-Up", "Right Arm Down-Up" };
            var offsets = new[]
            {
                new PortraitMoods.MuscleOffset { muscle = "Right Arm Down-Up", value = 2f },
                new PortraitMoods.MuscleOffset { muscle = "Unknown", value = 0.5f },
            };

            CollectionAssert.AreEqual(new[] { 0f, 1f }, MoodBlend.Targets(offsets, names));
        }

        [Test]
        public void MoodTargets_NoOffsetsOrNames_AreEmptyOrZero()
        {
            CollectionAssert.AreEqual(new[] { 0f }, MoodBlend.Targets(null, new[] { "Head Nod Down-Up" }));
            Assert.IsEmpty(MoodBlend.Targets(null, null));
        }

        [Test]
        public void Toward_SmallStep_MovesWithoutOvershooting()
        {
            Assert.AreEqual(1f, MoodBlend.Toward(0.9f, 1f, 10f, 1f));
            Assert.AreEqual(0.5f, MoodBlend.Toward(0.5f, 1f, 10f, 0f));
        }

        [Test]
        public void Mouth_AndBlink_StayInRange()
        {
            for (var t = 0f; t < 3f; t += 0.13f)
            {
                var mouth = MoodBlend.Mouth(t, 9f);
                Assert.That(mouth, Is.InRange(0f, 1f));
            }

            Assert.IsTrue(MoodBlend.Blinking(3.55f, 3.5f, 0.12f));
            Assert.IsFalse(MoodBlend.Blinking(1f, 3.5f, 0.12f));
            Assert.IsFalse(MoodBlend.Blinking(1f, 0f, 0.12f));
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
