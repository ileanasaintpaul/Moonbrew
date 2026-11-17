using System;
using UnityEngine;

namespace Moonbrew.Rendering
{
    public static class FreeCameraMotion
    {
        // Ground-plane offset for one frame of panning, relative to the camera's yaw so "up" pans away from it.
        // The speed is given at the reference zoom and scales with the current zoom.
        public static Vector3 Pan(Vector2 input, float cameraYaw, float speed, float zoom, float referenceZoom, float deltaTime)
        {
            if (speed < 0f || deltaTime < 0f || zoom <= 0f || referenceZoom <= 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(speed), "Speed, time and zoom values must be positive.");
            }

            var clamped = Vector2.ClampMagnitude(input, 1f);
            var direction = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(clamped.x, 0f, clamped.y);
            return direction * (speed * zoom / referenceZoom * deltaTime);
        }

        // New orthographic size after a zoom input: positive input zooms in.
        public static float Zoom(float current, float input, float step, float min, float max)
        {
            if (min > max)
            {
                throw new ArgumentException("The minimum zoom cannot be above the maximum.", nameof(min));
            }

            if (Mathf.Approximately(input, 0f))
            {
                return Mathf.Clamp(current, min, max);
            }

            var next = input > 0f ? current - step : current + step;
            return Mathf.Clamp(next, min, max);
        }
    }
}
