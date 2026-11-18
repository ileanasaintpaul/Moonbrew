using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Moonbrew.Cafe;
using Moonbrew.Core;
using Moonbrew.Farming;
using Moonbrew.Inventory;
using Moonbrew.Kitchen;
using Moonbrew.Player;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    /// <summary>
    /// Checks every ScriptableObject under Assets/_Project/Data (items, recipes, customers, quests...).
    /// Adding content should never break the game: these tests catch it in CI.
    /// </summary>
    public class DataValidationTests
    {
        private const string DataFolder = "Assets/_Project/Data";
        private static readonly Regex SnakeCase = new Regex("^[a-z0-9]+(_[a-z0-9]+)*$");

        private static IEnumerable<string> DataAssetPaths()
        {
            if (!AssetDatabase.IsValidFolder(DataFolder))
            {
                return Enumerable.Empty<string>();
            }

            return AssetDatabase.FindAssets("t:Object", new[] { DataFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Where(path => path.EndsWith(".asset"))
                .Distinct();
        }

        [Test]
        public void DataAssets_LoadWithoutMissingScript()
        {
            var broken = DataAssetPaths()
                .Where(path => AssetDatabase.LoadAssetAtPath<ScriptableObject>(path) == null)
                .ToList();

            Assert.IsEmpty(broken, "Assets whose script is missing or renamed:\n" + string.Join("\n", broken));
        }

        [Test]
        public void DataAssets_HaveNoBrokenReferences()
        {
            var problems = new List<string>();

            foreach (var path in DataAssetPaths())
            {
                var asset = AssetDatabase.LoadAssetAtPath<ScriptableObject>(path);
                if (asset == null)
                {
                    continue; // reported by DataAssets_LoadWithoutMissingScript
                }

                var property = new SerializedObject(asset).GetIterator();
                while (property.Next(true))
                {
                    // A reference that points to a deleted asset: not null in the file, null once loaded
                    if (property.propertyType == SerializedPropertyType.ObjectReference
                        && property.objectReferenceValue == null
                        && property.objectReferenceInstanceIDValue != 0)
                    {
                        problems.Add($"{path} -> {property.propertyPath}");
                    }
                }
            }

            Assert.IsEmpty(problems, "References to deleted assets:\n" + string.Join("\n", problems));
        }

        [Test]
        public void PlayerConfig_HaveUsableValues()
        {
            var problems = new List<string>();

            foreach (var path in DataAssetPaths())
            {
                var settings = AssetDatabase.LoadAssetAtPath<PlayerConfig>(path);
                if (settings == null)
                {
                    continue;
                }

                if (settings.WalkSpeed <= 0f)
                {
                    problems.Add($"{path}: walk speed must be positive");
                }

                if (settings.RunSpeed < settings.WalkSpeed)
                {
                    problems.Add($"{path}: run speed must not be slower than walk speed");
                }

                if (settings.TurnSpeed <= 0f)
                {
                    problems.Add($"{path}: turn speed must be positive");
                }

                if (settings.Gravity >= 0f)
                {
                    problems.Add($"{path}: gravity must pull down (negative)");
                }

                // The number row has ten keys, 1 to 0.
                if (settings.HotbarSlots < 1 || settings.HotbarSlots > 10)
                {
                    problems.Add($"{path}: the hotbar needs 1 to 10 slots");
                }

                if (settings.InteractRadius <= 0f)
                {
                    problems.Add($"{path}: interact radius must be positive");
                }
            }

            Assert.IsEmpty(problems, "Invalid player settings:\n" + string.Join("\n", problems));
        }

        [Test]
        public void ClockConfig_HasPositivePhasesAndFirstDay()
        {
            var problems = new List<string>();

            foreach (var path in DataAssetPaths())
            {
                var config = AssetDatabase.LoadAssetAtPath<ClockConfig>(path);
                if (config == null)
                {
                    continue;
                }

                if (config.PhaseDurations().Any(seconds => seconds <= 0f))
                {
                    problems.Add($"{path}: every phase must last more than 0 seconds");
                }

                if (config.FirstDay < 1)
                {
                    problems.Add($"{path}: the first day must be 1 or more");
                }

                var boundaries = config.PhaseStartMinutes();
                for (var i = 1; i < boundaries.Length; i++)
                {
                    if (boundaries[i] <= boundaries[i - 1])
                    {
                        problems.Add($"{path}: phase hours must strictly increase");
                    }
                }

                if (config.MinuteStep < 1)
                {
                    problems.Add($"{path}: the minute step must be at least 1");
                }
            }

            Assert.IsEmpty(problems, "Invalid clock config:\n" + string.Join("\n", problems));
        }

        [Test]
        public void CaptureConfig_HasUsableValues()
        {
            var problems = new List<string>();

            foreach (var path in DataAssetPaths())
            {
                var config = AssetDatabase.LoadAssetAtPath<CaptureConfig>(path);
                if (config == null)
                {
                    continue;
                }

                if (config.AdvanceMinutes < 1)
                {
                    problems.Add($"{path}: the advance shortcut must add at least 1 minute");
                }

                if (config.CameraMinZoom > config.CameraMaxZoom)
                {
                    problems.Add($"{path}: the minimum zoom must not be above the maximum");
                }

                if (config.CameraZoomStep <= 0f || config.CameraPanSpeed <= 0f)
                {
                    problems.Add($"{path}: the camera zoom step and pan speed must be positive");
                }
            }

            Assert.IsEmpty(problems, "Invalid capture config:\n" + string.Join("\n", problems));
        }

        [Test]
        public void ZonePalettes_HaveUniqueIdsAndUsableColors()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var path in DataAssetPaths())
            {
                var palette = AssetDatabase.LoadAssetAtPath<ZonePalette>(path);
                if (palette == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(palette.Id))
                {
                    problems.Add($"{path}: the palette has no ID");
                }
                else if (!ids.Add(palette.Id))
                {
                    problems.Add($"{path}: the palette ID '{palette.Id}' is already used");
                }

                if (palette.Colors.Count == 0 || palette.Colors.Count > ZonePalette.MaxColors)
                {
                    problems.Add($"{path}: a palette needs 1 to {ZonePalette.MaxColors} colors");
                }

                if (palette.Colors.Any(color => color.a < 1f))
                {
                    problems.Add($"{path}: palette colors must be opaque");
                }
            }

            Assert.IsEmpty(problems, "Invalid zone palette:\n" + string.Join("\n", problems));
        }

        [Test]
        public void Items_HaveUniqueSnakeCaseIdsAndUsableValues()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var path in DataAssetPaths())
            {
                var item = AssetDatabase.LoadAssetAtPath<ItemData>(path);
                if (item == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.Id) || !SnakeCase.IsMatch(item.Id))
                {
                    problems.Add($"{path}: the item ID '{item.Id}' must be snake_case, e.g. crop_wheat");
                }
                else if (!ids.Add(item.Id))
                {
                    problems.Add($"{path}: the item ID '{item.Id}' is already used");
                }

                if (string.IsNullOrWhiteSpace(item.DisplayName))
                {
                    problems.Add($"{path}: the item has no display name");
                }

                if (item.BasePrice < 0 || item.MaxStack < 1)
                {
                    problems.Add($"{path}: the price must not be negative and a stack must hold at least 1");
                }
            }

            Assert.IsEmpty(problems, "Invalid item:\n" + string.Join("\n", problems));
        }

        [Test]
        public void InventoryConfigs_HaveUniqueIdsAndSlots()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var path in DataAssetPaths())
            {
                var config = AssetDatabase.LoadAssetAtPath<InventoryConfig>(path);
                if (config == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(config.Id) || !SnakeCase.IsMatch(config.Id))
                {
                    problems.Add($"{path}: the inventory ID '{config.Id}' must be snake_case, e.g. inventory_player");
                }
                else if (!ids.Add(config.Id))
                {
                    problems.Add($"{path}: the inventory ID '{config.Id}' is already used");
                }

                if (string.IsNullOrWhiteSpace(config.DisplayName))
                {
                    problems.Add($"{path}: the inventory has no display name");
                }

                if (config.SlotCount < 1)
                {
                    problems.Add($"{path}: an inventory needs at least one slot");
                }
            }

            Assert.IsEmpty(problems, "Invalid inventory config:\n" + string.Join("\n", problems));
        }

        // A save stores item IDs: an item missing from the catalog would vanish from every save on load.
        [Test]
        public void ItemCatalog_ListsEveryItemOnce()
        {
            var catalogs = DataAssetPaths().Select(AssetDatabase.LoadAssetAtPath<ItemCatalog>).Where(catalog => catalog != null).ToList();
            var items = DataAssetPaths().Select(AssetDatabase.LoadAssetAtPath<ItemData>).Where(item => item != null).ToList();
            if (items.Count == 0)
            {
                return;
            }

            Assert.AreEqual(1, catalogs.Count, "There must be exactly one item catalog under Data.");
            var listed = catalogs[0].Items;
            var missing = items.Where(item => !listed.Contains(item)).Select(item => item.name).ToList();
            Assert.IsEmpty(missing, "Items missing from the catalog:\n" + string.Join("\n", missing));
            Assert.AreEqual(listed.Count, listed.Distinct().Count(), "An item is listed twice in the catalog.");
            Assert.IsFalse(listed.Any(item => item == null), "The catalog has an empty entry.");
        }

        [Test]
        public void Crops_HaveUniqueIdsSeedsAndStages()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();
            var seeds = new HashSet<ItemData>();

            foreach (var path in DataAssetPaths())
            {
                var crop = AssetDatabase.LoadAssetAtPath<CropData>(path);
                if (crop == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(crop.Id) || !SnakeCase.IsMatch(crop.Id))
                {
                    problems.Add($"{path}: the crop ID '{crop.Id}' must be snake_case, e.g. wheat");
                }
                else if (!ids.Add(crop.Id))
                {
                    problems.Add($"{path}: the crop ID '{crop.Id}' is already used");
                }

                if (crop.Seed == null || crop.Seed.Category != ItemCategory.Seed)
                {
                    problems.Add($"{path}: the seed must be an item of the Seed category");
                }
                else if (!seeds.Add(crop.Seed))
                {
                    problems.Add($"{path}: the seed '{crop.Seed.Id}' already plants another crop");
                }

                if (crop.Harvest == null || crop.Harvest.Category != ItemCategory.Crop)
                {
                    problems.Add($"{path}: the harvest must be an item of the Crop category");
                }

                if (crop.HarvestMin < 1 || crop.HarvestMax < crop.HarvestMin)
                {
                    problems.Add($"{path}: the harvest must give at least 1, and the maximum must not be below the minimum");
                }

                if (crop.StageCount < 2 || Enumerable.Range(0, crop.StageCount).Any(stage => crop.StagePrefab(stage) == null))
                {
                    problems.Add($"{path}: a crop needs at least 2 growth stages, with a model for each");
                }

                if (crop.Regrows && crop.StageCount < 3)
                {
                    problems.Add($"{path}: a regrowing crop needs a stage before the ripe one to show while it regrows");
                }
            }

            Assert.IsEmpty(problems, "Invalid crop:\n" + string.Join("\n", problems));
        }

        // A save stores crop IDs: a crop missing from the catalog would vanish from the fields on load.
        [Test]
        public void CropCatalog_ListsEveryCropOnce()
        {
            var catalogs = DataAssetPaths().Select(AssetDatabase.LoadAssetAtPath<CropCatalog>).Where(catalog => catalog != null).ToList();
            var crops = DataAssetPaths().Select(AssetDatabase.LoadAssetAtPath<CropData>).Where(crop => crop != null).ToList();
            if (crops.Count == 0)
            {
                return;
            }

            Assert.AreEqual(1, catalogs.Count, "There must be exactly one crop catalog under Data.");
            var listed = catalogs[0].Crops;
            var missing = crops.Where(crop => !listed.Contains(crop)).Select(crop => crop.name).ToList();
            Assert.IsEmpty(missing, "Crops missing from the catalog:\n" + string.Join("\n", missing));
            Assert.AreEqual(listed.Count, listed.Distinct().Count(), "A crop is listed twice in the catalog.");
            Assert.IsFalse(listed.Any(crop => crop == null), "The catalog has an empty entry.");
        }

        [Test]
        public void FarmConfig_HasAHoeToolAndTilledSoil()
        {
            var problems = new List<string>();

            foreach (var path in DataAssetPaths())
            {
                var config = AssetDatabase.LoadAssetAtPath<FarmConfig>(path);
                if (config == null)
                {
                    continue;
                }

                if (config.Hoe == null || config.Hoe.Category != ItemCategory.Tool)
                {
                    problems.Add($"{path}: the hoe must be an item of the Tool category");
                }

                if (config.Sickle == null || config.Sickle.Category != ItemCategory.Tool || config.Sickle == config.Hoe)
                {
                    problems.Add($"{path}: the sickle must be a Tool item, other than the hoe");
                }

                if (config.WateringCan == null || config.WateringCan.Category != ItemCategory.Tool || config.WateringCan.MaxStack != 1)
                {
                    problems.Add($"{path}: the watering can must be a Tool item that does not stack (each can keeps its own water)");
                }

                if (config.TilledSoilPrefab == null)
                {
                    problems.Add($"{path}: the tilled soil model is missing");
                }

                if (config.RipeBubblePrefab == null || config.RipeBubblePrefab.transform.Find("Icon") == null || config.RipeBubblePrefab.GetComponentInChildren<ClickTarget>() == null)
                {
                    problems.Add($"{path}: the ripe crop bubble needs an Icon child and a click target");
                }
            }

            Assert.IsEmpty(problems, "Invalid farm config:\n" + string.Join("\n", problems));
        }

        [Test]
        public void Processes_HaveUniqueIdsItemsAndQuantities()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var path in DataAssetPaths())
            {
                var process = AssetDatabase.LoadAssetAtPath<ProcessData>(path);
                if (process == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(process.Id) || !SnakeCase.IsMatch(process.Id))
                {
                    problems.Add($"{path}: the process ID '{process.Id}' must be snake_case, e.g. process_flour");
                }
                else if (!ids.Add(process.Id))
                {
                    problems.Add($"{path}: the process ID '{process.Id}' is already used");
                }

                if (process.Input == null || process.Output == null)
                {
                    problems.Add($"{path}: a process needs an input and an output item");
                }

                if (process.InputQuantity < 1 || process.OutputQuantity < 1 || process.Minutes < 0)
                {
                    problems.Add($"{path}: quantities must be at least 1 and the time must not be negative");
                }
            }

            Assert.IsEmpty(problems, "Invalid process:\n" + string.Join("\n", problems));
        }

        [Test]
        public void Stations_HaveUniqueIdsProcessesAndCapacity()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var path in DataAssetPaths())
            {
                var station = AssetDatabase.LoadAssetAtPath<StationData>(path);
                if (station == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(station.Id) || !SnakeCase.IsMatch(station.Id))
                {
                    problems.Add($"{path}: the station ID '{station.Id}' must be snake_case, e.g. station_mill");
                }
                else if (!ids.Add(station.Id))
                {
                    problems.Add($"{path}: the station ID '{station.Id}' is already used");
                }

                if (station.Processes.Count + station.Recipes.Count == 0
                    || station.Processes.Any(process => process == null) || station.Recipes.Any(recipe => recipe == null))
                {
                    problems.Add($"{path}: a station needs at least one process or recipe, with no empty entry");
                }
                else if (station.Processes.Select(process => process.Input).Distinct().Count() != station.Processes.Count)
                {
                    problems.Add($"{path}: two processes of the station take the same input item");
                }

                if (station.Capacity < 1)
                {
                    problems.Add($"{path}: a station takes at least one batch");
                }

                if (station.BubblePrefab == null || station.BubblePrefab.transform.Find("Icon") == null || station.WorkingIcon == null)
                {
                    problems.Add($"{path}: a station needs a bubble prefab with an Icon child, and a working icon");
                }
            }

            Assert.IsEmpty(problems, "Invalid station:\n" + string.Join("\n", problems));
        }

        [Test]
        public void Recipes_HaveUniqueIdsIngredientsAndStars()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var path in DataAssetPaths())
            {
                var recipe = AssetDatabase.LoadAssetAtPath<RecipeData>(path);
                if (recipe == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(recipe.Id) || !SnakeCase.IsMatch(recipe.Id))
                {
                    problems.Add($"{path}: the recipe ID '{recipe.Id}' must be snake_case, e.g. recipe_wheat_cake");
                }
                else if (!ids.Add(recipe.Id))
                {
                    problems.Add($"{path}: the recipe ID '{recipe.Id}' is already used");
                }

                if (string.IsNullOrWhiteSpace(recipe.DisplayName) || recipe.Result == null || recipe.ResultQuantity < 1)
                {
                    problems.Add($"{path}: a recipe needs a name and a result");
                }

                if (recipe.Ingredients.Count == 0 || recipe.Ingredients.Any(ingredient => ingredient.Item == null || ingredient.Quantity < 1))
                {
                    problems.Add($"{path}: a recipe needs ingredients, each with an item and a quantity of at least 1");
                }

                if (recipe.Stars < 1 || recipe.Stars > RecipeData.MaxStars || recipe.Minutes < 0)
                {
                    problems.Add($"{path}: stars go from 1 to {RecipeData.MaxStars} and the time must not be negative");
                }

                if (!recipe.KnownAtStart && recipe.Price <= 0)
                {
                    problems.Add($"{path}: a recipe to buy needs a price");
                }
            }

            Assert.IsEmpty(problems, "Invalid recipe:\n" + string.Join("\n", problems));
        }

        // The menu lists the catalog: a recipe missing from it could never be seen or cooked.
        [Test]
        public void RecipeCatalog_ListsEveryRecipeOnce()
        {
            var catalogs = DataAssetPaths().Select(AssetDatabase.LoadAssetAtPath<RecipeCatalog>).Where(catalog => catalog != null).ToList();
            var recipes = DataAssetPaths().Select(AssetDatabase.LoadAssetAtPath<RecipeData>).Where(recipe => recipe != null).ToList();
            if (recipes.Count == 0)
            {
                return;
            }

            Assert.AreEqual(1, catalogs.Count, "There must be exactly one recipe catalog under Data.");
            var listed = catalogs[0].Recipes;
            var missing = recipes.Where(recipe => !listed.Contains(recipe)).Select(recipe => recipe.name).ToList();
            Assert.IsEmpty(missing, "Recipes missing from the catalog:\n" + string.Join("\n", missing));
            Assert.AreEqual(listed.Count, listed.Distinct().Count(), "A recipe is listed twice in the catalog.");
        }

        [Test]
        public void Customers_HaveUniqueIdsModelsAndVillagerHints()
        {
            var problems = new List<string>();
            var ids = new HashSet<string>();

            foreach (var path in DataAssetPaths())
            {
                var customer = AssetDatabase.LoadAssetAtPath<CustomerData>(path);
                if (customer == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(customer.Id) || !SnakeCase.IsMatch(customer.Id))
                {
                    problems.Add($"{path}: the customer ID '{customer.Id}' must be snake_case, e.g. villager_rose");
                }
                else if (!ids.Add(customer.Id))
                {
                    problems.Add($"{path}: the customer ID '{customer.Id}' is already used");
                }

                if (string.IsNullOrWhiteSpace(customer.DisplayName))
                {
                    problems.Add($"{path}: a customer needs a name");
                }

                var animator = customer.Model != null ? customer.Model.GetComponentInChildren<Animator>() : null;
                if (animator == null || animator.runtimeAnimatorController == null)
                {
                    problems.Add($"{path}: a customer needs a model with an animated character");
                }

                if (customer.Lines.Count == 0 || customer.Lines.Any(string.IsNullOrWhiteSpace))
                {
                    problems.Add($"{path}: a customer needs at least one line, and no empty line");
                }

                if (customer.Loved.Concat(customer.Disliked).Any(item => item == null) || customer.Loved.Intersect(customer.Disliked).Any())
                {
                    problems.Add($"{path}: a taste needs an item, and an item cannot be both loved and disliked");
                }

                if (customer.Kind == CustomerKind.Villager && (customer.Loved.Count == 0 || customer.Disliked.Count == 0))
                {
                    problems.Add($"{path}: a villager needs at least one loved and one disliked item, for the player to discover");
                }
            }

            Assert.IsEmpty(problems, "Invalid customer:\n" + string.Join("\n", problems));
        }

        [Test]
        public void CustomerCatalog_ListsEveryCustomerOnce()
        {
            var catalogs = DataAssetPaths().Select(AssetDatabase.LoadAssetAtPath<CustomerCatalog>).Where(catalog => catalog != null).ToList();
            var customers = DataAssetPaths().Select(AssetDatabase.LoadAssetAtPath<CustomerData>).Where(customer => customer != null).ToList();
            if (customers.Count == 0)
            {
                return;
            }

            Assert.AreEqual(1, catalogs.Count, "There must be exactly one customer catalog under Data.");
            var listed = catalogs[0].Customers;
            var missing = customers.Where(customer => !listed.Contains(customer)).Select(customer => customer.name).ToList();
            Assert.IsEmpty(missing, "Customers missing from the catalog:\n" + string.Join("\n", missing));
            Assert.AreEqual(listed.Count, listed.Distinct().Count(), "A customer is listed twice in the catalog.");
        }

        [Test]
        public void SessionConfig_HasSaveFolderSceneAndSleepSpot()
        {
            var problems = new List<string>();

            foreach (var path in DataAssetPaths())
            {
                var config = AssetDatabase.LoadAssetAtPath<SessionConfig>(path);
                if (config == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(config.SavesFolder) || string.IsNullOrWhiteSpace(config.SleepInteractionId))
                {
                    problems.Add($"{path}: the saves folder and the sleep interaction ID are required");
                }

                var sceneInBuild = EditorBuildSettings.scenes.Any(scene => scene.enabled && System.IO.Path.GetFileNameWithoutExtension(scene.path) == config.WakeScene);
                if (!sceneInBuild)
                {
                    problems.Add($"{path}: the wake scene '{config.WakeScene}' is not an enabled scene of the build");
                }
            }

            Assert.IsEmpty(problems, "Invalid session config:\n" + string.Join("\n", problems));
        }
    }
}
