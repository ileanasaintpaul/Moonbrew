using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Rendering
{
    public static class DayLighting
    {
        private const float MinutesPerHour = 60f;

        // The light at this time of day, between the two surrounding keys. Keys must be in ascending hour order;
        // before the first key or after the last one, the nearest key holds.
        public static LightKey Evaluate(IReadOnlyList<LightKey> keys, int minutes)
        {
            if (keys == null)
            {
                throw new ArgumentNullException(nameof(keys));
            }

            if (keys.Count == 0)
            {
                throw new ArgumentException("Day lighting needs at least one key.", nameof(keys));
            }

            if (minutes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minutes), minutes, "Time cannot be negative.");
            }

            var hour = minutes / MinutesPerHour;
            if (hour <= keys[0].Hour)
            {
                return keys[0];
            }

            for (var i = 1; i < keys.Count; i++)
            {
                if (hour <= keys[i].Hour)
                {
                    var from = keys[i - 1];
                    var to = keys[i];
                    return Lerp(from, to, Mathf.InverseLerp(from.Hour, to.Hour, hour));
                }
            }

            return keys[keys.Count - 1];
        }

        private static LightKey Lerp(LightKey from, LightKey to, float t)
        {
            return new LightKey
            {
                Hour = Mathf.Lerp(from.Hour, to.Hour, t),
                SunColor = Color.Lerp(from.SunColor, to.SunColor, t),
                SunIntensity = Mathf.Lerp(from.SunIntensity, to.SunIntensity, t),
                AmbientSky = Color.Lerp(from.AmbientSky, to.AmbientSky, t),
                AmbientEquator = Color.Lerp(from.AmbientEquator, to.AmbientEquator, t),
                AmbientGround = Color.Lerp(from.AmbientGround, to.AmbientGround, t),
                WindowIntensity = Mathf.Lerp(from.WindowIntensity, to.WindowIntensity, t),
            };
        }
    }
}
