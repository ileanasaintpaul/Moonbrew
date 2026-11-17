using UnityEngine;

namespace Moonbrew.Core
{
    [CreateAssetMenu(menuName = "Moonbrew/Core/Clock Config", fileName = "SO_ClockConfig")]
    public class ClockConfig : ScriptableObject
    {
        private const int MinutesPerHour = 60;

        // Real-time seconds per phase.
        [SerializeField, Min(1f)] private float morningSeconds = 240f;
        [SerializeField, Min(1f)] private float daySeconds = 300f;
        [SerializeField, Min(1f)] private float eveningSeconds = 240f;
        [SerializeField, Min(1f)] private float nightSeconds = 120f;
        [SerializeField, Min(1)] private int firstDay = 1;

        // In-game hour at which each phase starts. Hours past 24 belong to the next morning (26 = 2:00).
        [SerializeField, Min(0)] private int morningStartHour = 6;
        [SerializeField, Min(0)] private int dayStartHour = 12;
        [SerializeField, Min(0)] private int eveningStartHour = 18;
        [SerializeField, Min(0)] private int nightStartHour = 22;
        [SerializeField, Min(0)] private int dayEndHour = 26;

        // The displayed time moves by this many in-game minutes at a time.
        [SerializeField, Min(1)] private int minuteStep = 10;

        public int FirstDay => firstDay;

        public int MinuteStep => minuteStep;

        // In DayPhase order.
        public float[] PhaseDurations()
        {
            return new[] { morningSeconds, daySeconds, eveningSeconds, nightSeconds };
        }

        // In-game minute at which each phase starts, followed by the minute the day ends.
        public int[] PhaseStartMinutes()
        {
            return new[]
            {
                morningStartHour * MinutesPerHour,
                dayStartHour * MinutesPerHour,
                eveningStartHour * MinutesPerHour,
                nightStartHour * MinutesPerHour,
                dayEndHour * MinutesPerHour,
            };
        }
    }
}
