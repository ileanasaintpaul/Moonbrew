using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // The mill: wheat in hand loads it, game time grinds it, a click collects the flour.
    public class StationIntegrationTests
    {
        private const int WheatSlot = 0;
        private const int OtherSlot = 1;
        private const int Morning = 360;

        private static readonly Vector3 MillPosition = new Vector3(1.5f, 0f, 0.5f);

        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private ItemData _wheat;
        private ItemData _flour;
        private ItemData _hoe;
        private StationData _millData;
        private InventoryHolder _bag;
        private Transform _player;
        private Sprite _hourglass;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _wheat = CreateItem("{\"id\":\"crop_wheat\",\"maxStack\":99}");
            _flour = CreateItem("{\"id\":\"ingredient_flour\",\"maxStack\":99}");
            _hoe = CreateItem("{\"id\":\"tool_hoe\",\"maxStack\":1}");
            var process = Create(ScriptableObject.CreateInstance<ProcessData>());
            process.Init("process_flour", _wheat, 1, _flour, 1, 60);
            _millData = Create(ScriptableObject.CreateInstance<StationData>());
            _hourglass = Sprite.Create(Create(new Texture2D(4, 4)), new Rect(0f, 0f, 4f, 4f), Vector2.one * 0.5f);
            _millData.Init("station_mill", new[] { process }, 10, CreateBubblePrefab(), _hourglass);

            var bagObject = Create(new GameObject("Bag"));
            bagObject.SetActive(false);
            var bagConfig = Create(ScriptableObject.CreateInstance<InventoryConfig>());
            JsonUtility.FromJsonOverwrite("{\"id\":\"inventory_player\",\"slotCount\":4}", bagConfig);
            var items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            items.Init(new[] { _wheat, _flour, _hoe });
            _bag = bagObject.AddComponent<InventoryHolder>();
            _bag.Init(bagConfig, items, _bus);
            bagObject.SetActive(true);
            _bag.Inventory.Add(_wheat, 12);
            _bag.Inventory.Add(_hoe, 1);

            _player = Create(new GameObject("Player")).transform;
            _player.position = new Vector3(0.5f, 0f, 0.5f);
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
        }

        [UnityTest]
        public IEnumerator Click_WithWheat_LoadsUpToCapacity()
        {
            var mill = CreateMill();
            Select(WheatSlot);
            yield return null;

            _bus.Publish(new GroundClicked(MillPosition));

            Assert.AreEqual(10, mill.State.Batches);
            Assert.AreEqual(2, _bag.Inventory.Count(_wheat));
            Assert.IsTrue(mill.State.IsWorking);
            Assert.IsTrue(mill.BubbleVisible, "The working bubble should show.");
            Assert.AreEqual(_hourglass, mill.BubbleIcon);
        }

        [UnityTest]
        public IEnumerator TimePasses_ThenClick_CollectsTheFlour()
        {
            var mill = CreateMill();
            var crafted = new List<ItemCrafted>();
            _bus.Subscribe<ItemCrafted>(message => crafted.Add(message));
            Select(WheatSlot);
            yield return null;
            _bus.Publish(new TimeChanged(Morning));
            mill.Use();

            _bus.Publish(new TimeChanged(Morning + 30));
            Assert.IsFalse(mill.State.IsReady, "The flour was ready too early.");
            _bus.Publish(new TimeChanged(Morning + 60));
            Assert.IsTrue(mill.State.IsReady);
            Assert.IsTrue(mill.BubbleVisible, "The done bubble should show.");
            Assert.AreNotEqual(_hourglass, mill.BubbleIcon, "The bubble should show the output, not the hourglass.");

            Select(OtherSlot);
            Assert.IsTrue(mill.Use(), "Collecting works whatever is in hand.");

            Assert.AreEqual(10, _bag.Inventory.Count(_flour));
            Assert.IsTrue(mill.State.IsIdle);
            Assert.IsFalse(mill.BubbleVisible, "An empty mill shows no bubble.");
            Assert.AreEqual(1, crafted.Count);
            Assert.AreEqual(("station_mill", "ingredient_flour", 10), (crafted[0].StationId, crafted[0].ItemId, crafted[0].Quantity));
        }

        [UnityTest]
        public IEnumerator Click_WhileWorking_AddsNothing()
        {
            var mill = CreateMill();
            Select(WheatSlot);
            yield return null;
            _bag.Inventory.Remove(_wheat, 9);
            mill.Use();

            Assert.IsFalse(mill.Use());
            Assert.AreEqual(3, mill.State.Batches);
        }

        [UnityTest]
        public IEnumerator Click_WithSomethingElse_DoesNothing()
        {
            var mill = CreateMill();
            Select(OtherSlot);
            yield return null;

            Assert.IsFalse(mill.Use());
            Assert.IsTrue(mill.State.IsIdle);
            Assert.AreEqual(12, _bag.Inventory.Count(_wheat));
        }

        [UnityTest]
        public IEnumerator Click_PlayerTooFar_IsIgnored()
        {
            var mill = CreateMill();
            Select(WheatSlot);
            _player.position = new Vector3(9f, 0f, 9f);
            yield return null;

            _bus.Publish(new GroundClicked(MillPosition));

            Assert.IsTrue(mill.State.IsIdle);
        }

        [UnityTest]
        public IEnumerator Night_UnfinishedBatch_IsReadyInTheMorning()
        {
            var mill = CreateMill();
            Select(WheatSlot);
            yield return null;
            mill.Use();

            _bus.Publish(new NightPassed(1));

            Assert.IsTrue(mill.State.IsReady);
        }

        [UnityTest]
        public IEnumerator Collect_BagFull_FlourWaitsInTheMill()
        {
            var mill = CreateMill();
            Select(WheatSlot);
            yield return null;
            mill.Use();
            _bus.Publish(new NightPassed(1));
            var pebble = CreateItem("{\"id\":\"pebble\",\"maxStack\":1}");
            _bag.Inventory.Add(pebble, 2);

            Assert.IsFalse(mill.Use());
            Assert.IsTrue(mill.State.IsReady);
            Assert.AreEqual(0, _bag.Inventory.Count(_flour));
        }

        [UnityTest]
        public IEnumerator SaveThenLoad_Batch_IsRestored()
        {
            var mill = CreateMill();
            Select(WheatSlot);
            yield return null;
            _bus.Publish(new TimeChanged(Morning));
            mill.Use();
            _bus.Publish(new TimeChanged(Morning + 20));
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            _bus.Publish(new GameSaving(data));
            Object.Destroy(mill.gameObject);
            yield return null;

            var reloaded = CreateMill();
            data.inventories.Add(_bag.Inventory.ToSave("inventory_player"));
            _bus.Publish(new GameLoaded(data));

            Assert.AreEqual(10, reloaded.State.Batches);
            Assert.AreEqual(40, reloaded.State.MinutesLeft);
            Assert.IsTrue(reloaded.BubbleVisible, "A restored batch shows its bubble.");
        }

        // Clicking the bubble floating above the mill uses the mill, like clicking its base.
        [UnityTest]
        public IEnumerator TargetClicked_OnTheBubble_CollectsTheFlour()
        {
            var mill = CreateMill();
            Select(WheatSlot);
            yield return null;
            mill.Use();
            _bus.Publish(new NightPassed(1));

            _bus.Publish(new TargetClicked(mill.GetComponentInChildren<ClickTarget>()));

            Assert.AreEqual(10, _bag.Inventory.Count(_flour));
        }

        [UnityTest]
        public IEnumerator TargetClicked_OtherObject_IsIgnored()
        {
            var mill = CreateMill();
            Select(WheatSlot);
            var other = Create(new GameObject("Other")).AddComponent<ClickTarget>();
            yield return null;

            _bus.Publish(new TargetClicked(other));

            Assert.IsTrue(mill.State.IsIdle);
        }

        private Station CreateMill()
        {
            var millObject = Create(new GameObject("Mill"));
            millObject.SetActive(false);
            millObject.transform.position = MillPosition;
            var mill = millObject.AddComponent<Station>();
            mill.Init(_millData, _bus, _bag, _player);
            millObject.SetActive(true);
            return mill;
        }

        // Like the real bubble: a click target with a child named Icon.
        private GameObject CreateBubblePrefab()
        {
            var bubble = Create(new GameObject("Bubble"));
            bubble.SetActive(false);
            bubble.AddComponent<BoxCollider>().isTrigger = true;
            bubble.AddComponent<ClickTarget>();
            var icon = new GameObject("Icon");
            icon.transform.SetParent(bubble.transform, false);
            icon.AddComponent<SpriteRenderer>();
            return bubble;
        }

        private void Select(int slot)
        {
            _bus.Publish(new HotbarSlotSelected(slot));
        }

        private ItemData CreateItem(string json)
        {
            var item = Create(ScriptableObject.CreateInstance<ItemData>());
            JsonUtility.FromJsonOverwrite(json, item);
            return item;
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
