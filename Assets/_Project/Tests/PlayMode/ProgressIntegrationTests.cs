using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Economy;
using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using Moonbrew.Player;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace Moonbrew.Tests.PlayMode
{
    // Activities give experience; a new level gives its points, money, recipe and items, and it all survives a reload.
    public class ProgressIntegrationTests
    {
        private const int ServedXp = 10;
        private const int HarvestXp = 4;
        private const int CraftXp = 15;

        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private PlayerProgress _progress;
        private WalletHolder _wallet;
        private RecipeBookHolder _book;
        private InventoryHolder _bag;
        private ItemData _seeds;
        private RecipeData _secret;
        private VitalsHud _hud;

        [SetUp]
        public void SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _seeds = Create(ScriptableObject.CreateInstance<ItemData>());
            var items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            items.Init(new[] { _seeds });
            _secret = Create(ScriptableObject.CreateInstance<RecipeData>());
            _secret.Init("recipe_secret", new Ingredient[0], _seeds, 2, 30, false);
            var recipes = Create(ScriptableObject.CreateInstance<RecipeCatalog>());
            recipes.Init(new[] { _secret });
            var data = Create(ScriptableObject.CreateInstance<ProgressionData>());
            data.Init(new[] { 20, 40 }, 2, new[] { new LevelReward(2, 100, "recipe_secret", _seeds, 5) }, ServedXp, HarvestXp, CraftXp);

            var panel = Create(ScriptableObject.CreateInstance<PanelSettings>());
            var hudObject = Create(new GameObject("HUD"));
            hudObject.SetActive(false);
            hudObject.AddComponent<UIDocument>().panelSettings = panel;
            _hud = hudObject.AddComponent<VitalsHud>();
            hudObject.SetActive(true);
            _hud.Init(_bus);

            var world = Create(new GameObject("World"));
            world.SetActive(false);
            _bag = world.AddComponent<InventoryHolder>();
            _bag.Init(Create(ScriptableObject.CreateInstance<InventoryConfig>()), items, _bus);
            _wallet = world.AddComponent<WalletHolder>();
            _wallet.Init(_bus);
            _book = world.AddComponent<RecipeBookHolder>();
            _book.Init(recipes, _bus);
            _progress = world.AddComponent<PlayerProgress>();
            _progress.Init(data, _bag, _bus);
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
        public IEnumerator Activities_GiveTheirExperience()
        {
            yield return null;

            _bus.Publish(new OrderServed(0, new[] { "dish" }, 10));
            _bus.Publish(new OrderServed(1, new[] { "dish" }, 10, true));
            _bus.Publish(new CropHarvested("crop_wheat", "crop_wheat", 3));

            Assert.AreEqual(ServedXp + HarvestXp, _progress.Progression.Xp, "The employee's tables give the owner nothing.");
        }

        [UnityTest]
        public IEnumerator ItemCrafted_GivesExperienceTheFirstTimeOnly()
        {
            yield return null;

            _bus.Publish(new ItemCrafted("oven", "dish_cake", 1));
            _bus.Publish(new ItemCrafted("oven", "dish_cake", 1));

            Assert.AreEqual(CraftXp, _progress.Progression.Xp);
        }

        [UnityTest]
        public IEnumerator LevelReached_GivesPointsMoneyRecipeAndItems()
        {
            yield return null;

            _bus.Publish(new OrderServed(0, new[] { "dish" }, 0));
            _bus.Publish(new OrderServed(0, new[] { "dish" }, 0));

            Assert.AreEqual(2, _progress.Progression.Level);
            Assert.AreEqual(2, _progress.Progression.SkillPoints);
            Assert.AreEqual(100, _wallet.Wallet.Balance);
            Assert.IsTrue(_book.Book.IsKnown(_secret));
            Assert.AreEqual(5, _bag.Inventory.Count(_seeds));
            Assert.AreEqual("Level 2", _hud.LevelText);
        }

        [UnityTest]
        public IEnumerator SaveAndLoad_KeepTheProgress()
        {
            yield return null;
            _bus.Publish(new OrderServed(0, new[] { "dish" }, 0));
            _bus.Publish(new OrderServed(0, new[] { "dish" }, 0));
            _bus.Publish(new ItemCrafted("oven", "dish_cake", 1));
            var save = SaveData.CreateNew("slot_a", 1);
            _bus.Publish(new GameSaving(save));
            _bus.Publish(new GameLoaded(SaveData.CreateNew("slot_b", 1)));

            _bus.Publish(new GameLoaded(save));

            Assert.AreEqual((2, CraftXp, 2), (_progress.Progression.Level, _progress.Progression.Xp, _progress.Progression.SkillPoints));
            Assert.IsFalse(_progress.Progression.FirstCraft("dish_cake"));
        }

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
