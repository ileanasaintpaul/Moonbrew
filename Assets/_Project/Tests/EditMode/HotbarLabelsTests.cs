using Moonbrew.Inventory;
using Moonbrew.Ui;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class HotbarLabelsTests
    {
        private ItemData _item;

        [SetUp]
        public void SetUp()
        {
            _item = ScriptableObject.CreateInstance<ItemData>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(_item);
        }

        [Test]
        public void Key_FirstSlots_FollowTheNumberRow()
        {
            Assert.AreEqual("1", HotbarLabels.Key(0));
            Assert.AreEqual("9", HotbarLabels.Key(8));
        }

        [Test]
        public void Key_TenthSlot_IsZero()
        {
            Assert.AreEqual("0", HotbarLabels.Key(9));
        }

        [Test]
        public void Key_NegativeIndex_IsEmpty()
        {
            Assert.AreEqual(string.Empty, HotbarLabels.Key(-1));
        }

        [Test]
        public void Count_SeveralItems_ShowsQuantity()
        {
            Assert.AreEqual("12", HotbarLabels.Count(new ItemStack(_item, 12)));
        }

        [Test]
        public void Count_SingleItem_ShowsNothing()
        {
            Assert.AreEqual(string.Empty, HotbarLabels.Count(new ItemStack(_item, 1)));
        }

        [Test]
        public void Count_EmptySlot_ShowsNothing()
        {
            Assert.AreEqual(string.Empty, HotbarLabels.Count(default));
            Assert.AreEqual(string.Empty, HotbarLabels.Count(new ItemStack(null, 5)));
        }

        [Test]
        public void FillPercent_HalfFull_IsFifty()
        {
            Assert.AreEqual(50f, HotbarLabels.FillPercent(10, 20), 1e-4f);
        }

        [Test]
        public void FillPercent_EmptyOrFull_IsZeroOrHundred()
        {
            Assert.AreEqual(0f, HotbarLabels.FillPercent(0, 20));
            Assert.AreEqual(100f, HotbarLabels.FillPercent(20, 20));
        }

        [Test]
        public void FillPercent_OutOfRangeOrNoCapacity_IsClamped()
        {
            Assert.AreEqual(100f, HotbarLabels.FillPercent(30, 20));
            Assert.AreEqual(0f, HotbarLabels.FillPercent(-4, 20));
            Assert.AreEqual(0f, HotbarLabels.FillPercent(5, 0));
        }
    }
}
