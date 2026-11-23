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

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
