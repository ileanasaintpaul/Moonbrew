using System.Collections.Generic;
using Moonbrew.Inventory;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class InventoryRecapTests
    {
        [Test]
        public void Between_GainsAndLosses_ListsEachChangedItem()
        {
            var morning = new Dictionary<string, int> { ["crop_wheat"] = 2, ["ingredient_flour"] = 5, ["seed_wheat"] = 10 };
            var evening = new Dictionary<string, int> { ["crop_wheat"] = 14, ["ingredient_flour"] = 5, ["dish_wheat_cake"] = 1 };

            var entries = InventoryRecap.Between("inventory_player", morning, evening);

            Assert.AreEqual(3, entries.Count);
            Assert.AreEqual(("crop_wheat", 12), (entries[0].itemId, entries[0].delta));
            Assert.AreEqual(("dish_wheat_cake", 1), (entries[1].itemId, entries[1].delta));
            Assert.AreEqual(("seed_wheat", -10), (entries[2].itemId, entries[2].delta));
            Assert.IsTrue(entries.TrueForAll(entry => entry.inventoryId == "inventory_player"));
        }

        [Test]
        public void Between_NothingChanged_IsEmpty()
        {
            var totals = new Dictionary<string, int> { ["crop_wheat"] = 3 };

            Assert.IsEmpty(InventoryRecap.Between("inventory_player", totals, new Dictionary<string, int>(totals)));
        }

        [Test]
        public void Between_NullSnapshots_AreTreatedAsEmpty()
        {
            Assert.IsEmpty(InventoryRecap.Between("inventory_player", null, null));
            Assert.AreEqual(4, InventoryRecap.Between("inventory_player", null, new Dictionary<string, int> { ["crop_wheat"] = 4 })[0].delta);
        }
    }
}
