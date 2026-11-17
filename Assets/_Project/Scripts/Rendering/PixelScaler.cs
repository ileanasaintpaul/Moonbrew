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

        // Turns a screen point into the game camera's viewport point (0 to 1 over its render texture).
        // visibleUv is the part of the texture shown in the on-screen viewport (the texture has a margin).
        // Null when the point is on the black bands, outside the image.
        public static Vector2? ScreenToViewport(Vector2 screenPoint, RectInt viewport, Rect visibleUv)
        {
            if (viewport.width <= 0 || viewport.height <= 0)
            {
                return null;
            }

            var u = (screenPoint.x - viewport.x) / viewport.width;
            var v = (screenPoint.y - viewport.y) / viewport.height;
            if (u < 0f || u > 1f || v < 0f || v > 1f)
            {
                return null;
            }

            return new Vector2(visibleUv.x + u * visibleUv.width, visibleUv.y + v * visibleUv.height);
        }
    }
}
