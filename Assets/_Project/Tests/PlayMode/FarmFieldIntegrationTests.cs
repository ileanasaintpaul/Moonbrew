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

        private static readonly Vector2Int PlayerCell = Vector2Int.zero;
        private static readonly Vector2Int NextCell = new Vector2Int(1, 0);

        private readonly List<Object> _created = new List<Object>();
        private EventBusAsset _bus;
        private ItemData _hoe;
        private ItemData _seeds;
        private ItemData _sickle;
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

            var stage = Create(GameObject.CreatePrimitive(PrimitiveType.Cylinder));
            stage.SetActive(false);
            var crop = Create(ScriptableObject.CreateInstance<CropData>());
            crop.Init("wheat", _seeds, null, 4, new[] { stage, stage });
            _crops = Create(ScriptableObject.CreateInstance<CropCatalog>());
            _crops.Init(new[] { crop });

            var soil = Create(GameObject.CreatePrimitive(PrimitiveType.Quad));
            soil.SetActive(false);
            _config = Create(ScriptableObject.CreateInstance<FarmConfig>());
            _config.Init(_hoe, _sickle, soil);

            var bagObject = Create(new GameObject("Bag"));
            bagObject.SetActive(false);
            var bagConfig = Create(ScriptableObject.CreateInstance<InventoryConfig>());
            JsonUtility.FromJsonOverwrite("{\"id\":\"inventory_player\",\"slotCount\":10}", bagConfig);
            _bag = bagObject.AddComponent<InventoryHolder>();
            var items = Create(ScriptableObject.CreateInstance<ItemCatalog>());
            items.Init(new[] { _hoe, _seeds, _sickle });
            _bag.Init(bagConfig, items, _bus);
            bagObject.SetActive(true);
            _bag.Inventory.Add(_hoe, 1);
            _bag.Inventory.Add(_seeds, 5);
            _bag.Inventory.Add(_sickle, 1);

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
            var fieldObject = Create(new GameObject("Farm Field"));
            fieldObject.SetActive(false);
            var field = fieldObject.AddComponent<FarmField>();
            field.Init(_config, _crops, _bus, _bag, _player, grass);
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
