using System;

namespace Moonbrew.Core
{
    // The single game clock: phases follow each other, and the end of the night ends the day and starts the next.
    // Each phase also covers a range of in-game time (minutes since midnight) shown to the player.
    public class GameClock
    {
        private static readonly int PhaseCount = Enum.GetValues(typeof(DayPhase)).Length;

        private readonly float[] _phaseSeconds;
        private readonly int[] _phaseStartMinutes;
        private readonly int _minuteStep;
        private float _elapsedInPhase;
        private int _announcedMinutes = -1;
        private bool _started;

        // phaseStartMinutes holds the in-game minute each phase starts at, plus the minute the day ends.
        public GameClock(float[] phaseSeconds, int[] phaseStartMinutes, int minuteStep, int firstDay)
        {
            if (phaseSeconds == null)
            {
                throw new ArgumentNullException(nameof(phaseSeconds));
            }

            if (phaseSeconds.Length != PhaseCount)
            {
                throw new ArgumentException($"Expected {PhaseCount} phase durations, got {phaseSeconds.Length}.", nameof(phaseSeconds));
            }

            foreach (var seconds in phaseSeconds)
            {
                if (seconds <= 0f || float.IsNaN(seconds) || float.IsInfinity(seconds))
                {
                    throw new ArgumentOutOfRangeException(nameof(phaseSeconds), seconds, "Phase durations must be positive and finite.");
                }
            }

            if (phaseStartMinutes == null)
            {
                throw new ArgumentNullException(nameof(phaseStartMinutes));
            }

            if (phaseStartMinutes.Length != PhaseCount + 1)
            {
                throw new ArgumentException($"Expected {PhaseCount + 1} phase boundaries, got {phaseStartMinutes.Length}.", nameof(phaseStartMinutes));
            }

            for (var i = 1; i < phaseStartMinutes.Length; i++)
            {
                if (phaseStartMinutes[i] <= phaseStartMinutes[i - 1])
                {
                    throw new ArgumentException("Phase boundaries must strictly increase.", nameof(phaseStartMinutes));
                }
            }

            if (phaseStartMinutes[0] < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(phaseStartMinutes), phaseStartMinutes[0], "The day cannot start before midnight.");
            }

            if (minuteStep < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(minuteStep), minuteStep, "The minute step must be at least 1.");
            }

            if (firstDay < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(firstDay), firstDay, "Days start at 1.");
            }

            _phaseSeconds = (float[])phaseSeconds.Clone();
            _phaseStartMinutes = (int[])phaseStartMinutes.Clone();
            _minuteStep = minuteStep;
            Day = firstDay;
            Phase = DayPhase.Morning;
        }

        public event Action<int> DayStarted;

        public event Action<int> DayEnded;

        public event Action<DayPhase> PhaseChanged;

        // Raised when the displayed time moves to the next step, with the minutes since midnight.
        public event Action<int> TimeChanged;

        public int Day { get; private set; }

        public DayPhase Phase { get; private set; }

        // 0 at the start of the current phase, 1 at its end.
        public float PhaseProgress => _elapsedInPhase / _phaseSeconds[(int)Phase];

        // In-game minutes since midnight, rounded down to the minute step.
        public int Minutes
        {
            get
            {
                var start = _phaseStartMinutes[(int)Phase];
                var end = _phaseStartMinutes[(int)Phase + 1];
                var minutes = start + (int)((end - start) * PhaseProgress);
                return minutes - (minutes - start) % _minuteStep;
            }
        }

        // Announces the first day, phase and time so listeners can initialize. Ticking starts the clock if needed.
        public void Start()
        {
            if (_started)
            {
                return;
            }

            _started = true;
            DayStarted?.Invoke(Day);
            PhaseChanged?.Invoke(Phase);
            AnnounceTime();
        }

        public void Tick(float deltaSeconds)
        {
            if (deltaSeconds < 0f || float.IsNaN(deltaSeconds))
            {
                throw new ArgumentOutOfRangeException(nameof(deltaSeconds), deltaSeconds, "Time cannot go backwards.");
            }

            Start();
            _elapsedInPhase += deltaSeconds;

            // A long frame can cross several phases: each one is still announced in order.
            while (_elapsedInPhase >= _phaseSeconds[(int)Phase])
            {
                _elapsedInPhase -= _phaseSeconds[(int)Phase];
                if (Phase == DayPhase.Night)
                {
                    StartNextDay(false);
                }
                else
                {
                    Phase++;
                    PhaseChanged?.Invoke(Phase);
                    AnnounceTime();
                }
            }

            AnnounceTime();
        }

        // Ends the current day and starts the next one in the morning, for example when the player goes to sleep.
        public void StartNextDay()
        {
            Start();
            StartNextDay(true);
        }

        private void StartNextDay(bool resetPhaseTime)
        {
            if (resetPhaseTime)
            {
                _elapsedInPhase = 0f;
            }

            DayEnded?.Invoke(Day);
            Day++;
            Phase = DayPhase.Morning;
            DayStarted?.Invoke(Day);
            PhaseChanged?.Invoke(Phase);
            AnnounceTime();
        }

        private void AnnounceTime()
        {
            var minutes = Minutes;
            if (minutes != _announcedMinutes)
            {
                _announcedMinutes = minutes;
                TimeChanged?.Invoke(minutes);
            }
        }
    }
}
