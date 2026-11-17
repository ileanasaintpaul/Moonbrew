using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Moonbrew.Core
{
    // Runs the day cycle around the save: load the last game, show the recap, start the clock;
    // at the end of the day (sleep or 2 a.m.), save and wake up in the morning scene.
    // The game is never saved in the middle of a day.
    public class GameSession : MonoBehaviour
    {
        [SerializeField] private SessionConfig config;
        [SerializeField] private GameClockRunner clockRunner;
        [SerializeField] private EventBusAsset eventBus;

        private SaveStore _store;
        private SaveData _current;
        private bool _subscribed;
        private bool _dayEnding;
        private bool _reloadWakeScene = true;

        public SaveData Current => _current;

        public bool WaitingForRecap { get; private set; }

        // For objects built from code: call on an inactive GameObject, before it is activated.
        // Tests save to a temporary folder and stay in their scene at the end of the day.
        public void Init(SessionConfig sessionConfig, GameClockRunner runner, EventBusAsset bus, string savesFolder, bool reloadWakeScene)
        {
            config = sessionConfig;
            clockRunner = runner;
            eventBus = bus;
            _store = new SaveStore(savesFolder);
            _reloadWakeScene = reloadWakeScene;
        }

        // Starts a new game in its own save file, next to the others, then reloads the morning scene.
        public void StartNewGame()
        {
            var data = SaveData.CreateNew(SaveStore.NewSlotId(), clockRunner.Config.FirstDay);
            Write(data);
            Debug.Log($"[Save] New game {data.slotId}");
            if (_reloadWakeScene)
            {
                SceneManager.LoadScene(config.WakeScene);
            }
        }

        private void Awake()
        {
            if (config == null || clockRunner == null || eventBus == null)
            {
                Debug.LogError($"{nameof(GameSession)} on {name} needs its config, clock runner and event bus.", this);
                enabled = false;
                return;
            }

            _store ??= new SaveStore(Path.Combine(Application.persistentDataPath, config.SavesFolder));
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<DayEnded>(OnDayEnded);
            eventBus.Subscribe<DayRecapClosed>(OnRecapClosed);
            eventBus.Subscribe<Interacted>(OnInteracted);
            eventBus.Subscribe<SleepConfirmed>(OnSleepConfirmed);
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<DayEnded>(OnDayEnded);
            eventBus.Unsubscribe<DayRecapClosed>(OnRecapClosed);
            eventBus.Unsubscribe<Interacted>(OnInteracted);
            eventBus.Unsubscribe<SleepConfirmed>(OnSleepConfirmed);
            _subscribed = false;
        }

        // After every OnEnable, so all modules hear the load.
        private void Start()
        {
            _current = _store.MostRecent() ?? SaveData.CreateNew(SaveStore.NewSlotId(), clockRunner.Config.FirstDay);
            if (_current.isNew)
            {
                eventBus.Publish(new NewGameStarted());
            }
            else
            {
                eventBus.Publish(new GameLoaded(_current));
                Debug.Log($"[Save] Loaded game {_current.slotId}, day {_current.day}");
            }

            if (_current.HasRecap)
            {
                WaitingForRecap = true;
                eventBus.Publish(new DayRecapOpened(_current.recap));
            }
            else
            {
                clockRunner.Begin(_current.day);
            }
        }

        private void OnRecapClosed(DayRecapClosed message)
        {
            if (!WaitingForRecap)
            {
                return;
            }

            WaitingForRecap = false;
            clockRunner.Begin(_current.day);
        }

        private void OnInteracted(Interacted message)
        {
            if (message.InteractionId == config.SleepInteractionId && clockRunner.Clock.IsStarted && !_dayEnding)
            {
                eventBus.Publish(new SleepOffered());
            }
        }

        private void OnSleepConfirmed(SleepConfirmed message)
        {
            if (clockRunner.Clock.IsStarted && !_dayEnding)
            {
                clockRunner.Clock.StartNextDay();
            }
        }

        // The clock carries on into the next morning in this scene; reloading the wake scene
        // starts that morning cleanly from the save, exactly as when the game is launched again.
        private void OnDayEnded(DayEnded message)
        {
            if (_dayEnding)
            {
                return;
            }

            _dayEnding = true;
            var next = _current.ForNextDay(message.Day);
            eventBus.Publish(new GameSaving(next));
            Write(next);
            _current = next;
            Debug.Log($"[Save] Day {message.Day} saved in game {next.slotId}");
            if (_reloadWakeScene)
            {
                SceneManager.LoadScene(config.WakeScene);
            }
        }

        private void Write(SaveData data)
        {
            data.lastPlayedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            _store.Save(data);
        }
    }
}
