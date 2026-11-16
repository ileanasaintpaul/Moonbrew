using UnityEngine;

namespace Moonbrew.Rendering
{
    public readonly struct PixelLayout
    {
        public PixelLayout(int scale, RectInt viewport)
        {
            Scale = scale;
            Viewport = viewport;
        }

        public int Scale { get; }

        public RectInt Viewport { get; }
    }
}
