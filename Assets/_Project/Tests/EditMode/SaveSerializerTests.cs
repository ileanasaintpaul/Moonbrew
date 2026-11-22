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

        // A game saved before recipes could be learned (version 4) loads knowing only the starting recipes.
        [Test]
        public void TryFromJson_Version4File_MigratesWithNoLearnedRecipe()
        {
            const string version4 = "{\"version\":4,\"slotId\":\"farm\",\"isNew\":false,\"day\":8,\"lastPlayedUtc\":\"\","
                + "\"inventories\":[],\"plots\":[],\"cutGrass\":[],\"wateringCanUsed\":0,"
                + "\"stations\":[{\"id\":\"station_mill\",\"processId\":\"process_flour\",\"batches\":2,\"minutesLeft\":0}],"
                + "\"recap\":{\"day\":7,\"entries\":[]}}";

            Assert.IsTrue(SaveSerializer.TryFromJson(version4, out var loaded, out var error), error);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(2, loaded.FindStation("station_mill").batches);
            Assert.IsNotNull(loaded.learnedRecipes);
            Assert.IsEmpty(loaded.learnedRecipes);
        }

        // A game saved before stations noted the time (version 5) loads with no time heard, so nothing advances twice.
        [Test]
        public void TryFromJson_Version5File_MigratesWithNoStationTime()
        {
            const string version5 = "{\"version\":5,\"slotId\":\"farm\",\"isNew\":false,\"day\":9,\"lastPlayedUtc\":\"\","
                + "\"inventories\":[],\"plots\":[],\"cutGrass\":[],\"wateringCanUsed\":0,"
                + "\"stations\":[{\"id\":\"station_mill\",\"processId\":\"process_flour\",\"batches\":1,\"minutesLeft\":30}],"
                + "\"learnedRecipes\":[],\"recap\":{\"day\":8,\"entries\":[]}}";

            Assert.IsTrue(SaveSerializer.TryFromJson(version5, out var loaded, out var error), error);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(-1, loaded.FindStation("station_mill").minutes);
            Assert.AreEqual(30, loaded.FindStation("station_mill").minutesLeft);
        }

        // A game saved before the owner had a name and a look (version 6) loads with neither: the default look applies.
        [Test]
        public void TryFromJson_Version6File_MigratesWithNoNameAndNoLook()
        {
            const string version6 = "{\"version\":6,\"slotId\":\"farm\",\"isNew\":false,\"day\":9,\"lastPlayedUtc\":\"\","
                + "\"inventories\":[],\"plots\":[],\"cutGrass\":[],\"wateringCanUsed\":0,"
                + "\"stations\":[],\"learnedRecipes\":[],\"recap\":{\"day\":8,\"entries\":[]}}";

            Assert.IsTrue(SaveSerializer.TryFromJson(version6, out var loaded, out var error), error);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual("", loaded.ownerName);
            Assert.IsNotNull(loaded.appearance);
            Assert.AreEqual("", loaded.appearance.topId);
        }

        // A version 7 look was made of parts that no longer exist: the name stays, the look becomes the default one.
        [Test]
        public void TryFromJson_Version7File_KeepsTheNameAndDropsTheOldLook()
        {
            const string version7 = "{\"version\":7,\"slotId\":\"farm\",\"isNew\":false,\"day\":9,\"lastPlayedUtc\":\"\","
                + "\"inventories\":[],\"plots\":[],\"cutGrass\":[],\"wateringCanUsed\":0,"
                + "\"stations\":[],\"learnedRecipes\":[],\"recap\":{\"day\":8,\"entries\":[]},\"ownerName\":\"Rose\","
                + "\"appearance\":{\"bodyId\":\"body_red_dress\",\"hairId\":\"hair_wavy\",\"hairColourId\":\"hair_brown\"}}";

            Assert.IsTrue(SaveSerializer.TryFromJson(version7, out var loaded, out var error), error);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual("Rose", loaded.ownerName);
            Assert.AreEqual(("", "", ""), (loaded.appearance.topId, loaded.appearance.hairId, loaded.appearance.hairColourId));
        }

        // A game saved before the owner had pronouns (version 8) keeps its look and gets the default pronouns.
        [Test]
        public void TryFromJson_Version8File_KeepsTheLookWithDefaultPronouns()
        {
            const string version8 = "{\"version\":8,\"slotId\":\"farm\",\"isNew\":false,\"day\":9,\"lastPlayedUtc\":\"\","
                + "\"inventories\":[],\"plots\":[],\"cutGrass\":[],\"wateringCanUsed\":0,"
                + "\"stations\":[],\"learnedRecipes\":[],\"recap\":{\"day\":8,\"entries\":[]},\"ownerName\":\"Rose\","
                + "\"appearance\":{\"topId\":\"top_villager_2\",\"bodySize\":30}}";

            Assert.IsTrue(SaveSerializer.TryFromJson(version8, out var loaded, out var error), error);
            Assert.AreEqual(SaveData.CurrentVersion, loaded.version);
            Assert.AreEqual(("Rose", ""), (loaded.ownerName, loaded.ownerPronounsId));
            Assert.AreEqual(("top_villager_2", 30), (loaded.appearance.topId, loaded.appearance.bodySize));
        }

        [Test]
        public void TryFromJson_OwnerPronouns_RoundTrip()
        {
            var data = SaveData.CreateNew("slot_a", 1);
            data.ownerPronounsId = "pronouns_he";

            Assert.IsTrue(SaveSerializer.TryFromJson(SaveSerializer.ToJson(data), out var loaded, out _));

            Assert.AreEqual("pronouns_he", loaded.ownerPronounsId);
        }

        [Test]
        public void TryFromJson_OwnerNameAndLook_RoundTrip()
        {
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            data.ownerName = "Rose";
            data.appearance = new AppearanceSave
            {
                topId = "top_villager_2",
                bottomId = "bottom_villager_4",
                hairId = "hair_bob",
                browsId = "brows_3",
                beardId = "beard_full",
                hatId = "hat_cap",
                bodyType = 80,
                bodySize = -30,
                musculature = 10,
                skinColourId = "skin_tan",
                hairColourId = "hair_ginger",
                browColourId = "hair_black",
                beardColourId = "hair_grey",
                topColourId = "outfit_red",
                bottomColourId = "outfit_original",
            };

            Assert.IsTrue(SaveSerializer.TryFromJson(SaveSerializer.ToJson(data), out var loaded, out _));

            var look = loaded.appearance;
            Assert.AreEqual("Rose", loaded.ownerName);
            Assert.AreEqual(("top_villager_2", "bottom_villager_4", "hair_bob"), (look.topId, look.bottomId, look.hairId));
            Assert.AreEqual(("brows_3", "beard_full", "hat_cap"), (look.browsId, look.beardId, look.hatId));
            Assert.AreEqual((80, -30, 10), (look.bodyType, look.bodySize, look.musculature));
            Assert.AreEqual(("skin_tan", "hair_ginger", "hair_black"), (look.skinColourId, look.hairColourId, look.browColourId));
            Assert.AreEqual(("hair_grey", "outfit_red", "outfit_original"), (look.beardColourId, look.topColourId, look.bottomColourId));
        }

        [Test]
        public void ForNextDay_KeepsTheOwnerNameAndLook()
        {
            var data = SaveData.CreateNew("slot_a", 1);
            data.ownerName = "Rose";
            data.appearance.topId = "top_villager_2";
            data.appearance.bodySize = 40;

            var next = data.ForNextDay(1);

            Assert.AreEqual(("Rose", "top_villager_2", 40), (next.ownerName, next.appearance.topId, next.appearance.bodySize));
        }

        [Test]
        public void TryFromJson_LearnedRecipes_RoundTrip()
        {
            var data = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            data.learnedRecipes.Add("recipe_strawberry_cake");

            Assert.IsTrue(SaveSerializer.TryFromJson(SaveSerializer.ToJson(data), out var loaded, out _));
            CollectionAssert.AreEqual(new[] { "recipe_strawberry_cake" }, loaded.learnedRecipes);
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
