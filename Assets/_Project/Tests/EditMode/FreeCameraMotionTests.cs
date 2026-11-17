using System;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class FreeCameraMotionTests
    {
        private const float Tolerance = 1e-4f;

        [Test]
        public void Pan_ForwardAtReferenceZoom_MovesAtSpeed()
        {
            var offset = FreeCameraMotion.Pan(Vector2.up, 0f, 8f, 10f, 10f, 0.5f);

            Assert.AreEqual(0f, offset.x, Tolerance);
            Assert.AreEqual(4f, offset.z, Tolerance);
        }

        [Test]
        public void Pan_ZoomedOut_MovesFaster()
        {
            var offset = FreeCameraMotion.Pan(Vector2.up, 0f, 8f, 20f, 10f, 0.5f);

            Assert.AreEqual(8f, offset.z, Tolerance);
        }

        [Test]
        public void Pan_CameraYaw_FollowsTheCamera()
        {
            var offset = FreeCameraMotion.Pan(Vector2.up, 90f, 8f, 10f, 10f, 1f);

            Assert.AreEqual(8f, offset.x, Tolerance);
            Assert.AreEqual(0f, offset.z, Tolerance);
        }

        [Test]
        public void Pan_ZeroInput_DoesNotMove()
        {
            Assert.AreEqual(Vector3.zero, FreeCameraMotion.Pan(Vector2.zero, 45f, 8f, 10f, 10f, 1f));
        }

        [TestCase(-1f, 10f, 1f)]
        [TestCase(8f, 0f, 1f)]
        [TestCase(8f, 10f, -1f)]
        public void Pan_InvalidValues_Throw(float speed, float zoom, float deltaTime)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => FreeCameraMotion.Pan(Vector2.up, 0f, speed, zoom, 10f, deltaTime));
        }

        [Test]
        public void Zoom_PositiveInput_ZoomsIn()
        {
            Assert.AreEqual(7.5f, FreeCameraMotion.Zoom(8f, 120f, 0.5f, 3f, 20f), Tolerance);
        }

        [Test]
        public void Zoom_NegativeInput_ZoomsOut()
        {
            Assert.AreEqual(8.5f, FreeCameraMotion.Zoom(8f, -1f, 0.5f, 3f, 20f), Tolerance);
        }

        [Test]
        public void Zoom_PastTheLimits_IsClamped()
        {
            Assert.AreEqual(3f, FreeCameraMotion.Zoom(3.2f, 1f, 0.5f, 3f, 20f), Tolerance);
            Assert.AreEqual(20f, FreeCameraMotion.Zoom(19.8f, -1f, 0.5f, 3f, 20f), Tolerance);
        }

        [Test]
        public void Zoom_ZeroInput_KeepsTheZoom()
        {
            Assert.AreEqual(8f, FreeCameraMotion.Zoom(8f, 0f, 0.5f, 3f, 20f), Tolerance);
        }

        [Test]
        public void Zoom_MinAboveMax_Throws()
        {
            Assert.Throws<ArgumentException>(() => FreeCameraMotion.Zoom(8f, 1f, 0.5f, 20f, 3f));
        }
    }
}
