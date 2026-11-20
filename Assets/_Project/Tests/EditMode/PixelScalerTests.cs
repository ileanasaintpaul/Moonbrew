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
        public void Compute_ExactMultiple_KeepsTheTargetSize()
        {
            var layout = PixelScaler.Compute(new Vector2Int(1920, 1080), RenderSize);

            Assert.AreEqual(4, layout.Scale);
            Assert.AreEqual(RenderSize, layout.RenderSize);
            Assert.AreEqual(new RectInt(0, 0, 1920, 1080), layout.Viewport);
        }

        [Test]
        public void Compute_NonMultiple_RenderSizeFollowsTheScreenAndFillsIt()
        {
            var layout = PixelScaler.Compute(new Vector2Int(2560, 1440), RenderSize);

            Assert.AreEqual(5, layout.Scale);
            Assert.AreEqual(new Vector2Int(512, 288), layout.RenderSize);
            Assert.AreEqual(new RectInt(0, 0, 2560, 1440), layout.Viewport);
        }

        [Test]
        public void Compute_DifferentAspectRatio_ShowsMoreInsteadOfBlackBands()
        {
            var layout = PixelScaler.Compute(new Vector2Int(1920, 1200), RenderSize);

            Assert.AreEqual(4, layout.Scale);
            Assert.AreEqual(new Vector2Int(480, 300), layout.RenderSize);
            Assert.AreEqual(new RectInt(0, 0, 1920, 1200), layout.Viewport);
        }

        // A window that is not a multiple of the render size: whole pixels, at most one screen pixel cropped.
        [Test]
        public void Compute_OddWindow_CropsLessThanOneRenderPixel()
        {
            var layout = PixelScaler.Compute(new Vector2Int(1277, 690), new Vector2Int(640, 360));

            Assert.AreEqual(2, layout.Scale);
            Assert.AreEqual(new Vector2Int(639, 345), layout.RenderSize);
            Assert.AreEqual(new RectInt(0, 0, 1278, 690), layout.Viewport);
        }

        [Test]
        public void Compute_ScreenEqualToRenderSize_ScaleIsOne()
        {
            var layout = PixelScaler.Compute(RenderSize, RenderSize);

            Assert.AreEqual(1, layout.Scale);
            Assert.AreEqual(new RectInt(0, 0, 480, 270), layout.Viewport);
        }

        [Test]
        public void Compute_ScreenSmallerThanRenderSize_KeepsScaleOneAndFills()
        {
            var layout = PixelScaler.Compute(new Vector2Int(320, 180), RenderSize);

            Assert.AreEqual(1, layout.Scale);
            Assert.AreEqual(new Vector2Int(320, 180), layout.RenderSize);
            Assert.AreEqual(new RectInt(0, 0, 320, 180), layout.Viewport);
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

        [Test]
        public void ScreenToPanel_TopLeftOfTheImage_IsTopLeftOfThePanel()
        {
            var viewport = new RectInt(10, 20, 1920, 1080);

            // The image's top edge is at 20 + 1080 = 1100 from the bottom, the very top of a 1100 px screen.
            var point = PixelScaler.ScreenToPanel(new Vector2(10f, 0f), 1100, viewport, new Vector2Int(640, 360));

            Assert.IsTrue(point.HasValue);
            Assert.AreEqual(0f, point.Value.x, 1e-3f);
            Assert.AreEqual(0f, point.Value.y, 1e-3f);
        }

        [Test]
        public void ScreenToPanel_CenterOfTheImage_IsCenterOfThePanel()
        {
            var viewport = new RectInt(0, 0, 1920, 1080);

            var point = PixelScaler.ScreenToPanel(new Vector2(960f, 540f), 1080, viewport, new Vector2Int(640, 360));

            Assert.AreEqual(new Vector2(320f, 180f), point.Value);
        }

        [Test]
        public void ScreenToPanel_BottomOfTheScreen_IsBottomOfThePanel()
        {
            var viewport = new RectInt(0, 0, 1920, 1080);

            var point = PixelScaler.ScreenToPanel(new Vector2(0f, 1080f), 1080, viewport, new Vector2Int(640, 360));

            Assert.AreEqual(360f, point.Value.y, 1e-3f);
        }

        [Test]
        public void ScreenToPanel_OnTheBlackBands_IsNull()
        {
            var viewport = new RectInt(100, 0, 1720, 1080);

            Assert.IsNull(PixelScaler.ScreenToPanel(new Vector2(50f, 540f), 1080, viewport, new Vector2Int(640, 360)));
        }

        [Test]
        public void ScreenToPanel_EmptyViewport_IsNull()
        {
            Assert.IsNull(PixelScaler.ScreenToPanel(Vector2.zero, 1080, new RectInt(0, 0, 0, 0), new Vector2Int(640, 360)));
        }

        [Test]
        public void ScreenToViewport_CenterOfTheImage_IsCenterOfTheVisibleTexture()
        {
            var viewport = new RectInt(0, 60, 1920, 1080);
            var visible = new Rect(0.1f, 0.1f, 0.8f, 0.8f);

            var point = PixelScaler.ScreenToViewport(new Vector2(960f, 600f), viewport, visible);

            Assert.AreEqual(0.5f, point.Value.x, 1e-5f);
            Assert.AreEqual(0.5f, point.Value.y, 1e-5f);
        }

        [Test]
        public void ScreenToViewport_ImageCorner_MapsToTheVisibleCorner()
        {
            var point = PixelScaler.ScreenToViewport(new Vector2(100f, 50f), new RectInt(100, 50, 640, 360), new Rect(0.25f, 0.5f, 0.5f, 0.25f));

            Assert.AreEqual(0.25f, point.Value.x, 1e-5f);
            Assert.AreEqual(0.5f, point.Value.y, 1e-5f);
        }

        [Test]
        public void ScreenToViewport_OnTheBlackBands_IsNull()
        {
            var viewport = new RectInt(0, 60, 1920, 1080);

            Assert.IsNull(PixelScaler.ScreenToViewport(new Vector2(960f, 30f), viewport, new Rect(0f, 0f, 1f, 1f)));
            Assert.IsNull(PixelScaler.ScreenToViewport(new Vector2(-1f, 600f), viewport, new Rect(0f, 0f, 1f, 1f)));
        }

        [Test]
        public void ScreenToViewport_EmptyViewport_IsNull()
        {
            Assert.IsNull(PixelScaler.ScreenToViewport(Vector2.zero, new RectInt(0, 0, 0, 0), new Rect(0f, 0f, 1f, 1f)));
        }
    }
}
