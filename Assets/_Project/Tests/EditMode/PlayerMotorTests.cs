using System;
using Moonbrew.Player;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class PlayerMotorTests
    {
        private const float Tolerance = 1e-4f;
        private const float Speed = 2f;

        [Test]
        public void PlanarVelocity_ForwardWithNoCameraYaw_MovesAlongWorldForward()
        {
            var velocity = PlayerMotor.PlanarVelocity(Vector2.up, 0f, Speed);

            AssertVector(new Vector3(0f, 0f, Speed), velocity);
        }

        [Test]
        public void PlanarVelocity_ForwardWithCameraYaw_MovesAwayFromCamera()
        {
            var velocity = PlayerMotor.PlanarVelocity(Vector2.up, 45f, Speed);

            var diagonal = Speed * Mathf.Sqrt(0.5f);
            AssertVector(new Vector3(diagonal, 0f, diagonal), velocity);
        }

        [Test]
        public void PlanarVelocity_ZeroInput_ReturnsZero()
        {
            AssertVector(Vector3.zero, PlayerMotor.PlanarVelocity(Vector2.zero, 45f, Speed));
        }

        [Test]
        public void PlanarVelocity_InputLongerThanOne_IsClampedToSpeed()
        {
            var velocity = PlayerMotor.PlanarVelocity(new Vector2(1f, 1f), 0f, Speed);

            Assert.AreEqual(Speed, velocity.magnitude, Tolerance);
        }

        [Test]
        public void PlanarVelocity_ZeroSpeed_ReturnsZero()
        {
            AssertVector(Vector3.zero, PlayerMotor.PlanarVelocity(Vector2.right, 0f, 0f));
        }

        [TestCase(-1f)]
        [TestCase(float.NaN)]
        public void PlanarVelocity_InvalidSpeed_Throws(float speed)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayerMotor.PlanarVelocity(Vector2.up, 0f, speed));
        }

        [Test]
        public void Turn_TargetFarAway_RotatesByMaxDelta()
        {
            var yaw = PlayerMotor.Turn(0f, Vector3.right, 30f);

            Assert.AreEqual(30f, yaw, Tolerance);
        }

        [Test]
        public void Turn_TargetWithinMaxDelta_ReachesTarget()
        {
            var yaw = PlayerMotor.Turn(80f, Vector3.right, 30f);

            Assert.AreEqual(90f, yaw, Tolerance);
        }

        [Test]
        public void Turn_ZeroVelocity_KeepsCurrentYaw()
        {
            Assert.AreEqual(123f, PlayerMotor.Turn(123f, Vector3.zero, 30f), Tolerance);
        }

        [Test]
        public void Turn_ZeroMaxDelta_KeepsCurrentYaw()
        {
            Assert.AreEqual(10f, PlayerMotor.Turn(10f, Vector3.back, 0f), Tolerance);
        }

        [Test]
        public void Turn_NegativeMaxDelta_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PlayerMotor.Turn(0f, Vector3.forward, -1f));
        }

        private static void AssertVector(Vector3 expected, Vector3 actual)
        {
            Assert.AreEqual(expected.x, actual.x, Tolerance, "x");
            Assert.AreEqual(expected.y, actual.y, Tolerance, "y");
            Assert.AreEqual(expected.z, actual.z, Tolerance, "z");
        }
    }
}
