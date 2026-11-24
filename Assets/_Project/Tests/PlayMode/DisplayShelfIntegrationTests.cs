using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // What sits in the display case can be seen in it: a few models per slot, gone when the slot empties.
    public class DisplayShelfIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private InventoryHolder _stock;
        private DisplayShelf _shelf;
        private ItemData _slice;

        [SetUp]
        public void SetUp()
        {
            var bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            var model = Create(new GameObject("Slice Model"));
            _slice = Create(ScriptableObject.CreateInstance<ItemData>());
            JsonUtility.FromJsonOverwrite("{\"id\":\"dish_slice\",\"maxStack\":6}", _slice);
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(_slice);
            serialized.FindProperty("worldModel").objectReferenceValue = model;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
            var items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            items.Init(new[] { _slice });
            var config = Create(ScriptableObject.CreateInstance<InventoryConfig>());
            JsonUtility.FromJsonOverwrite("{\"id\":\"inventory_case\",\"slotCount\":2}", config);

            var root = Create(new GameObject("Case"));
            root.SetActive(false);
            _stock = root.AddComponent<InventoryHolder>();
            _stock.Init(config, items, bus);
            var spots = new[] { new GameObject("Spot 1").transform, new GameObject("Spot 2").transform };
            foreach (var spot in spots)
            {
                spot.SetParent(root.transform);
            }

            _shelf = root.AddComponent<DisplayShelf>();
            _shelf.Init(_stock, bus, spots);
            root.SetActive(true);
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
        public IEnumerator Slices_PutInTheCase_AreShownUpToThree()
        {
            yield return null;

            _stock.Inventory.Add(_slice, 6);

            Assert.AreEqual(3, _shelf.ShownCount(0));
            Assert.AreEqual(0, _shelf.ShownCount(1));
        }

        [UnityTest]
        public IEnumerator SlotEmptied_ShowsNothing()
        {
            yield return null;
            _stock.Inventory.Add(_slice, 2);

            _stock.Inventory.Remove(_slice, 2);

            Assert.AreEqual(0, _shelf.ShownCount(0));
        }

        [TestCase(0, 3, 0)]
        [TestCase(2, 3, 2)]
        [TestCase(9, 3, 3)]
        [TestCase(-1, 3, 0)]
        public void Shown_Quantity_IsCappedAtTheMaximum(int quantity, int max, int shown)
        {
            Assert.AreEqual(shown, DisplayShelf.Shown(quantity, max));
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
