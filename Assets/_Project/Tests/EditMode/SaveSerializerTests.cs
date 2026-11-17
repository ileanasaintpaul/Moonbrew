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

        // Version 1 is the first format: this pins it, so a future version 2 must keep loading it through a migration.
        [Test]
        public void TryFromJson_Version1File_Loads()
        {
            const string version1 = "{\"version\":1,\"slotId\":\"old\",\"isNew\":false,\"day\":5,\"lastPlayedUtc\":\"\","
                + "\"inventories\":[{\"id\":\"inventory_player\",\"slots\":[{\"index\":0,\"itemId\":\"seed_wheat\",\"quantity\":7}]}],"
                + "\"recap\":{\"day\":4,\"entries\":[]}}";

            Assert.IsTrue(SaveSerializer.TryFromJson(version1, out var loaded, out var error), error);
            Assert.AreEqual(5, loaded.day);
            Assert.AreEqual(7, loaded.FindInventory("inventory_player").slots[0].quantity);
        }

        [Test]
        public void ToJson_Null_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => SaveSerializer.ToJson(null));
        }
    }
}
