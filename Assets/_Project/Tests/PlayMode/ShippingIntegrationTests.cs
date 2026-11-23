using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // Ingredients dropped in the crate reach the fridge overnight; dropped at the pantry, they are there at once.
    public class ShippingIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private InventoryHolder _bag;
        private InventoryHolder _crate;
        private InventoryHolder _fridge;
        private ItemData _wheat;
        private Transform _player;
        private GameObject _cratePoint;
        private GameObject _pantryPoint;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _wheat = Create(ScriptableObject.CreateInstance<ItemData>());
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(_wheat);
            serialized.FindProperty("id").stringValue = "crop_wheat";
            serialized.FindProperty("category").enumValueIndex = (int)ItemCategory.Crop;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
            var items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            items.Init(new[] { _wheat });
            _player = Create(new GameObject("Player")).transform;

            var world = Create(new GameObject("World"));
            world.SetActive(false);
            _bag = Holder(world, items, "inventory_bag");
            _crate = Holder(world, items, "inventory_crate");
            _fridge = Holder(world, items, "inventory_fridge");
            world.AddComponent<OvernightDelivery>().Init(_crate, _fridge, _bus);
            _cratePoint = DropPoint("Crate", world.transform, _crate);
            _pantryPoint = DropPoint("Pantry", world.transform, _fridge);
            world.SetActive(true);
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
        public IEnumerator Crate_Clicked_DeliversNextMorningOnly()
        {
            yield return null;
            _bag.Inventory.Add(_wheat, 3);

            Click(_cratePoint);
            Assert.AreEqual(3, _crate.Inventory.Count(_wheat));
            Assert.AreEqual(0, _fridge.Inventory.Count(_wheat));

            _bus.Publish(new NightPassed(1));

            Assert.AreEqual(0, _crate.Inventory.Count(_wheat));
            Assert.AreEqual(3, _fridge.Inventory.Count(_wheat));
        }

        [UnityTest]
        public IEnumerator Pantry_Clicked_FillsTheFridgeAtOnce()
        {
            yield return null;
            _bag.Inventory.Add(_wheat, 2);

            Click(_pantryPoint);

            Assert.AreEqual(2, _fridge.Inventory.Count(_wheat));
            Assert.AreEqual(0, _bag.Inventory.Count(_wheat));
        }

        [UnityTest]
        public IEnumerator DropPoint_OwnerTooFar_MovesNothing()
        {
            yield return null;
            _bag.Inventory.Add(_wheat, 2);
            _player.position = new Vector3(50f, 0f, 0f);

            Click(_pantryPoint);

            Assert.AreEqual(2, _bag.Inventory.Count(_wheat));
        }

        [UnityTest]
        public IEnumerator Crate_SavedAndLoaded_KeepsItsContent()
        {
            yield return null;
            _bag.Inventory.Add(_wheat, 4);
            Click(_cratePoint);
            var save = SaveData.CreateNew("slot_a", 1);
            _bus.Publish(new GameSaving(save));
            _crate.Inventory.Remove(_wheat, 4);

            _bus.Publish(new GameLoaded(save));

            Assert.AreEqual(4, _crate.Inventory.Count(_wheat));
        }

        private void Click(GameObject point)
        {
            _bus.Publish(new TargetClicked(point.GetComponent<ClickTarget>()));
        }

        private GameObject DropPoint(string pointName, Transform parent, InventoryHolder stock)
        {
            var point = new GameObject(pointName);
            point.transform.SetParent(parent);
            point.AddComponent<ClickTarget>();
            point.AddComponent<IngredientDrop>().Init(_bag, stock, _player, _bus);
            return point;
        }

        private InventoryHolder Holder(GameObject world, ItemCatalog items, string id)
        {
            var config = Create(ScriptableObject.CreateInstance<InventoryConfig>());
#if UNITY_EDITOR
            var serialized = new UnityEditor.SerializedObject(config);
            serialized.FindProperty("id").stringValue = id;
            serialized.ApplyModifiedPropertiesWithoutUndo();
#endif
            var holder = world.AddComponent<InventoryHolder>();
            holder.Init(config, items, _bus);
            return holder;
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
