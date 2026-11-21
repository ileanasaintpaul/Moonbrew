using System;
using Moonbrew.Player;
using NUnit.Framework;
using UnityEngine;

namespace Moonbrew.Tests.EditMode
{
    public class ColourMapTests
    {
        private const int Width = 8;

        private static Color[] Map(Color colour)
        {
            var pixels = new Color[Width * Width];
            for (var i = 0; i < pixels.Length; i++)
            {
                pixels[i] = colour;
            }

            return pixels;
        }

        [Test]
        public void Paint_Cell_FillsItsFourPixelsOnly()
        {
            var pixels = Map(Color.black);

            ColourMap.Paint(pixels, Width, new Vector2Int(1, 2), Color.red);

            Assert.AreEqual(Color.red, pixels[4 * Width + 2]);
            Assert.AreEqual(Color.red, pixels[5 * Width + 3]);
            Assert.AreEqual(Color.black, pixels[4 * Width + 4]);
            Assert.AreEqual(Color.red, ColourMap.CellColour(pixels, Width, new Vector2Int(1, 2)));
        }

        [Test]
        public void Paint_TransparentColour_IsOpaque()
        {
            var pixels = Map(Color.black);

            ColourMap.Paint(pixels, Width, Vector2Int.zero, new Color(0.2f, 0.4f, 0.6f, 0f));

            Assert.AreEqual(1f, pixels[0].a);
        }

        [Test]
        public void CopyCells_SkippedArea_KeepsItsColour()
        {
            var pixels = Map(Color.black);
            var source = Map(Color.green);

            ColourMap.CopyCells(pixels, source, Width, new[] { new Vector2Int(0, 0), new Vector2Int(3, 3) }, new[] { new RectInt(3, 3, 1, 1) });

            Assert.AreEqual(Color.green, ColourMap.CellColour(pixels, Width, new Vector2Int(0, 0)));
            Assert.AreEqual(Color.black, ColourMap.CellColour(pixels, Width, new Vector2Int(3, 3)));
        }

        [Test]
        public void CopyCells_SourceOfAnotherSize_Throws()
        {
            Assert.Throws<ArgumentException>(() => ColourMap.CopyCells(Map(Color.black), new Color[4], Width, new Vector2Int[0], new RectInt[0]));
            Assert.Throws<ArgumentException>(() => ColourMap.CopyCells(Map(Color.black), null, Width, new Vector2Int[0], new RectInt[0]));
        }

        [Test]
        public void Dye_MainCellTakesTheSwatchAndOthersKeepTheirShade()
        {
            var pixels = Map(Color.black);
            ColourMap.Paint(pixels, Width, new Vector2Int(0, 0), new Color(0.5f, 0.5f, 0.5f));
            ColourMap.Paint(pixels, Width, new Vector2Int(1, 0), new Color(0.25f, 0.25f, 0.25f));
            var swatch = new Color(0.8f, 0.2f, 0.4f);

            ColourMap.Dye(pixels, Width, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }, new[] { new RectInt(0, 0, 4, 4) }, swatch);

            var main = ColourMap.CellColour(pixels, Width, new Vector2Int(0, 0));
            var darker = ColourMap.CellColour(pixels, Width, new Vector2Int(1, 0));
            Assert.AreEqual(swatch.r, main.r, 0.001f);
            Assert.AreEqual(swatch.r * 0.5f, darker.r, 0.001f);
        }

        [Test]
        public void Dye_CellOutsideTheAreas_IsLeftAlone()
        {
            var pixels = Map(Color.white);

            ColourMap.Dye(pixels, Width, new[] { new Vector2Int(3, 3) }, new[] { new RectInt(0, 0, 2, 2) }, Color.blue);

            Assert.AreEqual(Color.white, ColourMap.CellColour(pixels, Width, new Vector2Int(3, 3)));
        }

        [Test]
        public void Dye_NoCells_ChangesNothing()
        {
            var pixels = Map(Color.white);

            ColourMap.Dye(pixels, Width, new Vector2Int[0], new[] { new RectInt(0, 0, 4, 4) }, Color.blue);

            Assert.AreEqual(Color.white, pixels[0]);
        }

        [Test]
        public void Dye_BlackMainCell_DoesNotDivideByZero()
        {
            var pixels = Map(Color.black);

            ColourMap.Dye(pixels, Width, new[] { new Vector2Int(0, 0), new Vector2Int(1, 0) }, new[] { new RectInt(0, 0, 4, 4) }, Color.red);

            Assert.IsFalse(float.IsNaN(pixels[0].r));
        }
    }
}
