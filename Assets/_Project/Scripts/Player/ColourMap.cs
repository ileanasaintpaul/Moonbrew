using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Player
{
    // The character models are coloured by a small texture: each colour is a cell of CellSize by CellSize pixels,
    // which the models' UVs point at. These operations work on its pixels, row by row from the bottom.
    public static class ColourMap
    {
        public const int CellSize = 2;

        // Brightness limits of a dyed cell next to the outfit's main cell, so a dark trim or a light lining keeps its role.
        private const float MinShade = 0.25f;
        private const float MaxShade = 2f;

        public static Color CellColour(Color[] pixels, int width, Vector2Int cell)
        {
            return pixels[cell.y * CellSize * width + cell.x * CellSize];
        }

        public static void Paint(Color[] pixels, int width, Vector2Int cell, Color colour)
        {
            colour.a = 1f;
            for (var y = 0; y < CellSize; y++)
            {
                for (var x = 0; x < CellSize; x++)
                {
                    pixels[(cell.y * CellSize + y) * width + cell.x * CellSize + x] = colour;
                }
            }
        }

        // The cells come from another map of the same size, except those inside the skipped areas.
        public static void CopyCells(Color[] pixels, Color[] source, int width, IReadOnlyList<Vector2Int> cells, IReadOnlyList<RectInt> skipped)
        {
            if (source == null || source.Length != pixels.Length)
            {
                throw new ArgumentException("The source map must have the same size.", nameof(source));
            }

            foreach (var cell in cells)
            {
                if (!Inside(cell, skipped))
                {
                    Paint(pixels, width, cell, CellColour(source, width, cell));
                }
            }
        }

        // Dyes the cells inside the areas the swatch's colour. The first dyed cell (an outfit's most used one) takes the swatch;
        // the others keep their brightness next to it.
        public static void Dye(Color[] pixels, int width, IReadOnlyList<Vector2Int> cells, IReadOnlyList<RectInt> areas, Color swatch)
        {
            var main = -1f;
            foreach (var cell in cells)
            {
                if (!Inside(cell, areas))
                {
                    continue;
                }

                var brightness = CellColour(pixels, width, cell).grayscale;
                if (main < 0f)
                {
                    main = Mathf.Max(brightness, 0.01f);
                }

                var shade = Mathf.Clamp(brightness / main, MinShade, MaxShade);
                Paint(pixels, width, cell, new Color(Mathf.Clamp01(swatch.r * shade), Mathf.Clamp01(swatch.g * shade), Mathf.Clamp01(swatch.b * shade)));
            }
        }

        public static bool Inside(Vector2Int cell, IReadOnlyList<RectInt> areas)
        {
            foreach (var area in areas)
            {
                if (area.Contains(cell))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
