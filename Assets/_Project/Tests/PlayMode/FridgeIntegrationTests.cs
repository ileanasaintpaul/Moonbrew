using System.Collections;
using System.Collections.Generic;
using Moonbrew.Cafe;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // A click on the fridge from close by opens its panel, whose slots move items to the owner's café inventory and back.
    public class FridgeIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private readonly List<bool> _toggles = new List<bool>();
        private EventBusAsset _bus;
        private InventoryHolder _fridge;
        private InventoryHolder _owner;
        private FridgePanel _panel;
        private ClickTarget _target;
        private Transform _player;
        private ItemData _flour;

        [SetUp]
        public void SetUp()
        {
#if UNITY_EDITOR
            var panelTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/FridgePanel.uxml");
            var slotTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/HotbarSlot.uxml");
#else
            Assert.Ignore("The panel's templates are loaded from the editor asset database.");
            VisualTreeAsset panelTemplate = null;
            VisualTreeAsset slotTemplate = null;
#endif
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _bus.Subscribe<GameMenuToggled>(message => _toggles.Add(message.Open));
            _flour = Create(ScriptableObject.CreateInstance<ItemData>());
            var catalog = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            var inventoryConfig = Create(ScriptableObject.CreateInstance<InventoryConfig>());
            var cafeConfig = Create(ScriptableObject.CreateInstance<CafeConfig>());

            var holders = Create(new GameObject("Holders"));
            holders.SetActive(false);
            _fridge = holders.AddComponent<InventoryHolder>();
            _fridge.Init(inventoryConfig, catalog, _bus);
            _owner = holders.AddComponent<InventoryHolder>();
            _owner.Init(inventoryConfig, catalog, _bus);
            holders.SetActive(true);
            _fridge.Inventory.Add(_flour, 5);

            _player = Create(new GameObject("Player")).transform;
            var fridgeObject = Create(new GameObject("Fridge"));
            fridgeObject.SetActive(false);
            _target = fridgeObject.AddComponent<ClickTarget>();
            fridgeObject.AddComponent<CafeFridge>().Init(cafeConfig, _bus, _fridge, _owner, _player);
            fridgeObject.SetActive(true);

            var hud = Create(new GameObject("HUD"));
            hud.SetActive(false);
            hud.AddComponent<UIDocument>().panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
            _panel = hud.AddComponent<FridgePanel>();
            hud.SetActive(true);
            _panel.Init(_bus, _fridge, _owner, panelTemplate, slotTemplate);
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
            _toggles.Clear();
        }

        [UnityTest]
        public IEnumerator Click_OwnerNearby_OpensThePanelAndPauses()
        {
            yield return null;

            _bus.Publish(new TargetClicked(_target));

            Assert.IsTrue(_panel.IsOpen);
            CollectionAssert.AreEqual(new[] { true }, _toggles);
            Assert.AreEqual("5", _panel.CountText(true, 0));
        }

        [UnityTest]
        public IEnumerator Click_OwnerTooFar_DoesNothing()
        {
            yield return null;
            _player.position = new Vector3(10f, 0f, 0f);

            _bus.Publish(new TargetClicked(_target));

            Assert.IsFalse(_panel.IsOpen);
        }

        [UnityTest]
        public IEnumerator Click_AnotherObject_DoesNothing()
        {
            yield return null;
            var other = Create(new GameObject("Other")).AddComponent<ClickTarget>();

            _bus.Publish(new TargetClicked(other));

            Assert.IsFalse(_panel.IsOpen);
        }

        [UnityTest]
        public IEnumerator ClickSlot_WholeStack_MovesToTheOwnerAndBack()
        {
            yield return null;
            _bus.Publish(new TargetClicked(_target));

            _panel.ClickSlot(true, 0, true);

            Assert.AreEqual(0, _fridge.Inventory.Count(_flour));
            Assert.AreEqual(5, _owner.Inventory.Count(_flour));
            Assert.AreEqual(string.Empty, _panel.CountText(true, 0), "The panel follows the move.");
            Assert.AreEqual("5", _panel.CountText(false, 0));

            _panel.ClickSlot(false, 0, true);

            Assert.AreEqual(5, _fridge.Inventory.Count(_flour));
            Assert.AreEqual(0, _owner.Inventory.Count(_flour));
        }

        [UnityTest]
        public IEnumerator ClickSlot_OneItem_LeavesTheRest()
        {
            yield return null;
            _bus.Publish(new TargetClicked(_target));

            _panel.ClickSlot(true, 0, false);

            Assert.AreEqual(4, _fridge.Inventory.Count(_flour));
            Assert.AreEqual(1, _owner.Inventory.Count(_flour));
        }

        [UnityTest]
        public IEnumerator ClickSlot_OutOfRange_ChangesNothing()
        {
            yield return null;

            _bus.Publish(new FridgeMoveRequested(true, 999, true));
            _bus.Publish(new FridgeMoveRequested(false, -1, true));

            Assert.AreEqual(5, _fridge.Inventory.Count(_flour));
        }

        [UnityTest]
        public IEnumerator Close_ResumesTheGame()
        {
            yield return null;
            _bus.Publish(new TargetClicked(_target));

            _panel.Close();

            Assert.IsFalse(_panel.IsOpen);
            CollectionAssert.AreEqual(new[] { true, false }, _toggles);
        }

        [UnityTest]
        public IEnumerator AnotherMenuOpens_ClosesThePanelAndKeepsThePause()
        {
            yield return null;
            _bus.Publish(new TargetClicked(_target));

            _bus.Publish(new GameMenuToggled(true));

            Assert.IsFalse(_panel.IsOpen);
            CollectionAssert.AreEqual(new[] { true, true }, _toggles, "The other menu now holds the pause.");
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
