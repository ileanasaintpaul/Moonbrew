using System;
using UnityEngine;

namespace Moonbrew.Rendering
{
    public static class PixelScaler
    {
        public static PixelLayout Compute(Vector2Int screenSize, Vector2Int renderSize)
        {
            if (screenSize.x <= 0 || screenSize.y <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(screenSize), screenSize, "Screen size must be positive.");
            }

            if (renderSize.x <= 0 || renderSize.y <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(renderSize), renderSize, "Render size must be positive.");
            }

            var scale = Mathf.Max(1, Mathf.Min(screenSize.x / renderSize.x, screenSize.y / renderSize.y));
            var size = renderSize * scale;
            var origin = new Vector2Int((screenSize.x - size.x) / 2, (screenSize.y - size.y) / 2);

            return new PixelLayout(scale, new RectInt(origin, size));
        }
    }
}
