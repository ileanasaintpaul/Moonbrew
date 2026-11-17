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

        [Test]
        public void ForNextDay_DayZeroOrNegative_Throws()
        {
            var data = SaveData.CreateNew("slot_a", 1);

            Assert.Throws<ArgumentOutOfRangeException>(() => data.ForNextDay(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => data.ForNextDay(-2));
        }

        [Test]
        public void FindInventory_Missing_ReturnsNull()
        {
            Assert.IsNull(SaveData.CreateNew("slot_a", 1).FindInventory("inventory_player"));
        }
    }
}
