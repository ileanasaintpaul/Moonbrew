using System;
using System.IO;
using Moonbrew.Core;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class GameSlotsTests
    {
        private static readonly DateTime Earlier = new DateTime(2026, 11, 19, 20, 0, 0, DateTimeKind.Utc);
        private static readonly DateTime Later = new DateTime(2026, 11, 20, 9, 0, 0, DateTimeKind.Utc);

        private string _folder;
        private SaveStore _store;
        private GameSlots _slots;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "moonbrew_tests_" + Guid.NewGuid().ToString("N"));
            _store = new SaveStore(_folder);
            _slots = new GameSlots(_store);
        }

        [TearDown]
        public void TearDown()
        {
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [Test]
        public void Constructor_NullStore_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => new GameSlots(null));
        }

        [Test]
        public void CanContinue_NoGame_IsFalse()
        {
            Assert.IsFalse(_slots.CanContinue);
            Assert.IsEmpty(_slots.List());
        }

        [Test]
        public void CreateNew_FirstDay_SavesANewGameThatCanContinue()
        {
            var game = _slots.CreateNew(1, Earlier);

            Assert.IsTrue(game.isNew);
            Assert.AreEqual(1, game.day);
            Assert.IsTrue(_slots.CanContinue);
            Assert.AreEqual(game.slotId, _store.MostRecent().slotId);
        }

        [Test]
        public void CreateNew_OwnerNameAndLook_AreSavedWithTheNameTrimmed()
        {
            _slots.CreateNew(1, Earlier, "  Rose ", new AppearanceSave { topId = "top_doublet" });

            var saved = _store.MostRecent();
            Assert.AreEqual("Rose", saved.ownerName);
            Assert.AreEqual("top_doublet", saved.appearance.topId);
        }

        [Test]
        public void CreateNew_NoNameAndNoLook_SavesAnEmptyNameAndTheDefaultLook()
        {
            _slots.CreateNew(1, Earlier, null, null);

            var saved = _store.MostRecent();
            Assert.AreEqual("", saved.ownerName);
            Assert.IsNotNull(saved.appearance);
            Assert.AreEqual("", saved.appearance.topId);
        }

        [Test]
        public void CreateNew_DayZero_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => _slots.CreateNew(0, Earlier));
            Assert.IsFalse(_slots.CanContinue);
        }

        [Test]
        public void Choose_OlderGame_BecomesTheMostRecent()
        {
            var first = _slots.CreateNew(1, Earlier);
            _slots.CreateNew(1, Later);

            var chosen = _slots.Choose(first.slotId, Later.AddMinutes(1));

            Assert.IsTrue(chosen);
            Assert.AreEqual(first.slotId, _store.MostRecent().slotId);
            Assert.AreEqual(first.slotId, _slots.List()[0].slotId);
        }

        [Test]
        public void Choose_MissingGame_IsFalseAndChangesNothing()
        {
            var game = _slots.CreateNew(1, Earlier);

            Assert.IsFalse(_slots.Choose("missing", Later));
            Assert.AreEqual(game.slotId, _store.MostRecent().slotId);
        }

        [Test]
        public void Choose_NullOrEmptyId_IsFalse()
        {
            Assert.IsFalse(_slots.Choose(null, Later));
            Assert.IsFalse(_slots.Choose(string.Empty, Later));
        }
    }
}
