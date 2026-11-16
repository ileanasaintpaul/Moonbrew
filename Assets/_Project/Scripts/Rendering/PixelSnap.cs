using System;
using UnityEngine;

namespace Moonbrew.Rendering
{
    public readonly struct PixelSnapResult
    {
        public PixelSnapResult(Vector3 position, Vector2 remainder)
        {
            Position = position;
            Remainder = remainder;
        }

        public Vector3 Position { get; }

        // How far, in texels along the camera's right and up axes, the original position is ahead of the snapped one.
        public Vector2 Remainder { get; }
    }

    public static class PixelSnap
    {
        public static PixelSnapResult Snap(Vector3 position, Quaternion rotation, float texelSize)
        {
            if (texelSize <= 0f || float.IsNaN(texelSize) || float.IsInfinity(texelSize))
            {
                throw new ArgumentOutOfRangeException(nameof(texelSize), texelSize, "Texel size must be a positive finite number.");
            }

            var local = Quaternion.Inverse(rotation) * position;
            var snappedX = Mathf.Round(local.x / texelSize) * texelSize;
            var snappedY = Mathf.Round(local.y / texelSize) * texelSize;
            var snapped = rotation * new Vector3(snappedX, snappedY, local.z);
            var remainder = new Vector2((local.x - snappedX) / texelSize, (local.y - snappedY) / texelSize);

            return new PixelSnapResult(snapped, remainder);
        }

        public static float TexelSize(float orthographicSize, int renderHeight)
        {
            if (orthographicSize <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(orthographicSize), orthographicSize, "Orthographic size must be positive.");
            }

            if (renderHeight <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(renderHeight), renderHeight, "Render height must be positive.");
            }

            return orthographicSize * 2f / renderHeight;
        }
    }
}
