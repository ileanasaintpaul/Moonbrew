using System;
using UnityEngine;

namespace Moonbrew.Core
{
    // Holds the day in memory while the next scene loads: the same asset is referenced by every scene's session.
    [CreateAssetMenu(menuName = "Moonbrew/Core/Day Carry", fileName = "SO_DayCarry")]
    public class DayCarry : ScriptableObject
    {
        [NonSerialized] private CarriedDay _held;

        public void Hold(CarriedDay day)
        {
            _held = day ?? throw new ArgumentNullException(nameof(day));
        }

        // Hands the day over once, to the first session that asks.
        public bool TryTake(out CarriedDay day)
        {
            day = _held;
            _held = null;
            return day != null;
        }
    }

    public class CarriedDay
    {
        public CarriedDay(SaveData day, int minutes, string arrival, bool dayOver, bool slept = false)
        {
            Day = day ?? throw new ArgumentNullException(nameof(day));
            Minutes = minutes;
            Arrival = arrival;
            DayOver = dayOver;
            Slept = slept;
        }

        public SaveData Day { get; }

        // Time of day the player left at.
        public int Minutes { get; }

        public string Arrival { get; }

        // The day ended away from the wake scene: the night and the save happen there.
        public bool DayOver { get; }

        // The day was ended in bed, rather than by running out.
        public bool Slept { get; }
    }
}
