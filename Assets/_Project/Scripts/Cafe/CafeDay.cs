using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Cafe
{
    // Keeps the café's day in memory across scenes. Never saved: the café empties at night.
    [CreateAssetMenu(menuName = "Moonbrew/Cafe/Cafe Day", fileName = "SO_CafeDay")]
    public class CafeDay : ScriptableObject
    {
        [NonSerialized] private CafeRun _run;

        public CafeRun Run(CafeConfig config, IReadOnlyList<int> tableSeats)
        {
            return _run ??= new CafeRun(config, tableSeats, new System.Random());
        }
    }
}
