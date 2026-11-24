using System.Collections;
using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Farming;
using Moonbrew.Inventory;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Moonbrew.Tests.PlayMode
{
    // Tilling with the hoe and planting seeds from the bag, on clicks near the player, and the field surviving a save.
    public class FarmFieldIntegrationTests
    {
        private const int HoeSlot = 0;
        private const int SeedSlot = 1;
        private const int SickleSlot = 2;
        private const int CanSlot = 3;
        private const int CanCapacity = 3;
        private const string SproutModel = "Sprout";
        private const string RipeModel = "Ripe";

        private static readonly Vector2Int PlayerCell = Vector2Int.zero;
        private static readonly Vector2Int NextCell = new Vector2Int(1, 0);

        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private ItemData _hoe;
        private ItemData _seeds;
        private ItemData _sickle;
        private ItemData _can;
        private ItemData _wheat;
        private ItemData _berry;
        private CropCatalog _crops;
        private FarmConfig _config;
        private InventoryHolder _bag;
        private Transform _player;

        [UnitySetUp]
        public IEnumerator SetUp()
        {
            _bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            _hoe = CreateItem("{\"id\":\"tool_hoe\",\"maxStack\":1}");
            _seeds = CreateItem("{\"id\":\"seed_wheat\",\"maxStack\":99}");
            _sickle = CreateItem("{\"id\":\"tool_sickle\",\"maxStack\":1}");

            _can = CreateItem("{\"id\":\"tool_watering_can\",\"maxStack\":1}");

            // Two growth days: sprout, then young, then ripe.
            var sprout = CreateModel(SproutModel);
            var young = CreateModel("Young");
            var ripe = CreateModel(RipeModel);
            _wheat = CreateItem("{\"id\":\"crop_wheat\",\"maxStack\":99}");
            _berry = CreateItem("{\"id\":\"crop_strawberry\",\"maxStack\":99}");
            var crop = Create(ScriptableObject.CreateInstance<CropData>());
            crop.Init("wheat", _seeds, _wheat, 2, new[] { sprout, young, ripe });
            var berries = Create(ScriptableObject.CreateInstance<CropData>());
            berries.Init("strawberry", CreateItem("{\"id\":\"seed_strawberry\",\"maxStack\":99}"), _berry, 2, new[] { sprout, young, ripe }, 1, 1, 3);
            _crops = Create(ScriptableObject.CreateInstance<CropCatalog>());
            _crops.Init(new[] { crop, berries });

            _config = Create(ScriptableObject.CreateInstance<FarmConfig>());
            _config.Init(_hoe, _sickle, CreateModel("Soil"), _can, CanCapacity, CreateBubble());

            var bagObject = Create(new GameObject("Bag"));
            bagObject.SetActive(false);
            var bagConfig = Create(ScriptableObject.CreateInstance<InventoryConfig>());
            JsonUtility.FromJsonOverwrite("{\"id\":\"inventory_player\",\"slotCount\":10}", bagConfig);
            _bag = bagObject.AddComponent<InventoryHolder>();
            var items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            items.Init(new[] { _hoe, _seeds, _sickle, _can, _wheat, _berry });
            _bag.Init(bagConfig, items, _bus);
            bagObject.SetActive(true);
            _bag.Inventory.Add(_hoe, 1);
            _bag.Inventory.Add(_seeds, 5);
            _bag.Inventory.Add(_sickle, 1);
            _bag.Inventory.Add(_can, 1);

            // The player stands in the middle of cell (0, 0), with a collider like the real one.
            var player = Create(new GameObject("Player"));
            player.AddComponent<CapsuleCollider>().center = Vector3.up;
            player.transform.position = new Vector3(0.5f, 0f, 0.5f);
            _player = player.transform;

            yield return new WaitForFixedUpdate();
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
        public IEnumerator Hoe_ClickOnNeighbourCell_TillsAndShowsSoil()
        {
            var field = CreateField();
            Select(HoeSlot);

            _bus.Publish(new GroundClicked(Center(NextCell)));
            yield return null;

            Assert.IsTrue(field.Plots.IsTilled(NextCell));
            Assert.IsTrue(field.HasSoilModel(NextCell));
        }

        [UnityTest]
        public IEnumerator Hoe_RotatedField_TillsAlongItsRows()
        {
            var field = CreateField();
            field.transform.rotation = Quaternion.Euler(0f, 45f, 0f);
            _player.position = field.transform.TransformPoint(Center(Vector2Int.zero));
            Select(HoeSlot);
            yield return null;

            field.UseAt(field.transform.TransformPoint(Center(NextCell)));

            Assert.IsTrue(field.Plots.IsTilled(NextCell));
            var soil = field.transform.GetChild(0);
            Assert.AreEqual(45f, soil.eulerAngles.y, 0.01f, "The soil turns with the field.");
            Assert.AreEqual(field.transform.TransformPoint(Center(NextCell)).x, soil.position.x, 0.01f);
        }

        [UnityTest]
        public IEnumerator Hoe_Used_SpendsStaminaOnlyWhenItTills()
        {
            var field = CreateField();
            Select(HoeSlot);
            yield return null;
            var used = 0;
            System.Action<ToolUsed> onUsed = _ => used++;
            _bus.Subscribe(onUsed);

            field.UseAt(Center(NextCell));
            field.UseAt(Center(NextCell));
            _bus.Unsubscribe(onUsed);

            Assert.AreEqual(1, used, "The second click finds the cell already tilled.");
        }

        [UnityTest]
        public IEnumerator Hoe_OwnCell_IgnoresThePlayer()
        {
            var field = CreateField();
            Select(HoeSlot);

            Assert.IsTrue(field.UseAt(Center(PlayerCell)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hoe_TwoCellsAway_DoesNothing()
        {
            var field = CreateField();
            Select(HoeSlot);

            Assert.IsFalse(field.UseAt(Center(new Vector2Int(2, 0))));
            Assert.IsFalse(field.Plots.IsTilled(new Vector2Int(2, 0)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Hoe_CellWithAnObstacle_StaysGrass()
        {
            var rock = Create(GameObject.CreatePrimitive(PrimitiveType.Cube));
            rock.transform.position = Center(NextCell) + Vector3.up * 0.5f;
            yield return new WaitForFixedUpdate();
            var field = CreateField();
            Select(HoeSlot);

            Assert.IsFalse(field.UseAt(Center(NextCell)));
            Assert.IsFalse(field.Plots.IsTilled(NextCell));
        }

        [UnityTest]
        public IEnumerator Seeds_OnTilledCell_PlantOnceAndUseOneSeed()
        {
            var field = CreateField();
            Select(HoeSlot);
            field.UseAt(Center(NextCell));
            Select(SeedSlot);

            Assert.IsTrue(field.UseAt(Center(NextCell)));
            Assert.IsFalse(field.UseAt(Center(NextCell)), "A second seed was planted on the same cell.");
            yield return null;

            Assert.AreEqual(4, _bag.Inventory.Count(_seeds));
            Assert.IsTrue(field.Plots.TryGet(NextCell, out var plot));
            Assert.AreEqual("wheat", plot.CropId);
            Assert.IsTrue(field.HasPlantModel(NextCell));
        }

        [UnityTest]
        public IEnumerator Seeds_OnGrass_KeepTheSeed()
        {
            var field = CreateField();
            Select(SeedSlot);

            Assert.IsFalse(field.UseAt(Center(NextCell)));
            Assert.AreEqual(5, _bag.Inventory.Count(_seeds));
            yield return null;
        }

        [UnityTest]
        public IEnumerator EmptySlot_Click_DoesNothing()
        {
            var field = CreateField();
            Select(5);

            Assert.IsFalse(field.UseAt(Center(NextCell)));
            yield return null;
        }

        [UnityTest]
        public IEnumerator SaveThenLoad_NewField_RestoresSoilAndPlants()
        {
            var field = CreateField();
            Select(HoeSlot);
            field.UseAt(Center(NextCell));
            field.UseAt(Center(new Vector2Int(0, 1)));
            Select(SeedSlot);
            field.UseAt(Center(NextCell));
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            _bus.Publish(new GameSaving(data));
            Object.Destroy(field.gameObject);
            yield return null;

            var reloaded = CreateField();
            _bus.Publish(new GameLoaded(data));
            yield return null;

            Assert.AreEqual(2, data.plots.Count);
            Assert.IsTrue(reloaded.HasSoilModel(new Vector2Int(0, 1)));
            Assert.IsTrue(reloaded.HasPlantModel(NextCell));
            Assert.IsFalse(reloaded.HasPlantModel(new Vector2Int(0, 1)));
        }

        [UnityTest]
        public IEnumerator Hoe_CellWithGrass_StaysGrass()
        {
            var clump = CreateGrass("grass_a", Center(NextCell));
            yield return new WaitForFixedUpdate();
            var field = CreateField(clump);
            Select(HoeSlot);

            Assert.IsFalse(field.UseAt(Center(NextCell)));
            Assert.IsFalse(field.Plots.IsTilled(NextCell));
        }

        [UnityTest]
        public IEnumerator Sickle_Swing_CutsGrassInFrontOnly()
        {
            var front = CreateGrass("grass_front", Center(NextCell));
            var side = CreateGrass("grass_side", Center(new Vector2Int(0, 1)));
            var behind = CreateGrass("grass_behind", Center(new Vector2Int(-1, 0)));
            var far = CreateGrass("grass_far", Center(new Vector2Int(3, 0)));
            var field = CreateField(front, side, behind, far);
            Select(SickleSlot);

            Assert.IsTrue(field.UseAt(Center(new Vector2Int(5, 0))));
            yield return null;

            Assert.IsTrue(front.IsCut);
            Assert.IsTrue(side.IsCut, "A clump at 90 degrees is inside a 180 degree swing.");
            Assert.IsFalse(behind.IsCut);
            Assert.IsFalse(far.IsCut, "A clump out of the sickle radius was cut.");
            Assert.AreEqual(2, field.CutGrassCount);
        }

        [UnityTest]
        public IEnumerator Sickle_ThenHoe_CellCanBeTilled()
        {
            var clump = CreateGrass("grass_a", Center(NextCell));
            yield return new WaitForFixedUpdate();
            var field = CreateField(clump);
            Select(SickleSlot);
            field.UseAt(Center(NextCell));
            yield return new WaitForFixedUpdate();
            Select(HoeSlot);

            Assert.IsTrue(field.UseAt(Center(NextCell)));
        }

        [UnityTest]
        public IEnumerator SaveThenLoad_CutGrass_StaysCut()
        {
            var clump = CreateGrass("grass_a", Center(NextCell));
            var field = CreateField(clump);
            Select(SickleSlot);
            field.UseAt(Center(NextCell));
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            _bus.Publish(new GameSaving(data));
            clump.gameObject.SetActive(true);
            Object.Destroy(field.gameObject);
            yield return null;

            CreateField(clump);
            _bus.Publish(new GameLoaded(data));
            yield return null;

            CollectionAssert.AreEqual(new[] { "grass_a" }, data.cutGrass);
            Assert.IsTrue(clump.IsCut);
        }

        [UnityTest]
        public IEnumerator Can_OnTilledCell_WatersAndUsesWater()
        {
            var field = CreateField();
            var levels = new List<int>();
            _bus.Subscribe<WateringCanChanged>(message => levels.Add(message.Water));
            TillAndPlant(field, NextCell);
            Select(CanSlot);

            Assert.IsTrue(field.UseAt(Center(NextCell)));
            Assert.IsFalse(field.UseAt(Center(NextCell)), "The same cell was watered twice.");
            yield return null;

            Assert.IsTrue(field.ShowsWet(NextCell));
            Assert.AreEqual(CanCapacity - 1, field.Can.Water);
            Assert.AreEqual(CanCapacity - 1, levels[levels.Count - 1]);
        }

        [UnityTest]
        public IEnumerator Can_OnGrass_DoesNothing()
        {
            var field = CreateField();
            Select(CanSlot);

            Assert.IsFalse(field.UseAt(Center(NextCell)));
            Assert.AreEqual(CanCapacity, field.Can.Water);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Can_Empty_DoesNotWater()
        {
            var field = CreateField();
            Select(HoeSlot);
            var cells = new[] { new Vector2Int(1, 0), new Vector2Int(0, 1), new Vector2Int(1, 1), new Vector2Int(-1, 0) };
            foreach (var cell in cells)
            {
                field.UseAt(Center(cell));
            }

            Select(CanSlot);
            for (var i = 0; i < CanCapacity; i++)
            {
                field.UseAt(Center(cells[i]));
            }

            Assert.IsTrue(field.Can.IsEmpty);
            Assert.IsFalse(field.UseAt(Center(cells[CanCapacity])));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Can_ClickOnWellNearby_Refills()
        {
            var well = Create(new GameObject("Well")).transform;
            well.position = new Vector3(2.5f, 0f, 0.5f);
            var field = CreateField(well);
            TillAndPlant(field, NextCell);
            Select(CanSlot);
            field.UseAt(Center(NextCell));

            Assert.IsTrue(field.UseAt(well.position));
            Assert.AreEqual(CanCapacity, field.Can.Water);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Can_WellTooFar_DoesNotRefill()
        {
            var well = Create(new GameObject("Well")).transform;
            well.position = new Vector3(8.5f, 0f, 0.5f);
            var field = CreateField(well);
            TillAndPlant(field, NextCell);
            Select(CanSlot);
            field.UseAt(Center(NextCell));

            Assert.IsFalse(field.UseAt(well.position));
            Assert.AreEqual(CanCapacity - 1, field.Can.Water);
            yield return null;
        }

        [UnityTest]
        public IEnumerator Night_WateredEveryDay_CropGrowsUntilRipe()
        {
            var field = CreateField();
            TillAndPlant(field, NextCell);
            StringAssert.StartsWith(SproutModel, field.PlantModel(NextCell).name);

            for (var day = 1; day <= 2; day++)
            {
                Select(CanSlot);
                field.UseAt(Center(NextCell));
                _bus.Publish(new NightPassed(day));
                yield return null;
            }

            field.Plots.TryGet(NextCell, out var plot);
            Assert.AreEqual(2, plot.DaysGrown);
            StringAssert.StartsWith(RipeModel, field.PlantModel(NextCell).name);
            Assert.IsFalse(field.ShowsWet(NextCell), "The soil should dry overnight.");
        }

        [UnityTest]
        public IEnumerator Night_NotWatered_CropWaits()
        {
            var field = CreateField();
            TillAndPlant(field, NextCell);

            _bus.Publish(new NightPassed(1));
            yield return null;

            field.Plots.TryGet(NextCell, out var plot);
            Assert.AreEqual(0, plot.DaysGrown);
            StringAssert.StartsWith(SproutModel, field.PlantModel(NextCell).name);
        }

        [UnityTest]
        public IEnumerator SaveThenLoad_UsedWater_IsKept()
        {
            var field = CreateField();
            TillAndPlant(field, NextCell);
            Select(CanSlot);
            field.UseAt(Center(NextCell));
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            _bus.Publish(new GameSaving(data));
            Object.Destroy(field.gameObject);
            yield return null;

            var reloaded = CreateField();
            _bus.Publish(new GameLoaded(data));

            Assert.AreEqual(1, data.wateringCanUsed);
            Assert.AreEqual(CanCapacity - 1, reloaded.Can.Water);
        }

        [UnityTest]
        public IEnumerator Harvest_RipeWheatWithAnyItem_GoesToTheBagAndIsAnnounced()
        {
            var field = CreateField();
            LoadRipe(NextCell, "wheat");
            var harvested = new List<CropHarvested>();
            _bus.Subscribe<CropHarvested>(message => harvested.Add(message));
            Select(SeedSlot);

            Assert.IsTrue(field.UseAt(Center(NextCell)));
            yield return null;

            Assert.AreEqual(1, _bag.Inventory.Count(_wheat));
            Assert.AreEqual(5, _bag.Inventory.Count(_seeds), "Harvesting should not plant the seeds in hand.");
            Assert.IsTrue(field.Plots.CanPlant(NextCell), "Wheat leaves the soil tilled and empty.");
            Assert.IsFalse(field.HasPlantModel(NextCell));
            Assert.AreEqual(1, harvested.Count);
            Assert.AreEqual(("wheat", "crop_wheat", 1), (harvested[0].CropId, harvested[0].ItemId, harvested[0].Quantity));
        }

        [UnityTest]
        public IEnumerator Harvest_EmptyHand_StillPicks()
        {
            var field = CreateField();
            LoadRipe(NextCell, "wheat");
            Select(9);

            Assert.IsTrue(field.UseAt(Center(NextCell)));
            Assert.AreEqual(1, _bag.Inventory.Count(_wheat));
            yield return null;
        }

        [UnityTest]
        public IEnumerator Harvest_Strawberry_RegrowsFromTheLeafyStage()
        {
            var field = CreateField();
            field.SetRandom(new System.Random(7));
            LoadRipe(NextCell, "strawberry");

            field.UseAt(Center(NextCell));
            yield return null;

            var picked = _bag.Inventory.Count(_berry);
            Assert.That(picked, Is.InRange(1, 3));
            field.Plots.TryGet(NextCell, out var plot);
            Assert.AreEqual("strawberry", plot.CropId);
            Assert.AreEqual(1, plot.DaysGrown);
            StringAssert.StartsWith("Young", field.PlantModel(NextCell).name);
            Assert.IsFalse(field.UseAt(Center(NextCell)), "A regrowing plant cannot be picked again at once.");
        }

        [UnityTest]
        public IEnumerator Harvest_BagFull_CropStaysRipe()
        {
            var field = CreateField();
            LoadRipe(NextCell, "wheat");
            var pebble = CreateItem("{\"id\":\"pebble\",\"maxStack\":1}");
            _bag.Inventory.Add(pebble, 6);
            var harvested = 0;
            _bus.Subscribe<CropHarvested>(message => harvested++);

            Assert.IsFalse(field.UseAt(Center(NextCell)));
            yield return null;

            Assert.AreEqual(0, _bag.Inventory.Count(_wheat));
            Assert.IsTrue(field.Plots.IsRipe(NextCell, 2));
            Assert.AreEqual(0, harvested);
        }

        [UnityTest]
        public IEnumerator Harvest_TwoCellsAway_DoesNothing()
        {
            var field = CreateField();
            LoadRipe(new Vector2Int(2, 0), "wheat");

            Assert.IsFalse(field.UseAt(Center(new Vector2Int(2, 0))));
            Assert.AreEqual(0, _bag.Inventory.Count(_wheat));
            yield return null;
        }

        [UnityTest]
        public IEnumerator RipeCrop_ShowsABubbleThatPicksItWhenClicked()
        {
            var field = CreateField();
            LoadRipe(NextCell, "wheat");
            yield return null;
            var bubble = field.transform.GetComponentInChildren<ClickTarget>();
            Assert.IsNotNull(bubble, "A ripe crop should show a bubble.");

            _bus.Publish(new TargetClicked(bubble));
            yield return null;

            Assert.AreEqual(1, _bag.Inventory.Count(_wheat));
            Assert.IsNull(field.transform.GetComponentInChildren<ClickTarget>(), "The bubble should go once the crop is picked.");
        }

        [UnityTest]
        public IEnumerator GrowingCrop_ShowsNoBubble()
        {
            var field = CreateField();
            TillAndPlant(field, NextCell);
            yield return null;

            Assert.IsNull(field.transform.GetComponentInChildren<ClickTarget>());
        }

        private GameObject CreateBubble()
        {
            var bubble = CreateModel("Bubble");
            bubble.AddComponent<BoxCollider>().isTrigger = true;
            bubble.AddComponent<ClickTarget>();
            var icon = new GameObject("Icon");
            icon.transform.SetParent(bubble.transform, false);
            icon.AddComponent<SpriteRenderer>();
            return bubble;
        }

        private void LoadRipe(Vector2Int cell, string cropId)
        {
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            data.plots.Add(new PlotSave { x = cell.x, y = cell.y, cropId = cropId, daysGrown = 2 });
            data.inventories.Add(_bag.Inventory.ToSave("inventory_player"));
            _bus.Publish(new GameLoaded(data));
        }

        private void TillAndPlant(FarmField field, Vector2Int cell)
        {
            Select(HoeSlot);
            field.UseAt(Center(cell));
            Select(SeedSlot);
            field.UseAt(Center(cell));
        }

        private GameObject CreateModel(string modelName)
        {
            var model = Create(new GameObject(modelName));
            model.SetActive(false);
            return model;
        }

        private CuttableGrass CreateGrass(string id, Vector3 position)
        {
            var clumpObject = Create(new GameObject(id));
            clumpObject.transform.position = position;
            var trigger = clumpObject.AddComponent<BoxCollider>();
            trigger.isTrigger = true;
            trigger.center = Vector3.up * 0.3f;
            trigger.size = new Vector3(0.5f, 0.6f, 0.5f);
            var clump = clumpObject.AddComponent<CuttableGrass>();
            clump.Init(id);
            return clump;
        }

        private FarmField CreateField(params CuttableGrass[] grass)
        {
            return CreateField(null, grass);
        }

        private FarmField CreateField(Transform well, params CuttableGrass[] grass)
        {
            var fieldObject = Create(new GameObject("Farm Field"));
            fieldObject.SetActive(false);
            var field = fieldObject.AddComponent<FarmField>();
            field.Init(_config, _crops, _bus, _bag, _player, grass, well);
            fieldObject.SetActive(true);
            return field;
        }

        private void Select(int slot)
        {
            _bus.Publish(new HotbarSlotSelected(slot));
        }

        private static Vector3 Center(Vector2Int cell)
        {
            return FarmGrid.CenterOf(cell, 1f, 0f);
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
