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
    // Walking between the farm and the café: the day is carried in memory from one scene's session to the next.
    // Each game stands for one scene; the tests stay in their own scene instead of loading another.
    public class SceneTravelIntegrationTests
    {
        private const string PlayerTag = "Player";

        private readonly List<Object> _created = new List<Object>();
        private string _folder;
        private EventBusAsset _bus;
        private DayCarry _carry;
        private ItemData _seeds;
        private ItemCatalog _catalog;
        private InventoryConfig _bagConfig;

        [SetUp]
        public void SetUp()
        {
            _folder = Path.Combine(Path.GetTempPath(), "moonbrew_travel_" + Guid.NewGuid().ToString("N"));
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _carry = Create(ScriptableObject.CreateInstance<DayCarry>());
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
        public IEnumerator Portal_PlayerWalksIn_RequestsTheTravel()
        {
            var portal = Create(new GameObject("Portal"));
            portal.AddComponent<BoxCollider>().isTrigger = true;
            portal.AddComponent<ScenePortal>().Init("SC_Cafe", "cafe_entrance", _bus);
            var player = Create(new GameObject("Player") { tag = PlayerTag });
            player.transform.position = new Vector3(0f, 0f, 5f);
            player.AddComponent<CapsuleCollider>();
            player.AddComponent<Rigidbody>().isKinematic = true;
            TravelRequested? requested = null;
            _bus.Subscribe<TravelRequested>(message => requested = message);
            yield return new WaitForFixedUpdate();

            player.GetComponent<Rigidbody>().MovePosition(Vector3.zero);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.IsTrue(requested.HasValue, "Walking into the portal did not ask to travel.");
            Assert.AreEqual("SC_Cafe", requested.Value.Scene);
            Assert.AreEqual("cafe_entrance", requested.Value.Arrival);
        }

        [UnityTest]
        public IEnumerator Portal_OtherObjectWalksIn_RequestsNothing()
        {
            var portal = Create(new GameObject("Portal"));
            portal.AddComponent<BoxCollider>().isTrigger = true;
            portal.AddComponent<ScenePortal>().Init("SC_Cafe", "cafe_entrance", _bus);
            var customer = Create(new GameObject("Customer"));
            customer.transform.position = new Vector3(0f, 0f, 5f);
            customer.AddComponent<CapsuleCollider>();
            customer.AddComponent<Rigidbody>().isKinematic = true;
            var requests = 0;
            _bus.Subscribe<TravelRequested>(message => requests++);
            yield return new WaitForFixedUpdate();

            customer.GetComponent<Rigidbody>().MovePosition(Vector3.zero);
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            Assert.AreEqual(0, requests);
        }

        [UnityTest]
        public IEnumerator Travel_ToAnotherScene_CarriesTheBagAndTheTimeWithoutSaving()
        {
            var farm = CreateGame(true);
            yield return null;
            farm.Clock.Clock.AdvanceMinutes(200);
            var leftAt = farm.Clock.Clock.Minutes;
            farm.Bag.Inventory.Remove(_seeds, 4);

            _bus.Publish(new TravelRequested("SC_Cafe", "cafe_entrance"));
            yield return null;
            Object.Destroy(farm.Root);
            yield return null;
            var arrived = 0;
            var recaps = 0;
            _bus.Subscribe<TravelArrived>(message => arrived++);
            _bus.Subscribe<DayRecapOpened>(message => recaps++);
            var cafe = CreateGame(false);
            yield return null;

            Assert.AreEqual(6, cafe.Bag.Inventory.Count(_seeds), "The bag was not carried, or starting items were added.");
            Assert.IsTrue(cafe.Clock.Clock.IsStarted);
            Assert.AreEqual(1, cafe.Clock.Clock.Day);
            Assert.AreEqual(leftAt, cafe.Clock.Clock.Minutes);
            Assert.AreEqual(1, arrived);
            Assert.AreEqual(0, recaps);
            Assert.IsNull(new SaveStore(_folder).MostRecent(), "Travelling saved the game in the middle of the day.");
        }

        [UnityTest]
        public IEnumerator Travel_SecondRequestWhileLeaving_IsIgnored()
        {
            var farm = CreateGame(true);
            var started = 0;
            _bus.Subscribe<TravelStarted>(message => started++);
            yield return null;

            _bus.Publish(new TravelRequested("SC_Cafe", "cafe_entrance"));
            _bus.Publish(new TravelRequested("SC_Cafe", "cafe_entrance"));
            yield return null;

            Assert.AreEqual(1, started);
            Assert.IsTrue(farm.Clock.Clock.IsPaused, "The clock kept running during the fade.");
        }

        [UnityTest]
        public IEnumerator DayEnd_AwayFromTheWakeScene_SavesTheWholeDayThere()
        {
            var farm = CreateGame(true);
            yield return null;
            farm.Bag.Inventory.Remove(_seeds, 4);
            _bus.Publish(new TravelRequested("SC_Cafe", "cafe_entrance"));
            yield return null;
            Object.Destroy(farm.Root);
            yield return null;
            var cafe = CreateGame(false);
            yield return null;
            cafe.Bag.Inventory.Remove(_seeds, 1);

            cafe.Clock.Clock.StartNextDay();
            yield return null;
            Assert.IsNull(new SaveStore(_folder).MostRecent(), "The café saved the day instead of the farm.");
            Object.Destroy(cafe.Root);
            yield return null;
            CreateGame(true);
            yield return null;

            var saved = new SaveStore(_folder).MostRecent();
            Assert.IsNotNull(saved, "The day was not saved in the wake scene.");
            Assert.AreEqual(2, saved.day);
            Assert.AreEqual(5, saved.FindInventory(_bagConfig.Id).slots[0].quantity);
            Assert.AreEqual(1, saved.recap.day);
            Assert.AreEqual(1, saved.recap.entries.Count);
            Assert.AreEqual(-5, saved.recap.entries[0].delta, "The recap does not add up both scenes.");
        }

        private (GameObject Root, GameClockRunner Clock, InventoryHolder Bag) CreateGame(bool wakeScene)
        {
            var root = Create(new GameObject(wakeScene ? "Farm" : "Cafe"));
            root.SetActive(false);

            var clock = root.AddComponent<GameClockRunner>();
            clock.Init(Create(ScriptableObject.CreateInstance<ClockConfig>()), _bus, false);

            var bag = root.AddComponent<InventoryHolder>();
            bag.Init(_bagConfig, _catalog, _bus, new[] { new ItemStack(_seeds, 10) });

            var session = root.AddComponent<GameSession>();
            session.Init(Create(ScriptableObject.CreateInstance<SessionConfig>()), clock, _bus, _folder, false, _carry, wakeScene);

            root.SetActive(true);
            return (root, clock, bag);
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
