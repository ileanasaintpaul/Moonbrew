using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Rendering
{
    // How a zone is lit at given hours of the day. The light between two keys is interpolated.
    [CreateAssetMenu(menuName = "Moonbrew/Rendering/Day Lighting", fileName = "SO_DayLighting_")]
    public class DayLightingConfig : ScriptableObject
    {
        [SerializeField] private string id;
        [Tooltip("In ascending hour order. Hours past 24 belong to the next morning (26 = 2:00).")]
        [SerializeField] private LightKey[] keys = new LightKey[0];

        public string Id => id;

        public IReadOnlyList<LightKey> Keys => keys;

        // For configs built from code.
        public void Init(string configId, LightKey[] lightKeys)
        {
            id = configId;
            keys = lightKeys ?? new LightKey[0];
        }
    }

    [Serializable]
    public struct LightKey
    {
        [Min(0f)] public float Hour;
        public Color SunColor;
        [Min(0f)] public float SunIntensity;
        public Color AmbientSky;
        public Color AmbientEquator;
        public Color AmbientGround;
        [Tooltip("Daylight coming in through the windows. 0 at night.")]
        [Min(0f)] public float WindowIntensity;
    }
}
