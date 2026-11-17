using System;
using Moonbrew.Player;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class HotbarTests
    {
        [Test]
        public void Constructor_ZeroSlots_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => new Hotbar(0));
        }

        [Test]
        public void Constructor_NewHotbar_SelectsFirstSlot()
        {
            Assert.AreEqual(0, new Hotbar(10).SelectedIndex);
        }

        [Test]
        public void Select_ValidSlot_ChangesSelection()
        {
            var hotbar = new Hotbar(10);

            Assert.IsTrue(hotbar.Select(4));
            Assert.AreEqual(4, hotbar.SelectedIndex);
        }

        [Test]
        public void Select_SameSlot_ReturnsFalse()
        {
            var hotbar = new Hotbar(10);
            hotbar.Select(4);

            Assert.IsFalse(hotbar.Select(4));
        }

        [Test]
        public void Select_NegativeOrPastTheEnd_IsIgnored()
        {
            var hotbar = new Hotbar(10);
            hotbar.Select(2);

            Assert.IsFalse(hotbar.Select(-1));
            Assert.IsFalse(hotbar.Select(10));
            Assert.AreEqual(2, hotbar.SelectedIndex);
        }

        [Test]
        public void SelectFromKey_One_SelectsFirstSlot()
        {
            var hotbar = new Hotbar(10);
            hotbar.Select(5);

            Assert.IsTrue(hotbar.SelectFromKey(1f));
            Assert.AreEqual(0, hotbar.SelectedIndex);
        }

        [Test]
        public void SelectFromKey_Ten_SelectsTenthSlot()
        {
            var hotbar = new Hotbar(10);

            hotbar.SelectFromKey(10f);

            Assert.AreEqual(9, hotbar.SelectedIndex);
        }

        [Test]
        public void SelectFromKey_ZeroMeansReleased_KeepsSelection()
        {
            var hotbar = new Hotbar(10);
            hotbar.Select(3);

            Assert.IsFalse(hotbar.SelectFromKey(0f));
            Assert.AreEqual(3, hotbar.SelectedIndex);
        }

        [Test]
        public void SelectFromKey_KeyBeyondSmallerHotbar_IsIgnored()
        {
            var hotbar = new Hotbar(4);

            Assert.IsFalse(hotbar.SelectFromKey(7f));
            Assert.AreEqual(0, hotbar.SelectedIndex);
        }
    }
}
