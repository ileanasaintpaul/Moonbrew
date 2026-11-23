using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // The HUD keeps a reminder of each order taken, and drops it when the table leaves.
    public class OrderHudIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private OrderHud _hud;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            var catalog = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            var panel = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var hudObject = Create(new GameObject("HUD"));
            hudObject.SetActive(false);
            hudObject.AddComponent<UIDocument>().panelSettings = panel;
            _hud = hudObject.AddComponent<OrderHud>();
            hudObject.SetActive(true);
            _hud.Init(_bus, catalog);
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
        public IEnumerator OrderTaken_ShowsARowWithEachDish()
        {
            yield return null;

            _bus.Publish(new OrderTaken(2, new[] { "dish_cake", "dish_juice" }));

            Assert.AreEqual(1, _hud.RowCount);
            Assert.AreEqual(2, _hud.IconCount(0));
        }

        [UnityTest]
        public IEnumerator OrderAbandoned_RemovesTheRow()
        {
            yield return null;
            _bus.Publish(new OrderTaken(0, new[] { "dish_cake" }));
            _bus.Publish(new OrderTaken(1, new[] { "dish_juice" }));

            _bus.Publish(new OrderAbandoned(0));

            Assert.AreEqual(1, _hud.RowCount);
        }

        [UnityTest]
        public IEnumerator OrderAbandoned_UnknownTable_ChangesNothing()
        {
            yield return null;
            _bus.Publish(new OrderTaken(0, new[] { "dish_cake" }));

            _bus.Publish(new OrderAbandoned(5));

            Assert.AreEqual(1, _hud.RowCount);
        }

        [UnityTest]
        public IEnumerator InventoryChanged_DishGathered_ShowsWhatIsStillMissing()
        {
            yield return null;
            var owner = OwnerInventory("inventory_owner_cafe");
            var slice = Item("dish_slice");
            var juice = Item("dish_juice");
            _hud.Init(_bus, Create(ScriptableObject.CreateInstance<ItemCatalog>()), owner);
            _bus.Publish(new OrderTaken(0, new[] { "dish_slice", "dish_slice", "dish_juice" }));

            Assert.IsTrue(_hud.IsDishMissing(0, 0));
            Assert.IsFalse(_hud.IsOrderReady(0));

            owner.Inventory.Add(slice, 1);

            Assert.IsFalse(_hud.IsDishMissing(0, 0));
            Assert.IsTrue(_hud.IsDishMissing(0, 1), "Two identical dishes need two items.");
            Assert.IsTrue(_hud.IsDishMissing(0, 2));

            owner.Inventory.Add(slice, 1);
            owner.Inventory.Add(juice, 1);

            Assert.IsTrue(_hud.IsOrderReady(0));

            owner.Inventory.Remove(juice, 1);

            Assert.IsTrue(_hud.IsDishMissing(0, 2));
            Assert.IsFalse(_hud.IsOrderReady(0));
        }

        [UnityTest]
        public IEnumerator InventoryChanged_AnotherInventory_ChangesNothing()
        {
            yield return null;
            var owner = OwnerInventory("inventory_owner_cafe");
            var other = OwnerInventory("inventory_cafe");
            _hud.Init(_bus, Create(ScriptableObject.CreateInstance<ItemCatalog>()), owner);
            _bus.Publish(new OrderTaken(0, new[] { "dish_juice" }));

            other.Inventory.Add(Item("dish_juice"), 1);

            Assert.IsTrue(_hud.IsDishMissing(0, 0));
        }

        private InventoryHolder OwnerInventory(string id)
        {
            var config = Create(ScriptableObject.CreateInstance<InventoryConfig>());
            SetId(config, id);
            var holderObject = Create(new GameObject(id));
            holderObject.SetActive(false);
            var holder = holderObject.AddComponent<InventoryHolder>();
            holder.Init(config, Create(ScriptableObject.CreateInstance<ItemCatalog>()), _bus);
            holderObject.SetActive(true);
            return holder;
        }

        private ItemData Item(string id)
        {
            var item = Create(ScriptableObject.CreateInstance<ItemData>());
            SetId(item, id);
            return item;
        }

        private static void SetId(Object asset, string id)
        {
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(asset);
            serialized.FindProperty("id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#else
            Assert.Ignore("IDs are set through the editor's serialization.");
#endif
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
