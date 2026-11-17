using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class ZonePaletteTests
    {
        [Test]
        public void ToLinear_SrgbColors_WritesLinearValues()
        {
            var colors = new[] { new Color(0.5f, 1f, 0f) };
            var buffer = new Vector4[ZonePalette.MaxColors];

            var count = ZonePalette.ToLinear(colors, buffer);

            Assert.AreEqual(1, count);
            Assert.AreEqual(Mathf.GammaToLinearSpace(0.5f), buffer[0].x, 0.0001f);
            Assert.AreEqual(1f, buffer[0].y, 0.0001f);
            Assert.AreEqual(0f, buffer[0].z, 0.0001f);
        }

        [Test]
        public void ToLinear_NullColors_ReturnsZero()
        {
            Assert.AreEqual(0, ZonePalette.ToLinear(null, new Vector4[ZonePalette.MaxColors]));
        }

        [Test]
        public void ToLinear_NullBuffer_ReturnsZero()
        {
            Assert.AreEqual(0, ZonePalette.ToLinear(new[] { Color.white }, null));
        }

        [Test]
        public void ToLinear_EmptyColors_ReturnsZero()
        {
            Assert.AreEqual(0, ZonePalette.ToLinear(new Color[0], new Vector4[ZonePalette.MaxColors]));
        }

        [Test]
        public void ToLinear_MoreColorsThanBuffer_StopsAtBufferSize()
        {
            var colors = new Color[ZonePalette.MaxColors + 4];
            var buffer = new Vector4[ZonePalette.MaxColors];

            Assert.AreEqual(ZonePalette.MaxColors, ZonePalette.ToLinear(colors, buffer));
        }
    }
}
