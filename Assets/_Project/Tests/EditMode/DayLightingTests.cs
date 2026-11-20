using System;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class DayLightingTests
    {
        private static LightKey Key(float hour, float intensity, Color color)
        {
            return new LightKey
            {
                Hour = hour,
                SunColor = color,
                SunIntensity = intensity,
                AmbientSky = color,
                AmbientEquator = color,
                AmbientGround = color,
                WindowIntensity = intensity * 2f,
            };
        }

        private static readonly LightKey[] Keys =
        {
            Key(6f, 0.5f, Color.red),
            Key(12f, 1f, Color.white),
            Key(22f, 0f, Color.blue),
        };

        [Test]
        public void Evaluate_BetweenKeys_Interpolates()
        {
            var light = DayLighting.Evaluate(Keys, 9 * 60);

            Assert.AreEqual(0.75f, light.SunIntensity, 1e-4f);
            Assert.AreEqual(1.5f, light.WindowIntensity, 1e-4f);
            Assert.AreEqual(Color.Lerp(Color.red, Color.white, 0.5f), light.SunColor);
            Assert.AreEqual(Color.Lerp(Color.red, Color.white, 0.5f), light.AmbientGround);
        }

        [Test]
        public void Evaluate_OnAKey_ReturnsThatKey()
        {
            var light = DayLighting.Evaluate(Keys, 12 * 60);

            Assert.AreEqual(1f, light.SunIntensity, 1e-4f);
            Assert.AreEqual(Color.white, light.SunColor);
        }

        [Test]
        public void Evaluate_BeforeFirstKey_HoldsFirstKey()
        {
            var light = DayLighting.Evaluate(Keys, 0);

            Assert.AreEqual(0.5f, light.SunIntensity, 1e-4f);
            Assert.AreEqual(Color.red, light.SunColor);
        }

        [Test]
        public void Evaluate_PastMidnight_HoldsLastKey()
        {
            var light = DayLighting.Evaluate(Keys, 26 * 60);

            Assert.AreEqual(0f, light.SunIntensity, 1e-4f);
            Assert.AreEqual(Color.blue, light.SunColor);
        }

        [Test]
        public void Evaluate_SingleKey_AlwaysReturnsIt()
        {
            var single = new[] { Key(10f, 0.8f, Color.green) };

            Assert.AreEqual(0.8f, DayLighting.Evaluate(single, 0).SunIntensity, 1e-4f);
            Assert.AreEqual(0.8f, DayLighting.Evaluate(single, 20 * 60).SunIntensity, 1e-4f);
        }

        [Test]
        public void Evaluate_NullKeys_Throws()
        {
            Assert.Throws<ArgumentNullException>(() => DayLighting.Evaluate(null, 0));
        }

        [Test]
        public void Evaluate_NoKeys_Throws()
        {
            Assert.Throws<ArgumentException>(() => DayLighting.Evaluate(new LightKey[0], 0));
        }

        [Test]
        public void Evaluate_NegativeTime_Throws()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => DayLighting.Evaluate(Keys, -1));
        }
    }
}
