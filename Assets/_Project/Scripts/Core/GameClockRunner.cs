using UnityEngine;

namespace Moonbrew.Core
{
    // Drives the game clock with real time. Every other system reads time from Clock, never from Time.
    public class GameClockRunner : MonoBehaviour
    {
        [SerializeField] private ClockConfig config;

        private GameClock _clock;

        // Created on first access so listeners can subscribe in OnEnable, whatever the script order.
        public GameClock Clock => _clock ??= Create();

        public void Init(ClockConfig clockConfig)
        {
            config = clockConfig;
            _clock = null;
        }

        private void OnEnable()
        {
            Clock.DayStarted += LogDayStarted;
            Clock.DayEnded += LogDayEnded;
            Clock.PhaseChanged += LogPhaseChanged;
        }

        private void OnDisable()
        {
            Clock.DayStarted -= LogDayStarted;
            Clock.DayEnded -= LogDayEnded;
            Clock.PhaseChanged -= LogPhaseChanged;
        }

        private void Start()
        {
            Clock.Start();
        }

        // The clock is the one place where real time becomes game time.
        private void Update()
        {
            Clock.Tick(Time.deltaTime);
        }

        private GameClock Create()
        {
            return new GameClock(config.PhaseDurations(), config.PhaseStartMinutes(), config.MinuteStep, config.FirstDay);
        }

        private static void LogDayStarted(int day)
        {
            Debug.Log($"[Clock] Day {day} started");
        }

        private static void LogDayEnded(int day)
        {
            Debug.Log($"[Clock] Day {day} ended");
        }

        private static void LogPhaseChanged(DayPhase phase)
        {
            Debug.Log($"[Clock] Phase changed to {phase}");
        }
    }
}
