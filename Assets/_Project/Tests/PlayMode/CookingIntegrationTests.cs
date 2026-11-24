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
    // Cooking at the café oven: the menu opens on the recipes, a known recipe with its ingredients
    // cooks and uses them, and a recipe to buy cannot be cooked until it is learned.
    public class CookingIntegrationTests
    {
        private const string OvenId = "station_oven";
        private const int WheatCakeRow = 0;
        private const int BerryCakeRow = 1;

        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private ItemData _flour;
        private ItemData _berry;
        private ItemData _wheatCake;
        private RecipeData _wheatCakeRecipe;
        private RecipeData _berryCakeRecipe;
        private InventoryHolder _bag;
        private RecipeBookHolder _book;
        private Station _oven;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _flour = CreateItem("{\"id\":\"ingredient_flour\",\"maxStack\":99}");
            _berry = CreateItem("{\"id\":\"crop_strawberry\",\"maxStack\":99}");
            _wheatCake = CreateItem("{\"id\":\"dish_wheat_cake\",\"maxStack\":20}");
            var berryCake = CreateItem("{\"id\":\"dish_strawberry_cake\",\"maxStack\":20}");

            _wheatCakeRecipe = Create(ScriptableObject.CreateInstance<RecipeData>());
            _wheatCakeRecipe.Init("recipe_wheat_cake", new[] { new Ingredient(_flour, 2) }, _wheatCake, 1, 30, true);
            _berryCakeRecipe = Create(ScriptableObject.CreateInstance<RecipeData>());
            _berryCakeRecipe.Init("recipe_strawberry_cake", new[] { new Ingredient(_flour, 2), new Ingredient(_berry, 3) }, berryCake, 2, 45, false);
            var catalog = Create(ScriptableObject.CreateInstance<RecipeCatalog>());
            catalog.Init(new[] { _wheatCakeRecipe, _berryCakeRecipe });

            var bagObject = Create(new GameObject("Bag"));
            bagObject.SetActive(false);
            var bagConfig = Create(ScriptableObject.CreateInstance<InventoryConfig>());
            JsonUtility.FromJsonOverwrite("{\"id\":\"inventory_player\",\"slotCount\":10}", bagConfig);
            _bag = bagObject.AddComponent<InventoryHolder>();
            var items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            items.Init(new[] { _flour, _berry, _wheatCake, berryCake });
            _bag.Init(bagConfig, items, _bus);
            _book = bagObject.AddComponent<RecipeBookHolder>();
            _book.Init(catalog, _bus);
            bagObject.SetActive(true);
            _bag.Inventory.Add(_flour, 4);
            _bag.Inventory.Add(_berry, 3);

            var ovenData = Create(ScriptableObject.CreateInstance<StationData>());
            ovenData.Init(OvenId, null, 1, null, null, new[] { _wheatCakeRecipe, _berryCakeRecipe });
            var player = Create(new GameObject("Player")).transform;
            var ovenObject = Create(new GameObject("Oven"));
            ovenObject.SetActive(false);
            _oven = ovenObject.AddComponent<Station>();
            _oven.Init(ovenData, _bus, _bag, player, _book);
            ovenObject.SetActive(true);
            yield return null;
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
        public IEnumerator Oven_IdleClick_OpensTheRecipeMenu()
        {
            var opened = new List<string>();
            _bus.Subscribe<StoveOpened>(message => opened.Add(message.StationId));

            Assert.IsTrue(_oven.Use());
            yield return null;

            CollectionAssert.AreEqual(new[] { OvenId }, opened);
        }

        [UnityTest]
        public IEnumerator Cook_KnownRecipe_UsesIngredientsThenGivesTheDish()
        {
            Assert.IsTrue(_oven.Cook(_wheatCakeRecipe));
            Assert.AreEqual(2, _bag.Inventory.Count(_flour));
            Assert.IsTrue(_oven.State.IsWorking);

            _bus.Publish(new NightPassed(1));
            Assert.IsTrue(_oven.Use());
            yield return null;

            Assert.AreEqual(1, _bag.Inventory.Count(_wheatCake));
            Assert.IsTrue(_oven.State.IsIdle);
        }

        [UnityTest]
        public IEnumerator Cook_RecipeToBuy_IsRefusedUntilLearned()
        {
            Assert.IsFalse(_oven.Cook(_berryCakeRecipe));
            Assert.AreEqual(4, _bag.Inventory.Count(_flour), "Nothing should be used.");

            Assert.IsTrue(_book.Learn(_berryCakeRecipe));
            Assert.IsTrue(_oven.Cook(_berryCakeRecipe));
            Assert.AreEqual(0, _bag.Inventory.Count(_berry));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Cook_MissingIngredients_IsRefused()
        {
            _bag.Inventory.Remove(_flour, 3);

            Assert.IsFalse(_oven.Cook(_wheatCakeRecipe));
            Assert.AreEqual(1, _bag.Inventory.Count(_flour));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RecipeChosen_ForThisOven_Cooks()
        {
            _bus.Publish(new RecipeChosen(OvenId, _wheatCakeRecipe.Id));
            _bus.Publish(new RecipeChosen("station_other", _wheatCakeRecipe.Id));
            yield return null;

            Assert.IsTrue(_oven.State.IsWorking);
            Assert.AreEqual(2, _bag.Inventory.Count(_flour), "Only this oven should have cooked.");
        }

        [UnityTest]
        public IEnumerator SaveThenLoad_LearnedRecipe_IsStillKnown()
        {
            _book.Learn(_berryCakeRecipe);
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            _bus.Publish(new GameSaving(data));
            _book.Book.Restore(null);
            Assert.IsFalse(_book.Book.IsKnown(_berryCakeRecipe));

            _bus.Publish(new GameLoaded(data));
            yield return null;

            Assert.IsTrue(_book.Book.IsKnown(_berryCakeRecipe));
        }

        [UnityTest]
        public IEnumerator Menu_OpenedByTheOven_CooksTheChosenRecipe()
        {
            var menu = CreateMenu();
            var toggles = new List<bool>();
            _bus.Subscribe<GameMenuToggled>(message => toggles.Add(message.Open));
            yield return null;

            _oven.Use();
            Assert.IsTrue(menu.IsOpen);
            Assert.AreEqual(2, menu.RowCount);
            Assert.IsTrue(menu.CanCook(WheatCakeRow));
            Assert.IsFalse(menu.CanCook(BerryCakeRow), "A recipe to buy cannot be cooked.");
            Assert.IsNotEmpty(menu.StatusText(BerryCakeRow));

            menu.ClickCook(WheatCakeRow);
            yield return null;

            Assert.IsFalse(menu.IsOpen);
            Assert.IsTrue(_oven.State.IsWorking);
            CollectionAssert.AreEqual(new[] { true, false }, toggles);
        }

        [UnityTest]
        public IEnumerator Menu_OpenedByAnotherMachine_ListsOnlyItsRecipes()
        {
            var menu = CreateMenu();
            var blenderData = Create(ScriptableObject.CreateInstance<StationData>());
            blenderData.Init("station_blender", null, 1, null, null, new[] { _berryCakeRecipe });
            var blenderObject = Create(new GameObject("Blender"));
            blenderObject.SetActive(false);
            var blender = blenderObject.AddComponent<Station>();
            blender.Init(blenderData, _bus, _bag, Create(new GameObject("Player")).transform, _book);
            blenderObject.SetActive(true);
            _book.Learn(_berryCakeRecipe);
            yield return null;

            blender.Use();
            Assert.AreEqual(1, menu.RowCount);
            menu.ClickCook(0);
            yield return null;

            Assert.IsTrue(blender.State.IsWorking, "The blender makes its own recipe.");
            Assert.IsTrue(_oven.State.IsIdle);
        }

        [UnityTest]
        public IEnumerator Menu_OpenedFromTheKeyboard_ListsEveryRecipe()
        {
            var menu = CreateMenu();
            yield return null;

            menu.Open(null);

            Assert.AreEqual(2, menu.RowCount);
            menu.Close();
        }

        [UnityTest]
        public IEnumerator Menu_OpenedFromTheKeyboard_CannotCook()
        {
            var menu = CreateMenu();
            yield return null;

            menu.Open(null);

            Assert.IsTrue(menu.IsOpen);
            Assert.IsFalse(menu.CanCook(WheatCakeRow), "Cooking needs the oven.");
            menu.Close();
        }

        private GameMenu CreateMenu()
        {
            var menuObject = Create(new GameObject("Menu"));
            menuObject.SetActive(false);
            var document = menuObject.AddComponent<UIDocument>();
            document.panelSettings = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var menu = menuObject.AddComponent<GameMenu>();
#if UNITY_EDITOR
            var menuTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/GameMenu.uxml");
            var rowTemplate = UnityEditor.AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/_Project/Art/Ui/RecipeRow.uxml");
            menu.Init(_bus, _book, _bag, menuTemplate, rowTemplate);
#else
            Assert.Ignore("The menu templates are loaded from the editor asset database.");
#endif
            menuObject.SetActive(true);
            return menu;
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
