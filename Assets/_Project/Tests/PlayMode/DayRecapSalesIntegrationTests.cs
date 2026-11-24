using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Economy;
using Moonbrew.Inventory;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // The café's sales of the day reach the save's recap, and the morning recap lists them.
    public class DayRecapSalesIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private ItemCatalog _items;
        private DayPanels _panels;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            _items.Init(new[] { Dish("dish_cake", 10), Dish("dish_juice", 20) });

            var bookObject = Create(new GameObject("Sales"));
            bookObject.SetActive(false);
            bookObject.AddComponent<SalesBook>().Init(_items, _bus);
            bookObject.SetActive(true);

            var panel = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var hudObject = Create(new GameObject("HUD"));
            hudObject.SetActive(false);
            hudObject.AddComponent<UIDocument>().panelSettings = panel;
            _panels = hudObject.AddComponent<DayPanels>();
            hudObject.SetActive(true);
#if UNITY_EDITOR
            var recap = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/DayRecap.uxml");
#else
            VisualTreeAsset recap = null;
#endif
            _panels.Init(_bus, _items, new InventoryConfig[0], null, recap);
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
        public IEnumerator OrdersServed_ReachTheSavedRecap()
        {
            yield return null;
            _bus.Publish(new OrderServed(0, new[] { "dish_cake", "dish_juice" }, 30));
            _bus.Publish(new OrderServed(1, new[] { "dish_cake" }, 10, true));
            var save = SaleSave();

            _bus.Publish(new GameSaving(save));

            Assert.AreEqual(30, RecapSales.TotalOf(save.recap, false).Money);
            Assert.AreEqual((1, 10), (RecapSales.TotalOf(save.recap, true).Customers, RecapSales.TotalOf(save.recap, true).Money));
        }

        [UnityTest]
        public IEnumerator SavedTwice_CountsEachSaleOnce()
        {
            yield return null;
            _bus.Publish(new OrderServed(0, new[] { "dish_cake" }, 10));
            var save = SaleSave();
            _bus.Publish(new GameSaving(save));

            _bus.Publish(new GameSaving(save));

            Assert.AreEqual(10, RecapSales.Earned(save.recap));
        }

        [UnityTest]
        public IEnumerator RecapOpened_ListsTheSales()
        {
            yield return null;
            var recap = new DayRecap { day = 3 };
            recap.AddSale("dish_cake", 25, false);
            recap.AddSale("dish_juice", 20, true);

            _bus.Publish(new DayRecapOpened(recap));

            Assert.AreEqual(2, _panels.RecapDishRows);
            StringAssert.Contains("45", _panels.RecapTotal);
            StringAssert.Contains("1 customers, 25", _panels.RecapByOwner);
            StringAssert.Contains("1 customers, 20", _panels.RecapByEmployee);
        }

        private static SaveData SaleSave()
        {
            return SaveData.CreateNew("slot_a", 1);
        }

        private ItemData Dish(string id, int price)
        {
            var item = Create(ScriptableObject.CreateInstance<ItemData>());
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(item);
            serialized.FindProperty("id").stringValue = id;
            serialized.FindProperty("basePrice").intValue = price;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
            return item;
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
