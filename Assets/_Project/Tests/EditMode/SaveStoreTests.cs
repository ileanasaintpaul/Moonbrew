using System;
using System.IO;
using Moonbrew.Core;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.EditMode
{
    public class SaveStoreTests
    {
        private string _folder;
        private SaveStore _store;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "moonbrew_tests_" + Guid.NewGuid().ToString("N"));
            _store = new SaveStore(_folder);
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
        public void Constructor_EmptyFolder_Throws()
        {
            Assert.Throws<ArgumentException>(() => new SaveStore(string.Empty));
            Assert.Throws<ArgumentException>(() => new SaveStore(null));
        }

        [Test]
        public void SaveThenLoad_SameGame_ReturnsTheSavedState()
        {
            var data = Game("a", 4, "2026-11-17T10:00:00Z");

            _store.Save(data);
            var loaded = _store.Load("a");

            Assert.AreEqual(4, loaded.day);
            Assert.AreEqual("a", loaded.slotId);
        }

        [Test]
        public void Save_SameGameTwice_KeepsTheLatest()
        {
            _store.Save(Game("a", 2, "2026-11-17T10:00:00Z"));
            _store.Save(Game("a", 3, "2026-11-17T11:00:00Z"));

            Assert.AreEqual(3, _store.Load("a").day);
            Assert.AreEqual(1, _store.List().Count);
        }

        [Test]
        public void Save_NoSlot_Throws()
        {
            Assert.Throws<ArgumentException>(() => _store.Save(new SaveData()));
            Assert.Throws<ArgumentException>(() => _store.Save(null));
        }

        [Test]
        public void Load_MissingGameOrEmptyId_ReturnsNull()
        {
            Assert.IsNull(_store.Load("nothing"));
            Assert.IsNull(_store.Load(null));
        }

        [Test]
        public void List_SeveralGames_MostRecentFirst()
        {
            _store.Save(Game("old", 9, "2026-11-10T08:00:00Z"));
            _store.Save(Game("recent", 2, "2026-11-17T08:00:00Z"));
            _store.Save(Game("middle", 5, "2026-11-12T08:00:00Z"));

            var games = _store.List();

            Assert.AreEqual(new[] { "recent", "middle", "old" }, games.ConvertAll(game => game.slotId).ToArray());
            Assert.AreEqual("recent", _store.MostRecent().slotId);
        }

        [Test]
        public void List_NoFolderYet_IsEmpty()
        {
            Assert.IsEmpty(_store.List());
            Assert.IsNull(_store.MostRecent());
        }

        [Test]
        public void List_CorruptFile_IsSkipped()
        {
            _store.Save(Game("good", 2, "2026-11-17T08:00:00Z"));
            File.WriteAllText(Path.Combine(_folder, "game_broken.json"), "{ not json");
            LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex(@"\[Save\] Ignoring"));

            var games = _store.List();

            Assert.AreEqual(1, games.Count);
            Assert.AreEqual("good", games[0].slotId);
        }

        [Test]
        public void Delete_ExistingGame_RemovesOnlyThatGame()
        {
            _store.Save(Game("a", 2, "2026-11-17T08:00:00Z"));
            _store.Save(Game("b", 2, "2026-11-17T09:00:00Z"));

            Assert.IsTrue(_store.Delete("a"));
            Assert.IsNull(_store.Load("a"));
            Assert.IsNotNull(_store.Load("b"));
        }

        [Test]
        public void Delete_MissingGame_ReturnsFalse()
        {
            Assert.IsFalse(_store.Delete("nothing"));
            Assert.IsFalse(_store.Delete(null));
        }

        [Test]
        public void NewSlotId_TwoCalls_AreDifferent()
        {
            Assert.AreNotEqual(SaveStore.NewSlotId(), SaveStore.NewSlotId());
        }

        private static SaveData Game(string slotId, int day, string lastPlayedUtc)
        {
            var data = SaveData.CreateNew(slotId, day);
            data.lastPlayedUtc = lastPlayedUtc;
            return data;
        }
    }
}
