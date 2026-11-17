using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Moonbrew.Core;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object = UnityEngine.Object;

namespace Moonbrew.Tests.PlayMode
{
    // The day cycle around the save: a new game, sleeping to save, and loading back with the recap.
    public class GameSessionIntegrationTests
    {
        private const string SleepSpot = "farm_house_door";

        private readonly List<Object> _created = new List<Object>();
        private string _folder;
        private EventBusAsset _bus;
        private ItemData _seeds;
        private ItemCatalog _catalog;
        private InventoryConfig _bagConfig;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "moonbrew_session_" + Guid.NewGuid().ToString("N"));
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            // Saves store IDs, so the test content needs them; JsonUtility fills the serialized fields.
            _seeds = Create(ScriptableObject.CreateInstance<ItemData>());
            JsonUtility.FromJsonOverwrite("{\"id\":\"seed_wheat\",\"maxStack\":99}", _seeds);
            _catalog = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            _catalog.Init(new[] { _seeds });
            _bagConfig = Create(ScriptableObject.CreateInstance<InventoryConfig>());
            JsonUtility.FromJsonOverwrite("{\"id\":\"inventory_player\",\"slotCount\":24}", _bagConfig);
        }

        [TearDown]
        public void TearDown()
        {
            foreach (var created in _created)
            {
                if (created != null)
                {
                    Object.Destroy(created);
                }
            }

            _created.Clear();
            if (Directory.Exists(_folder))
            {
                Directory.Delete(_folder, true);
            }
        }

        [UnityTest]
        public IEnumerator Start_NoSave_StartsANewGameOnDayOne()
        {
            var game = CreateGame(new[] { new ItemStack(_seeds, 10) });
            yield return null;

            Assert.IsTrue(game.Session.Current.isNew);
            Assert.AreEqual(10, game.Bag.Inventory.Count(_seeds));
            Assert.IsTrue(game.Clock.Clock.IsStarted);
            Assert.AreEqual(1, game.Clock.Clock.Day);
        }

        [UnityTest]
        public IEnumerator DayEnd_NightPassesBeforeTheSave()
        {
            CreateGame(null);
            var order = new List<string>();
            _bus.Subscribe<NightPassed>(message => order.Add("night " + message.Day));
            _bus.Subscribe<GameSaving>(message => order.Add("save"));
            yield return null;

            _bus.Publish(new SleepConfirmed());
            yield return null;

            CollectionAssert.AreEqual(new[] { "night 1", "save" }, order);
        }

        [UnityTest]
        public IEnumerator Sleep_Confirmed_SavesTheNextDayWithInventoryAndRecap()
        {
            var game = CreateGame(new[] { new ItemStack(_seeds, 10) });
            var offered = 0;
            _bus.Subscribe<SleepOffered>(message => offered++);
            yield return null;

            game.Bag.Inventory.Remove(_seeds, 4);
            _bus.Publish(new Interacted(SleepSpot));
            _bus.Publish(new SleepConfirmed());
            yield return null;

            Assert.AreEqual(1, offered);
            var saved = new SaveStore(_folder).MostRecent();
            Assert.IsNotNull(saved, "Nothing was saved.");
            Assert.AreEqual(2, saved.day);
            Assert.IsFalse(saved.isNew);
            Assert.AreEqual(6, saved.FindInventory(_bagConfig.Id).slots[0].quantity);
            Assert.AreEqual(1, saved.recap.day);
            Assert.AreEqual(-4, saved.recap.entries[0].delta);
        }

        [UnityTest]
        public IEnumerator Interacted_OtherObject_OffersNoSleep()
        {
            CreateGame(null);
            var offered = 0;
            _bus.Subscribe<SleepOffered>(message => offered++);
            yield return null;

            _bus.Publish(new Interacted("farm_letterbox"));

            Assert.AreEqual(0, offered);
            Assert.IsNull(new SaveStore(_folder).MostRecent());
        }

        [UnityTest]
        public IEnumerator Start_SavedGame_RestoresAndWaitsForTheRecap()
        {
            var saved = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            saved.lastPlayedUtc = DateTime.UtcNow.ToString("o");
            saved.inventories.Add(new InventorySave { id = _bagConfig.Id, slots = { new SlotSave { index = 3, itemId = _seeds.Id, quantity = 7 } } });
            saved.recap.entries.Add(new RecapEntry { inventoryId = _bagConfig.Id, itemId = _seeds.Id, delta = -3 });
            new SaveStore(_folder).Save(saved);
            DayRecap opened = null;
            _bus.Subscribe<DayRecapOpened>(message => opened = message.Recap);

            var game = CreateGame(new[] { new ItemStack(_seeds, 10) });
            yield return null;

            Assert.AreEqual(7, game.Bag.Inventory.Slots[3].Quantity, "The save was not restored.");
            Assert.AreEqual(7, game.Bag.Inventory.Count(_seeds), "Starting items were added on top of the save.");
            Assert.IsNotNull(opened);
            Assert.AreEqual(1, opened.day);
            Assert.IsFalse(game.Clock.Clock.IsStarted, "The clock started before the recap was closed.");

            _bus.Publish(new DayRecapClosed());
            yield return null;

            Assert.IsTrue(game.Clock.Clock.IsStarted);
            Assert.AreEqual(2, game.Clock.Clock.Day);
        }

        private (GameSession Session, GameClockRunner Clock, InventoryHolder Bag) CreateGame(ItemStack[] startingItems)
        {
            var root = Create(new GameObject("Game"));
            root.SetActive(false);

            var clock = root.AddComponent<GameClockRunner>();
            clock.Init(Create(ScriptableObject.CreateInstance<ClockConfig>()), _bus, false);

            var bag = root.AddComponent<InventoryHolder>();
            bag.Init(_bagConfig, _catalog, _bus, startingItems);

            var session = root.AddComponent<GameSession>();
            session.Init(Create(ScriptableObject.CreateInstance<SessionConfig>()), clock, _bus, _folder, false);

            root.SetActive(true);
            return (session, clock, bag);
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
