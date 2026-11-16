using System;
using Moonbrew.Rendering;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class PixelScalerTests
    {
        private static readonly Vector2Int RenderSize = new Vector2Int(480, 270);

        [Test]
        public void Compute_ExactMultiple_FillsScreen()
        {
            var layout = PixelScaler.Compute(new Vector2Int(1920, 1080), RenderSize);

            Assert.AreEqual(4, layout.Scale);
            Assert.AreEqual(new RectInt(0, 0, 1920, 1080), layout.Viewport);
        }

        [Test]
        public void Compute_NonMultiple_UsesLargestIntegerScaleAndCenters()
        {
            var layout = PixelScaler.Compute(new Vector2Int(2560, 1440), RenderSize);

            Assert.AreEqual(5, layout.Scale);
            Assert.AreEqual(new RectInt(80, 45, 2400, 1350), layout.Viewport);
        }

        [Test]
        public void Compute_DifferentAspectRatio_ScaleLimitedBySmallestAxis()
        {
            var layout = PixelScaler.Compute(new Vector2Int(1920, 1200), RenderSize);

            Assert.AreEqual(4, layout.Scale);
            Assert.AreEqual(new RectInt(0, 60, 1920, 1080), layout.Viewport);
        }

        [Test]
        public void Compute_ScreenEqualToRenderSize_ScaleIsOne()
        {
            var layout = PixelScaler.Compute(RenderSize, RenderSize);

            Assert.AreEqual(1, layout.Scale);
            Assert.AreEqual(new RectInt(0, 0, 480, 270), layout.Viewport);
        }

        [Test]
        public void Compute_ScreenSmallerThanRenderSize_KeepsScaleOneAndCrops()
        {
            var layout = PixelScaler.Compute(new Vector2Int(320, 180), RenderSize);

            Assert.AreEqual(1, layout.Scale);
            Assert.AreEqual(new RectInt(-80, -45, 480, 270), layout.Viewport);
        }

        [TestCase(0, 1080)]
        [TestCase(1920, 0)]
        [TestCase(-1920, 1080)]
        public void Compute_NonPositiveScreenSize_Throws(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PixelScaler.Compute(new Vector2Int(width, height), RenderSize));
        }

        [TestCase(0, 270)]
        [TestCase(480, -1)]
        public void Compute_NonPositiveRenderSize_Throws(int width, int height)
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => PixelScaler.Compute(new Vector2Int(1920, 1080), new Vector2Int(width, height)));
        }
    }
}
