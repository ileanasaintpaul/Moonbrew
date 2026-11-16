using System;
using UnityEngine;

namespace Moonbrew.Player
{
    public static class PlayerMotor
    {
        // Turns stick or keyboard input into a ground velocity relative to the camera's yaw, so "up" always
        // walks away from the camera in the 3/4 view.
        public static Vector3 PlanarVelocity(Vector2 input, float cameraYaw, float speed)
        {
            if (speed < 0f || float.IsNaN(speed))
            {
                throw new ArgumentOutOfRangeException(nameof(speed), speed, "Speed must be zero or positive.");
            }

            var clamped = Vector2.ClampMagnitude(input, 1f);
            var direction = Quaternion.Euler(0f, cameraYaw, 0f) * new Vector3(clamped.x, 0f, clamped.y);
            return direction * speed;
        }

        // Rotates the current yaw towards the movement direction by at most maxDelta degrees.
        public static float Turn(float currentYaw, Vector3 velocity, float maxDelta)
        {
            if (maxDelta < 0f)
            {
                throw new ArgumentOutOfRangeException(nameof(maxDelta), maxDelta, "Max delta must be zero or positive.");
            }

            var planar = new Vector2(velocity.x, velocity.z);
            if (planar.sqrMagnitude < 1e-6f)
            {
                return currentYaw;
            }

            var target = Mathf.Atan2(planar.x, planar.y) * Mathf.Rad2Deg;
            return Mathf.MoveTowardsAngle(currentYaw, target, maxDelta);
        }
    }
}
