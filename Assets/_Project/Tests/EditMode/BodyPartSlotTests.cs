using Moonbrew.Player;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class BodyPartSlotTests
    {
        [Test]
        public void Of_ModelName_GivesItsBodyPartCode()
        {
            Assert.AreEqual("TORS", BodyPartSlot.Of("SK_FANT_VILL_02_10TORS_HU01"));
            Assert.AreEqual("HAIR", BodyPartSlot.Of("SK_HUMN_BASE_07_02HAIR_HU01"));
        }

        [Test]
        public void Of_NameWithoutCode_IsNull()
        {
            Assert.IsNull(BodyPartSlot.Of("Hat"));
            Assert.IsNull(BodyPartSlot.Of("SK_BaseModel"));
        }

        [Test]
        public void Of_NullOrEmpty_IsNull()
        {
            Assert.IsNull(BodyPartSlot.Of(null));
            Assert.IsNull(BodyPartSlot.Of(""));
        }
    }
}
