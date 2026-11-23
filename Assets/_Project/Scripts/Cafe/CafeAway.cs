using Moonbrew.Core;
using UnityEngine;

namespace Moonbrew.Cafe
{
    // Keeps the café running from a scene where its room is not loaded.
    public class CafeAway : MonoBehaviour
    {
        [SerializeField] private CafeConfig config;
        [SerializeField] private CafeDay cafeDay;
        [SerializeField] private EventBusAsset eventBus;

        private CafeRun _run;
        private bool _subscribed;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(CafeConfig cafeConfig, CafeDay day, EventBusAsset bus)
        {
            config = cafeConfig;
            cafeDay = day;
            eventBus = bus;
        }

        private void Awake()
        {
            if (config == null || cafeDay == null || eventBus == null)
            {
                Debug.LogError($"{nameof(CafeAway)} on {name} needs the café config, the café day and the event bus.", this);
                enabled = false;
                return;
            }

            _run = cafeDay.Run(config, config.TableSeats);
        }

        private void OnEnable()
        {
            if (_subscribed || _run == null)
            {
                return;
            }

            eventBus.Subscribe<TimeChanged>(OnTimeChanged);
            eventBus.Subscribe<NightPassed>(OnNightPassed);
            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<NewGameStarted>(OnNewGame);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<TimeChanged>(OnTimeChanged);
            eventBus.Unsubscribe<NightPassed>(OnNightPassed);
            eventBus.Unsubscribe<GameLoaded>(OnLoaded);
            eventBus.Unsubscribe<NewGameStarted>(OnNewGame);
            _subscribed = false;
        }

        private void OnTimeChanged(TimeChanged message)
        {
            foreach (var group in _run.AdvanceAway(message.Minutes))
            {
                foreach (var member in group.Members)
                {
                    eventBus.Publish(new CustomerLeft(member.Id));
                }
            }
        }

        private void OnNightPassed(NightPassed message)
        {
            _run.Reset();
        }

        private void OnLoaded(GameLoaded message)
        {
            if (!message.SameDay)
            {
                _run.Reset();
            }
        }

        private void OnNewGame(NewGameStarted message)
        {
            _run.Reset();
        }
    }
}
