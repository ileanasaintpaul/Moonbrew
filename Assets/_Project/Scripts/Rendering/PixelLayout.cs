using UnityEngine;

namespace Moonbrew.Rendering
{
    public readonly struct PixelLayout
    {
        public PixelLayout(int scale, RectInt viewport, Vector2Int renderSize)
        {
            Scale = scale;
            Viewport = viewport;
            RenderSize = renderSize;
        }

        public int Scale { get; }

        public RectInt Viewport { get; }

        // The low-resolution image size for this screen: about the target size, shaped like the screen.
        public Vector2Int RenderSize { get; }
    }
}
