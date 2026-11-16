using System;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class PixelSnapTests
    {
        private const float Tolerance = 1e-4f;
        private const float Texel = 0.05f;

        [Test]
        public void Snap_PositionOnGrid_KeepsPositionWithNoRemainder()
        {
            var result = PixelSnap.Snap(new Vector3(1f, 2f, -10f), Quaternion.identity, Texel);

            AssertVector(new Vector3(1f, 2f, -10f), result.Position);
            AssertVector(Vector2.zero, result.Remainder);
        }

        [Test]
        public void Snap_PositionBetweenTexels_RoundsToNearestAndReturnsRemainder()
        {
            var result = PixelSnap.Snap(new Vector3(1.01f, 2.04f, -10f), Quaternion.identity, Texel);

            AssertVector(new Vector3(1f, 2.05f, -10f), result.Position);
            AssertVector(new Vector2(0.2f, -0.2f), result.Remainder);
        }

        [Test]
        public void Snap_NegativeCoordinates_RoundsSymmetrically()
        {
            var result = PixelSnap.Snap(new Vector3(-1.01f, -2.04f, 0f), Quaternion.identity, Texel);

            AssertVector(new Vector3(-1f, -2.05f, 0f), result.Position);
            AssertVector(new Vector2(-0.2f, 0.2f), result.Remainder);
        }

        [Test]
        public void Snap_RotatedCamera_SnapsAlongCameraAxesAndKeepsDepth()
        {
            var rotation = Quaternion.Euler(35f, 45f, 0f);
            var onGrid = rotation * new Vector3(4f * Texel, 7f * Texel, -20f);
            var offset = rotation * new Vector3(0.3f * Texel, -0.1f * Texel, 0.123f);

            var result = PixelSnap.Snap(onGrid + offset, rotation, Texel);

            var local = Quaternion.Inverse(rotation) * result.Position;
            AssertVector(new Vector3(4f * Texel, 7f * Texel, -20f + 0.123f), local);
            AssertVector(new Vector2(0.3f, -0.1f), result.Remainder);
        }

        [Test]
        public void Snap_RemainderNeverExceedsHalfTexel()
        {
            for (var i = 0; i < 100; i++)
            {
                var result = PixelSnap.Snap(new Vector3(i * 0.0137f, i * -0.0291f, 0f), Quaternion.identity, Texel);

                Assert.LessOrEqual(Mathf.Abs(result.Remainder.x), 0.5f + Tolerance);
                Assert.LessOrEqual(Mathf.Abs(result.Remainder.y), 0.5f + Tolerance);
            }
        }

        [TestCase(0f)]
        [TestCase(-0.05f)]
        [TestCase(float.NaN)]
        [TestCase(float.PositiveInfinity)]
        public void Snap_InvalidTexelSize_Throws(float texelSize)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PixelSnap.Snap(Vector3.zero, Quaternion.identity, texelSize));
        }

        [Test]
        public void TexelSize_OrthographicSizeAndHeight_ReturnsWorldUnitsPerTexel()
        {
            Assert.AreEqual(0.05f, PixelSnap.TexelSize(9f, 360), Tolerance);
        }

        [TestCase(0f, 360)]
        [TestCase(-9f, 360)]
        [TestCase(9f, 0)]
        [TestCase(9f, -360)]
        public void TexelSize_NonPositiveArguments_Throws(float orthographicSize, int renderHeight)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PixelSnap.TexelSize(orthographicSize, renderHeight));
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, "x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, "y");
            Assert.AreEqual(expected.z, actual.z, Tolerance, "z");
        }

        private static void AssertVector(Vector2 expected, Vector2 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, "x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, "y");
        }
    }
}
