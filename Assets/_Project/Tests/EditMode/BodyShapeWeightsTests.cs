using Moonbrew.Player;
using NUnit.Framework;

namespace Moonbrew.Tests.EditMode
{
    public class BodyShapeWeightsTests
    {
        [Test]
        public void Type_MasculineToFeminine_GoesFromZeroToFull()
        {
            Assert.AreEqual(0f, BodyShapeWeights.Type(-100));
            Assert.AreEqual(50f, BodyShapeWeights.Type(0));
            Assert.AreEqual(100f, BodyShapeWeights.Type(100));
        }

        [Test]
        public void Size_SlimOrHeavy_UsesOnlyTheMatchingBlendShape()
        {
            Assert.AreEqual((0f, 60f), (BodyShapeWeights.Heavy(-60), BodyShapeWeights.Skinny(-60, 100f)));
            Assert.AreEqual((40f, 0f), (BodyShapeWeights.Heavy(40), BodyShapeWeights.Skinny(40, 100f)));
            Assert.AreEqual((0f, 0f), (BodyShapeWeights.Heavy(0), BodyShapeWeights.Skinny(0, 100f)));
        }

        [Test]
        public void Skinny_Limit_ScalesTheSlimmestBody()
        {
            Assert.AreEqual(50f, BodyShapeWeights.Skinny(-100, 50f));
            Assert.AreEqual(25f, BodyShapeWeights.Skinny(-50, 50f));
        }

        [Test]
        public void Skinny_LimitOutOfRange_IsKeptWithinZeroAndFull()
        {
            Assert.AreEqual(0f, BodyShapeWeights.Skinny(-100, -20f));
            Assert.AreEqual(100f, BodyShapeWeights.Skinny(-100, 300f));
        }

        [Test]
        public void Muscle_BeyondTheLimits_StaysWithinTheWeights()
        {
            Assert.AreEqual(100f, BodyShapeWeights.Muscle(400, 0f));
            Assert.AreEqual(0f, BodyShapeWeights.Muscle(-400, 0f));
            Assert.AreEqual(100f, BodyShapeWeights.Heavy(400));
        }

        [Test]
        public void Muscle_Floor_KeepsTheLeanestBodyAboveIt()
        {
            Assert.AreEqual(40f, BodyShapeWeights.Muscle(-100, 40f));
            Assert.AreEqual(45f, BodyShapeWeights.Muscle(-50, 40f));
            Assert.AreEqual(50f, BodyShapeWeights.Muscle(0, 40f), "Half-way at 0, whatever the floor.");
            Assert.AreEqual(100f, BodyShapeWeights.Muscle(100, 40f));
        }

        [Test]
        public void Muscle_FloorOutOfRange_IsKeptWithinZeroAndHalf()
        {
            Assert.AreEqual(0f, BodyShapeWeights.Muscle(-100, -50f));
            Assert.AreEqual(50f, BodyShapeWeights.Muscle(-100, 250f));
        }
    }
}
