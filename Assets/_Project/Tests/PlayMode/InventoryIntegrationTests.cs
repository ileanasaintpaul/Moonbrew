using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Player;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // An inventory announces its changes on the bus, and the debug HUD shows its contents.
    public class InventoryIntegrationTests
    {
        private const string SlotTemplatePath = "Assets/_Project/Art/Ui/HotbarSlot.uxml";

        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private ItemData _item;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _item = Create(ScriptableObject.CreateInstance<ItemData>());
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
        public IEnumerator Holder_NewGame_AddsStartingItemsAndPublishes()
        {
            var received = new List<InventoryChanged>();
            _bus.Subscribe<InventoryChanged>(message => received.Add(message));

            var holder = CreateHolder(new[] { new ItemStack(_item, 3) });
            _bus.Publish(new NewGameStarted());
            yield return null;

            Assert.AreEqual(3, holder.Inventory.Count(_item));
            Assert.AreEqual(1, received.Count);
            Assert.AreEqual(3, received[0].Quantity);
        }

        [UnityTest]
        public IEnumerator HotbarHud_Start_ShowsTenSlotsWithTheFirstSelected()
        {
            var hud = CreateHotbarHud(CreateHolder(null), out _);
            yield return null;

            Assert.AreEqual(10, hud.VisibleSlots);
            Assert.AreEqual(0, hud.HighlightedIndex);
        }

        [UnityTest]
        public IEnumerator HotbarHud_ItemAddedThenRemoved_ShowsCurrentCount()
        {
            var holder = CreateHolder(null);
            var hud = CreateHotbarHud(holder, out _);
            yield return null;

            holder.Inventory.Add(_item, 5);
            yield return null;
            Assert.AreEqual("5", hud.CountText(0));

            holder.Inventory.Remove(_item, 5);
            yield return null;
            Assert.AreEqual(string.Empty, hud.CountText(0));
        }

        [UnityTest]
        public IEnumerator HotbarHud_SlotSelected_MovesTheHighlight()
        {
            var hud = CreateHotbarHud(CreateHolder(null), out var hotbar);
            yield return null;

            hotbar.Select(3);
            yield return null;

            Assert.AreEqual(3, hud.HighlightedIndex);
        }

        [UnityTest]
        public IEnumerator HotbarHud_SlotClicked_SelectsThatSlot()
        {
            var hud = CreateHotbarHud(CreateHolder(null), out var hotbar);
            yield return null;

            hud.ClickSlot(6);
            yield return null;

            Assert.AreEqual(6, hotbar.Hotbar.SelectedIndex);
            Assert.AreEqual(6, hud.HighlightedIndex);
        }

        private InventoryHolder CreateHolder(ItemStack[] items)
        {
            var config = Create(ScriptableObject.CreateInstance<InventoryConfig>());
            var holderObject = Create(new GameObject("Inventory"));
            holderObject.SetActive(false);
            var holder = holderObject.AddComponent<InventoryHolder>();
            holder.Init(config, null, _bus, items);
            holderObject.SetActive(true);
            return holder;
        }

        private HotbarHud CreateHotbarHud(InventoryHolder bag, out PlayerHotbar hotbar)
        {
            var playerObject = Create(new GameObject("Player"));
            playerObject.SetActive(false);
            hotbar = playerObject.AddComponent<PlayerHotbar>();
            hotbar.Init(Create(ScriptableObject.CreateInstance<PlayerConfig>()), _bus);
            playerObject.SetActive(true);

            var hudObject = Create(new GameObject("Hotbar HUD"));
            hudObject.SetActive(false);
            var document = hudObject.AddComponent<UIDocument>();
            document.panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var hud = hudObject.AddComponent<HotbarHud>();
            hud.Init(_bus, hotbar, bag, LoadSlotTemplate());
            hudObject.SetActive(true);
            return hud;
        }

        private static VisualTreeAsset LoadSlotTemplate()
        {
#if UNITY_EDITOR
            var template = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(SlotTemplatePath);
            Assert.IsNotNull(template, $"Slot template not found at {SlotTemplatePath}.");
            return template;
#else
            Assert.Ignore("The slot template is loaded from the editor asset database.");
            return null;
#endif
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
