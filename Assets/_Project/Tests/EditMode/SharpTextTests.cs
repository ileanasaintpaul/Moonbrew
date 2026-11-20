using Moonbrew.Rendering;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class SharpTextTests
    {
        [Test]
        public void Hide_Text_IsHiddenAndKeepsTheText()
        {
            var hidden = SharpText.Hide("06:00");

            Assert.IsTrue(SharpText.IsHidden(hidden));
            StringAssert.EndsWith("06:00", hidden);
        }

        [Test]
        public void Hide_Null_GivesAHiddenEmptyText()
        {
            Assert.IsTrue(SharpText.IsHidden(SharpText.Hide(null)));
        }

        [Test]
        public void IsHidden_PlainText_IsFalse()
        {
            Assert.IsFalse(SharpText.IsHidden("Day 12"));
        }

        [Test]
        public void IsHidden_NullOrEmpty_IsFalse()
        {
            Assert.IsFalse(SharpText.IsHidden(null));
            Assert.IsFalse(SharpText.IsHidden(string.Empty));
        }
    }
}
