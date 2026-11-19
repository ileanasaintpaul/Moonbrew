using System;
using Moonbrew.Core;

namespace Moonbrew.Ui
{
    public static class ClockLabel
    {
        private const int MinutesPerHour = 60;
        private const int HoursPerDay = 24;

        // dayFormat receives {0} = day number, {1} = phase name and {2} = time, e.g. "Day {0} — {1} — {2}".
        public static string Format(string dayFormat, string[] phaseNames, int day, DayPhase phase, int minutes)
        {
            if (dayFormat == null)
            {
                throw new ArgumentNullException(nameof(dayFormat));
            }

            if (phaseNames == null)
            {
                throw new ArgumentNullException(nameof(phaseNames));
            }

            var index = (int)phase;
            if (index < 0 || index >= phaseNames.Length)
            {
                throw new ArgumentOutOfRangeException(nameof(phase), phase, "No display name for this phase.");
            }

            return string.Format(dayFormat, day, phaseNames[index], Time(minutes));
        }

        // Minutes since midnight as "HH:mm"; times past midnight wrap to the next morning (26:00 shows 02:00).
        public static string Time(int minutes)
        {
            if (minutes < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(minutes), minutes, "Time cannot be negative.");
            }

            var hours = minutes / MinutesPerHour % HoursPerDay;
            return $"{hours:00}:{minutes % MinutesPerHour:00}";
        }
    }
}
