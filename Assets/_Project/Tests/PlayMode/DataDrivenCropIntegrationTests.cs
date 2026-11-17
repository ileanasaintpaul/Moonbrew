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
    // The fruit is pure data: with the real game assets, and no line of code about strawberries,
    // it is planted, watered, grown, harvested and grows back (VS-21).
    public class DataDrivenCropIntegrationTests
    {
        private const string DataFolder = "Assets/_Project/Data/";

        private static readonly Vector2Int Cell = new Vector2Int(1, 0);

        private readonly List<Object> _created = new List<Object>();

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
        public IEnumerator Strawberry_FromTheRealAssets_GoesThroughTheWholeCycle()
        {
#if UNITY_EDITOR
            var config = Load<FarmConfig>("Crops/SO_FarmConfig.asset");
            var crops = Load<CropCatalog>("Crops/SO_CropCatalog.asset");
            var items = Load<ItemCatalog>("Items/SO_ItemCatalog.asset");
            var bagConfig = Load<InventoryConfig>("Inventories/SO_Inventory_Player.asset");
            var strawberry = crops.Find("strawberry");
            Assert.IsNotNull(strawberry, "The strawberry crop is missing from the crop catalog.");

            var bus = Create(ScriptableObject.CreateInstance<EventBusAsset>());
            var bagObject = Create(new GameObject("Bag"));
            bagObject.SetActive(false);
            var bag = bagObject.AddComponent<InventoryHolder>();
            bag.Init(bagConfig, items, bus);
            bagObject.SetActive(true);
            bag.Inventory.Add(config.Hoe, 1);
            bag.Inventory.Add(strawberry.Seed, 1);
            bag.Inventory.Add(config.WateringCan, 1);

            var player = Create(new GameObject("Player")).transform;
            player.position = new Vector3(0.5f, 0f, 0.5f);
            var fieldObject = Create(new GameObject("Farm Field"));
            fieldObject.SetActive(false);
            var field = fieldObject.AddComponent<FarmField>();
            field.Init(config, crops, bus, bag, player);
            fieldObject.SetActive(true);
            field.SetRandom(new System.Random(3));
            yield return new WaitForFixedUpdate();

            var cellCenter = FarmGrid.CenterOf(Cell, config.CellSize, 0f);
            bus.Publish(new HotbarSlotSelected(0));
            Assert.IsTrue(field.UseAt(cellCenter), "The hoe did not till.");
            bus.Publish(new HotbarSlotSelected(1));
            Assert.IsTrue(field.UseAt(cellCenter), "The strawberry seed was not planted.");
            Assert.AreEqual(0, bag.Inventory.Count(strawberry.Seed));

            bus.Publish(new HotbarSlotSelected(2));
            for (var night = 1; night <= strawberry.GrowthDays; night++)
            {
                Assert.IsFalse(field.Plots.IsRipe(Cell, strawberry.GrowthDays), $"Ripe too early, before night {night}.");
                field.UseAt(cellCenter);
                bus.Publish(new NightPassed(night));
            }

            Assert.IsTrue(field.Plots.IsRipe(Cell, strawberry.GrowthDays));
            Assert.IsTrue(field.UseAt(cellCenter), "The ripe strawberries were not picked.");
            Assert.That(bag.Inventory.Count(strawberry.Harvest), Is.InRange(strawberry.HarvestMin, strawberry.HarvestMax));

            field.Plots.TryGet(Cell, out var plot);
            Assert.AreEqual(strawberry.Id, plot.CropId, "The strawberry plant should stay to grow back.");
            for (var night = 1; night <= strawberry.RegrowDays; night++)
            {
                field.UseAt(cellCenter);
                bus.Publish(new NightPassed(100 + night));
            }

            Assert.IsTrue(field.Plots.IsRipe(Cell, strawberry.GrowthDays), "The strawberries did not grow back.");
            yield return null;
#else
            Assert.Ignore("The game assets are loaded from the editor asset database.");
            yield return null;
#endif
        }

#if UNITY_EDITOR
        private static T Load<T>(string path) where T : Object
        {
            var asset = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(DataFolder + path);
            Assert.IsNotNull(asset, $"Missing asset {DataFolder + path}.");
            return asset;
        }
#endif

        private T Create<T>(T created) where T : Object
        {
            _created.Add(created);
            return created;
        }
    }
}
