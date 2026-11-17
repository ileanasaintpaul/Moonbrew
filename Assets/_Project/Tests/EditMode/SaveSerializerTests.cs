using System;
using Moonbrew.Core;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class SaveSerializerTests
    {
        [Test]
        public void TryFromJson_RoundTrip_KeepsEverything()
        {
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(3);
            data.lastPlayedUtc = "2026-11-17T10:00:00.0000000Z";
            data.inventories.Add(new InventorySave { id = "inventory_player", slots = { new SlotSave { index = 2, itemId = "crop_wheat", quantity = 12 } } });
            data.recap.entries.Add(new RecapEntry { inventoryId = "inventory_player", itemId = "crop_wheat", delta = 12 });

            Assert.IsTrue(SaveSerializer.TryFromJson(SaveSerializer.ToJson(data), out var loaded, out var error), error);

            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual("slot_a", loaded.slotId);
            Assert.AreEqual(4, loaded.day);
            Assert.IsFalse(loaded.isNew);
            Assert.AreEqual(data.lastPlayedUtc, loaded.lastPlayedUtc);
            var slot = loaded.FindInventory("inventory_player").slots[0];
            Assert.AreEqual((2, "crop_wheat", 12), (slot.index, slot.itemId, slot.quantity));
            Assert.AreEqual(3, loaded.recap.day);
            Assert.AreEqual(12, loaded.recap.entries[0].delta);
        }

        [Test]
        public void TryFromJson_NewGame_HasNoRecap()
        {
            var json = SaveSerializer.ToJson(SaveData.CreateNew("slot_a", 1));

            Assert.IsTrue(SaveSerializer.TryFromJson(json, out var loaded, out _));
            Assert.IsTrue(loaded.isNew);
            Assert.IsFalse(loaded.HasRecap);
        }

        [Test]
        public void TryFromJson_NullOrEmpty_ReturnsFalse()
        {
            Assert.IsFalse(SaveSerializer.TryFromJson(null, out var data, out var error));
            Assert.IsNull(data);
            Assert.IsNotEmpty(error);
            Assert.IsFalse(SaveSerializer.TryFromJson("   ", out _, out _));
        }

        [Test]
        public void TryFromJson_CorruptText_ReturnsFalseWithoutThrowing()
        {
            Assert.IsFalse(SaveSerializer.TryFromJson("{ \"version\": 1, \"day\": ", out var data, out var error));
            Assert.IsNull(data);
            Assert.IsNotEmpty(error);
        }

        [Test]
        public void TryFromJson_NoVersion_ReturnsFalse()
        {
            Assert.IsFalse(SaveSerializer.TryFromJson("{ \"slotId\": \"a\", \"day\": 2 }", out _, out _));
        }

        [Test]
        public void TryFromJson_NewerVersion_ReturnsFalse()
        {
            var json = $"{{ \"version\": {SaveData.CurrentVersion + 1}, \"slotId\": \"a\", \"day\": 2 }}";

            Assert.IsFalse(SaveSerializer.TryFromJson(json, out _, out var error));
            StringAssert.Contains("newer", error);
        }

        [Test]
        public void TryFromJson_MissingSlotOrDay_ReturnsFalse()
        {
            Assert.IsFalse(SaveSerializer.TryFromJson("{ \"version\": 1, \"day\": 2 }", out _, out _));
            Assert.IsFalse(SaveSerializer.TryFromJson("{ \"version\": 1, \"slotId\": \"a\", \"day\": 0 }", out _, out _));
        }

        // A game saved before the farm plots existed (version 1) still loads, with an empty farm.
        [Test]
        public void TryFromJson_Version1File_MigratesToCurrentWithNoPlots()
        {
            const string version1 = "{\"version\":1,\"slotId\":\"old\",\"isNew\":false,\"day\":5,\"lastPlayedUtc\":\"\","
                + "\"inventories\":[{\"id\":\"inventory_player\",\"slots\":[{\"index\":0,\"itemId\":\"seed_wheat\",\"quantity\":7}]}],"
                + "\"recap\":{\"day\":4,\"entries\":[]}}";

            Assert.IsTrue(SaveSerializer.TryFromJson(version1, out var loaded, out var error), error);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(5, loaded.day);
            Assert.AreEqual(7, loaded.FindInventory("inventory_player").slots[0].quantity);
            Assert.IsNotNull(loaded.plots);
            Assert.IsEmpty(loaded.plots);
        }

        // A game saved before the watering can existed (version 2) loads with a full can and keeps its farm.
        [Test]
        public void TryFromJson_Version2File_MigratesWithAFullCan()
        {
            const string version2 = "{\"version\":2,\"slotId\":\"farm\",\"isNew\":false,\"day\":3,\"lastPlayedUtc\":\"\","
                + "\"inventories\":[],\"plots\":[{\"x\":4,\"y\":5,\"cropId\":\"wheat\",\"daysGrown\":1}],"
                + "\"cutGrass\":[\"grass_1.00_2.00\"],\"recap\":{\"day\":2,\"entries\":[]}}";

            Assert.IsTrue(SaveSerializer.TryFromJson(version2, out var loaded, out var error), error);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(0, loaded.wateringCanUsed);
            Assert.AreEqual("wheat", loaded.plots[0].cropId);
            Assert.AreEqual("grass_1.00_2.00", loaded.cutGrass[0]);
        }

        // A game saved before the stations existed (version 3) loads with an idle mill and keeps its can.
        [Test]
        public void TryFromJson_Version3File_MigratesWithIdleStations()
        {
            const string version3 = "{\"version\":3,\"slotId\":\"farm\",\"isNew\":false,\"day\":6,\"lastPlayedUtc\":\"\","
                + "\"inventories\":[],\"plots\":[],\"cutGrass\":[],\"wateringCanUsed\":4,\"recap\":{\"day\":5,\"entries\":[]}}";

            Assert.IsTrue(SaveSerializer.TryFromJson(version3, out var loaded, out var error), error);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(4, loaded.wateringCanUsed);
            Assert.IsNotNull(loaded.stations);
            Assert.IsEmpty(loaded.stations);
            Assert.IsNull(loaded.FindStation("station_mill"));
        }

        [Test]
        public void TryFromJson_Stations_RoundTrip()
        {
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            data.stations.Add(new StationSave { id = "station_mill", processId = "process_flour", batches = 6, minutesLeft = 25 });

            Assert.IsTrue(SaveSerializer.TryFromJson(SaveSerializer.ToJson(data), out var loaded, out _));

            var mill = loaded.FindStation("station_mill");
            Assert.AreEqual(("process_flour", 6, 25), (mill.processId, mill.batches, mill.minutesLeft));
        }

        [Test]
        public void TryFromJson_WateringCan_RoundTrip()
        {
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            data.wateringCanUsed = 7;

            Assert.IsTrue(SaveSerializer.TryFromJson(SaveSerializer.ToJson(data), out var loaded, out _));
            Assert.AreEqual(7, loaded.wateringCanUsed);
        }

        [Test]
        public void TryFromJson_Version2Plots_RoundTrip()
        {
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            data.plots.Add(new PlotSave { x = -3, y = 7, cropId = "wheat", daysGrown = 2 });
            data.plots.Add(new PlotSave { x = 4, y = 0, cropId = string.Empty });

            Assert.IsTrue(SaveSerializer.TryFromJson(SaveSerializer.ToJson(data), out var loaded, out _));

            Assert.AreEqual(2, loaded.plots.Count);
            Assert.AreEqual((-3, 7, "wheat", 2), (loaded.plots[0].x, loaded.plots[0].y, loaded.plots[0].cropId, loaded.plots[0].daysGrown));
            Assert.AreEqual(string.Empty, loaded.plots[1].cropId);
        }

        [Test]
        public void ToJson_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SaveSerializer.ToJson(null));
        }
    }
}
