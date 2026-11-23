using System;
using Moonbrew.Core;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class SaveDataTests
    {
        [Test]
        public void CreateNew_ValidArguments_IsNewWithoutRecap()
        {
            var data = SaveData.CreateNew("slot_a", 1);

            Assert.IsTrue(data.isNew);
            Assert.AreEqual(1, data.day);
            Assert.IsFalse(data.HasRecap);
            Assert.IsEmpty(data.inventories);
        }

        [Test]
        public void CreateNew_EmptySlot_Throws()
        {
            Assert.Throws<ArgumentException>(() => SaveData.CreateNew(string.Empty, 1));
            Assert.Throws<ArgumentException>(() => SaveData.CreateNew(null, 1));
        }

        [Test]
        public void CreateNew_DayZero_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => SaveData.CreateNew("slot_a", 0));
        }

        [Test]
        public void ForNextDay_EndedDay_WakesUpTheNextDayWithItsRecap()
        {
            var next = SaveData.CreateNew("slot_a", 1).ForNextDay(1);

            Assert.AreEqual("slot_a", next.slotId);
            Assert.AreEqual(2, next.day);
            Assert.IsFalse(next.isNew);
            Assert.IsTrue(next.HasRecap);
            Assert.AreEqual(1, next.recap.day);
        }

        // The owner's café inventory only exists in the café: the farm scene must carry it untouched.
        [Test]
        public void ForNextDay_InventoryOfAnotherScene_IsKept()
        {
            var data = SaveData.CreateNew("slot_a", 1);
            data.inventories.Add(new InventorySave { id = "inventory_owner_cafe" });

            Assert.IsNotNull(data.ForSameDay().FindInventory("inventory_owner_cafe"));
            Assert.IsNotNull(data.ForNextDay(1).FindInventory("inventory_owner_cafe"));
        }

        [Test]
        public void ForNextDay_DayZeroOrNegative_Throws()
        {
            var data = SaveData.CreateNew("slot_a", 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => data.ForNextDay(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => data.ForNextDay(-2));
        }

        [Test]
        public void ForNextDay_CarriesTheDayStateAndDropsYesterdaysRecap()
        {
            var morning = SaveData.CreateNew("slot_a", 1).ForNextDay(1);
            morning.recap.Add("inventory_player", "crop_wheat", 5);
            morning.plots.Add(new PlotSave { x = 1, y = 2, cropId = "crop_wheat" });
            morning.learnedRecipes.Add("recipe_cake");

            var next = morning.ForNextDay(2);

            Assert.AreEqual(3, next.day);
            Assert.AreEqual(2, next.recap.day);
            Assert.IsEmpty(next.recap.entries, "Yesterday's recap was carried into today's.");
            Assert.AreEqual(1, next.plots.Count);
            CollectionAssert.AreEqual(new[] { "recipe_cake" }, next.learnedRecipes);
        }

        [Test]
        public void ForNextDay_AfterTravel_KeepsTodaysRecap()
        {
            var carried = SaveData.CreateNew("slot_a", 1).ForSameDay();
            carried.recap.Add("inventory_player", "crop_wheat", 4);

            var next = carried.ForNextDay(1);

            Assert.AreEqual(1, next.recap.day);
            Assert.AreEqual(4, next.recap.entries[0].delta);
        }

        [Test]
        public void ForSameDay_SavedMorning_OpensWithoutRecapOnTheSameDay()
        {
            var morning = SaveData.CreateNew("slot_a", 1).ForNextDay(3);
            morning.recap.Add("inventory_player", "crop_wheat", 2);

            var same = morning.ForSameDay();

            Assert.AreEqual(4, same.day);
            Assert.IsFalse(same.HasRecap);
            Assert.IsEmpty(same.recap.entries);
        }

        [Test]
        public void ForSameDay_NewGame_IsNoLongerNew()
        {
            Assert.IsFalse(SaveData.CreateNew("slot_a", 1).ForSameDay().isNew);
        }

        [Test]
        public void ForSameDay_ChangingTheCopy_LeavesTheOriginal()
        {
            var original = SaveData.CreateNew("slot_a", 1);
            original.inventories.Add(new InventorySave { id = "inventory_player" });

            var copy = original.ForSameDay();
            copy.inventories.Clear();
            copy.cutGrass.Add("grass_1");

            Assert.AreEqual(1, original.inventories.Count);
            Assert.IsEmpty(original.cutGrass);
        }

        [Test]
        public void RecapAdd_SameItemTwice_MergesTheChanges()
        {
            var recap = new DayRecap();

            recap.Add("inventory_player", "crop_wheat", 3);
            recap.Add("inventory_player", "crop_wheat", -1);
            recap.Add("inventory_cafe", "crop_wheat", 2);

            Assert.AreEqual(2, recap.entries.Count);
            Assert.AreEqual(2, recap.entries.Find(e => e.inventoryId == "inventory_player").delta);
        }

        [Test]
        public void RecapAdd_ChangesCancelOut_DropsTheEntry()
        {
            var recap = new DayRecap();

            recap.Add("inventory_player", "crop_wheat", 3);
            recap.Add("inventory_player", "crop_wheat", -3);

            Assert.IsEmpty(recap.entries);
        }

        [Test]
        public void RecapAdd_ZeroChange_AddsNothing()
        {
            var recap = new DayRecap();

            recap.Add("inventory_player", "crop_wheat", 0);

            Assert.IsEmpty(recap.entries);
        }

        [Test]
        public void FindInventory_Missing_ReturnsNull()
        {
            Assert.IsNull(SaveData.CreateNew("slot_a", 1).FindInventory("inventory_player"));
        }
    }
}
