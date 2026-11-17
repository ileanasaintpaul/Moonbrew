using UnityEngine;

namespace Moonbrew.Core
{
    // Drives the game clock with real time and publishes its changes on the event bus.
    // Every other system reads time from the bus or from Clock, never from Time.
    public class GameClockRunner : MonoBehaviour
    {
        [SerializeField] private ClockConfig config;
        [SerializeField] private EventBusAsset eventBus;
        [Tooltip("Off when a GameSession decides the starting day, after loading the save and showing the recap.")]
        [SerializeField] private bool startOnPlay = true;

        private GameClock _clock;

        // Created on first access so listeners can subscribe in OnEnable, whatever the script order.
        public GameClock Clock => _clock ??= Create();

        public ClockConfig Config => config;

        public void Init(ClockConfig clockConfig, EventBusAsset bus, bool startOnPlay = true)
        {
            config = clockConfig;
            eventBus = bus;
            this.startOnPlay = startOnPlay;
            _clock = null;
        }

        private void OnEnable()
        {
            Clock.DayStarted += OnDayStarted;
            Clock.DayEnded += OnDayEnded;
            Clock.PhaseChanged += OnPhaseChanged;
            Clock.TimeChanged += OnTimeChanged;
        }

        private void OnDisable()
        {
            Clock.DayStarted -= OnDayStarted;
            Clock.DayEnded -= OnDayEnded;
            Clock.PhaseChanged -= OnPhaseChanged;
            Clock.TimeChanged -= OnTimeChanged;
        }

        // After every OnEnable, so listeners on the bus receive the first day, phase and time.
        private void Start()
        {
            if (startOnPlay)
            {
                Clock.Start();
            }
        }

        // Starts the clock on a given day; time does not run before that.
        public void Begin(int day)
        {
            Clock.Start(day);
        }

        // The clock is the one place where real time becomes game time.
        private void Update()
        {
            if (Clock.IsStarted)
            {
                Clock.Tick(Time.deltaTime);
            }
        }

        private GameClock Create()
        {
            return new GameClock(config.PhaseDurations(), config.PhaseStartMinutes(), config.MinuteStep, config.FirstDay);
        }

        private void OnDayStarted(int day)
        {
            Debug.Log($"[Clock] Day {day} started");
            Publish(new DayStarted(day));
        }

        private void OnDayEnded(int day)
        {
            Debug.Log($"[Clock] Day {day} ended");
            Publish(new DayEnded(day));
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            Debug.Log($"[Clock] Phase changed to {phase}");
            Publish(new PhaseChanged(Clock.Day, phase));
        }

        private void OnTimeChanged(int minutes)
        {
            Publish(new TimeChanged(minutes));
        }

        private void Publish<TEvent>(TEvent message)
        {
            if (eventBus != null)
            {
                eventBus.Publish(message);
            }
        }
    }
}
