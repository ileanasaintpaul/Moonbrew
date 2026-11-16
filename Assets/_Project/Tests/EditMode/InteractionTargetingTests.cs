using System;
using System.Collections.Generic;
using Moonbrew.Player;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class InteractionTargetingTests
    {
        private const float Radius = 1.5f;

        [Test]
        public void FindClosest_SeveralInRange_ReturnsClosest()
        {
            var candidates = new List<Vector3> { new Vector3(1.2f, 0f, 0f), new Vector3(0f, 0f, 0.5f), new Vector3(-1f, 0f, 0f) };

            Assert.AreEqual(1, InteractionTargeting.FindClosest(Vector3.zero, candidates, Radius));
        }

        [Test]
        public void FindClosest_AllOutOfRange_ReturnsMinusOne()
        {
            var candidates = new List<Vector3> { new Vector3(2f, 0f, 0f), new Vector3(0f, 0f, -3f) };

            Assert.AreEqual(-1, InteractionTargeting.FindClosest(Vector3.zero, candidates, Radius));
        }

        [Test]
        public void FindClosest_HeightDifference_IsIgnored()
        {
            var candidates = new List<Vector3> { new Vector3(0f, 5f, 1f) };

            Assert.AreEqual(0, InteractionTargeting.FindClosest(Vector3.zero, candidates, Radius));
        }

        [Test]
        public void FindClosest_EmptyList_ReturnsMinusOne()
        {
            Assert.AreEqual(-1, InteractionTargeting.FindClosest(Vector3.zero, new List<Vector3>(), Radius));
        }

        [Test]
        public void FindClosest_ZeroRadius_OnlyMatchesSamePosition()
        {
            var candidates = new List<Vector3> { new Vector3(0.1f, 0f, 0f), Vector3.zero };

            Assert.AreEqual(1, InteractionTargeting.FindClosest(Vector3.zero, candidates, 0f));
        }

        [Test]
        public void FindClosest_NullList_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => InteractionTargeting.FindClosest(Vector3.zero, null, Radius));
        }
    }
}
