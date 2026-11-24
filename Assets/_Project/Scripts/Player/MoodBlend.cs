using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Player
{
    // The numbers behind a portrait's face and pose: muscle offsets to aim for, and how a value moves towards its target.
    public static class MoodBlend
    {
        // One offset per humanoid muscle; unknown muscle names are skipped.
        public static float[] Targets(IEnumerable<PortraitMoods.MuscleOffset> offsets, IReadOnlyList<string> muscleNames)
        {
            var targets = new float[muscleNames?.Count ?? 0];
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

        // The mouth opens and closes while talking, from 0 to 1.
        public static float Mouth(float time, float speed)
        {
            return 0.5f + 0.5f * Mathf.Sin(time * speed * Mathf.PI * 2f);
        }

        // True during a blink, which comes back every interval.
        public static bool Blinking(float time, float interval, float blinkSeconds)
        {
            return interval > 0f && Mathf.Repeat(time, interval) < blinkSeconds;
        }
    }
}
