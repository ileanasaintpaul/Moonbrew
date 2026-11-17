using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Rendering
{
    // The colors a zone is allowed to show: every rendered pixel is pulled towards the nearest one.
    [CreateAssetMenu(menuName = "Moonbrew/Rendering/Zone Palette", fileName = "SO_Palette_")]
    public class ZonePalette : ScriptableObject
    {
        // Must match MAX_COLORS in the Moonbrew/PaletteFilter shader.
        public const int MaxColors = 32;

        [SerializeField] private string id;
        [SerializeField] private Color[] colors = new Color[0];
        [SerializeField, Range(0f, 1f)] private float strength = 1f;

        public string Id => id;

        public IReadOnlyList<Color> Colors => colors;

        public float Strength => strength;

        // Palette colors are authored in sRGB and the render texture holds linear values,
        // so the shader receives them converted. Returns how many colors were written.
        public static int ToLinear(IReadOnlyList<Color> source, Vector4[] buffer)
        {
            if (source == null || buffer == null)
            {
                return 0;
            }

            var count = Mathf.Min(source.Count, buffer.Length);
            for (var i = 0; i < count; i++)
            {
                buffer[i] = source[i].linear;
            }

            return count;
        }
    }
}
