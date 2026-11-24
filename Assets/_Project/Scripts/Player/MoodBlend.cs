using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Player
{
    // The numbers behind a portrait's face and pose: muscle offsets to aim for, and how a value moves towards its target.
    public static class MoodBlend
    {
        // One target per humanoid muscle, NaN for the muscles the pose leaves to the animation; unknown names are skipped.
        public static float[] Targets(IEnumerable<PortraitMoods.MuscleOffset> offsets, IReadOnlyList<string> muscleNames)
        {
            var targets = new float[muscleNames?.Count ?? 0];
            for (var i = 0; i < targets.Length; i++)
            {
                targets[i] = float.NaN;
            }

            foreach (var offset in offsets ?? Array.Empty<PortraitMoods.MuscleOffset>())
            {
                for (var i = 0; i < targets.Length; i++)
                {
                    if (muscleNames[i] == offset.muscle)
                    {
                        targets[i] = Mathf.Clamp(offset.value, -1f, 1f);
                    }
                }
            }

            return targets;
        }

        public static float Toward(float current, float target, float speed, float seconds)
        {
            return Mathf.MoveTowards(current, target, Mathf.Max(0f, speed) * Mathf.Max(0f, seconds) * Mathf.Max(1f, Mathf.Abs(target - current)));
        }

        // True during a blink, which comes back every interval.
        public static bool Blinking(float time, float interval, float blinkSeconds)
        {
            return interval > 0f && Mathf.Repeat(time, interval) < blinkSeconds;
        }
    }
}
