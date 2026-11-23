using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // A click on the display case opens its panel: a cake put in gives its portions, taken out one at a time.
    public class DisplayCaseIntegrationTests
    {
        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private InventoryHolder _display;
        private InventoryHolder _owner;
        private FridgePanel _panel;
        private ClickTarget _target;
        private Transform _player;
        private ItemData _cake;
        private ItemData _slice;

        [SetUp]
        public void SetUp()
        {
#if UNITY_EDITOR
            var fridgeTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/FridgePanel.uxml");
            var displayTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/DisplayPanel.uxml");
            var slotTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/HotbarSlot.uxml");
#else
            Assert.Ignore("The panel's templates are loaded from the editor asset database.");
            VisualTreeAsset fridgeTemplate = null;
            VisualTreeAsset displayTemplate = null;
            VisualTreeAsset slotTemplate = null;
#endif
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _cake = Create(ScriptableObject.CreateInstance<ItemData>());
            _slice = Create(ScriptableObject.CreateInstance<ItemData>());
            var recipe = Create(ScriptableObject.CreateInstance<RecipeData>());
            recipe.Init("recipe_cake", new Ingredient[0], _cake, 1, 30, true);
            recipe.InitPortions(_slice, 6);
            var recipes = Create(ScriptableObject.CreateInstance<RecipeCatalog>());
            recipes.Init(new[] { recipe });
            var catalog = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            var inventoryConfig = Create(ScriptableObject.CreateInstance<InventoryConfig>());
            var caseData = Create(ScriptableObject.CreateInstance<DisplayCaseData>());

            var holders = Create(new GameObject("Holders"));
            holders.SetActive(false);
            _display = holders.AddComponent<InventoryHolder>();
            _display.Init(inventoryConfig, catalog, _bus);
            _owner = holders.AddComponent<InventoryHolder>();
            _owner.Init(inventoryConfig, catalog, _bus);
            var fridge = holders.AddComponent<InventoryHolder>();
            fridge.Init(inventoryConfig, catalog, _bus);
            holders.SetActive(true);
            _owner.Inventory.Add(_cake, 1);

            _player = Create(new GameObject("Player")).transform;
            var caseObject = Create(new GameObject("Display Case"));
            caseObject.SetActive(false);
            _target = caseObject.AddComponent<ClickTarget>();
            caseObject.AddComponent<CafeDisplayCase>().Init(caseData, _bus, _display, _owner, recipes, _player);
            caseObject.SetActive(true);

            var hud = Create(new GameObject("HUD"));
            hud.SetActive(false);
            hud.AddComponent<UIDocument>().panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
            _panel = hud.AddComponent<FridgePanel>();
            hud.SetActive(true);
            _panel.Init(_bus, fridge, _owner, fridgeTemplate, slotTemplate, _display, displayTemplate);
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
        public IEnumerator Click_OwnerNearby_OpensThePanel()
        {
            yield return null;

            _bus.Publish(new TargetClicked(_target));

            Assert.IsTrue(_panel.IsOpen);
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
        public IEnumerator ClickSlots_PlaceThenTakeEveryPortion_EmptiesTheCase()
        {
            yield return null;
            _bus.Publish(new TargetClicked(_target));

            _panel.ClickSlot(false, 0, true);

            Assert.AreEqual(6, _display.Inventory.Count(_slice));
            Assert.AreEqual("6", _panel.CountText(true, 0), "The panel follows the move.");

            for (var i = 0; i < 7; i++)
            {
                _panel.ClickSlot(true, 0, true);
            }

            Assert.AreEqual(0, _display.Inventory.Count(_slice));
            Assert.AreEqual(6, _owner.Inventory.Count(_slice));
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
