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
            Assert.AreEqual((0f, 60f), (BodyShapeWeights.Heavy(-60), BodyShapeWeights.Skinny(-60)));
            Assert.AreEqual((40f, 0f), (BodyShapeWeights.Heavy(40), BodyShapeWeights.Skinny(40)));
            Assert.AreEqual((0f, 0f), (BodyShapeWeights.Heavy(0), BodyShapeWeights.Skinny(0)));
        }

        [Test]
        public void Muscle_BeyondTheLimits_StaysWithinTheWeights()
        {
            Assert.AreEqual(100f, BodyShapeWeights.Muscle(400));
            Assert.AreEqual(0f, BodyShapeWeights.Muscle(-400));
            Assert.AreEqual(100f, BodyShapeWeights.Heavy(400));
        }
    }
}
