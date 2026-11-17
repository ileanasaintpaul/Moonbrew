using System;
using Moonbrew.Farming;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class WateringCanTests
    {
        [Test]
        public void Constructor_NothingUsed_IsFull()
        {
            var can = new WateringCan(20);

            Assert.AreEqual(20, can.Water);
            Assert.AreEqual(0, can.Used);
        }

        [Test]
        public void Constructor_SomeUsed_HasTheRest()
        {
            Assert.AreEqual(12, new WateringCan(20, 8).Water);
        }

        [Test]
        public void Constructor_UsedOutOfRange_IsClamped()
        {
            Assert.AreEqual(0, new WateringCan(20, 50).Water);
            Assert.AreEqual(20, new WateringCan(20, -5).Water);
        }

        [Test]
        public void Constructor_ZeroCapacity_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new WateringCan(0));
        }

        [Test]
        public void Pour_WithWater_UsesOne()
        {
            var can = new WateringCan(2);

            Assert.IsTrue(can.Pour());
            Assert.AreEqual(1, can.Water);
        }

        [Test]
        public void Pour_Empty_ReturnsFalse()
        {
            var can = new WateringCan(1, 1);

            Assert.IsTrue(can.IsEmpty);
            Assert.IsFalse(can.Pour());
            Assert.AreEqual(0, can.Water);
        }

        [Test]
        public void Refill_PartlyEmpty_FillsToCapacity()
        {
            var can = new WateringCan(20, 15);

            Assert.IsTrue(can.Refill());
            Assert.AreEqual(20, can.Water);
        }

        [Test]
        public void Refill_AlreadyFull_ReturnsFalse()
        {
            Assert.IsFalse(new WateringCan(20).Refill());
        }
    }
}
