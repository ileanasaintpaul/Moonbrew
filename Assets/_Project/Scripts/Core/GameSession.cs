using System;
using System.Globalization;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Moonbrew.Core
{
    // Runs the day cycle around the save: load the last game, show the recap, start the clock;
    // at the end of the day (sleep or 2 a.m.), save and wake up in the morning scene.
    // The game is never saved in the middle of a day: walking to another scene carries the day in memory.
    public class GameSession : MonoBehaviour
    {
        [SerializeField] private SessionConfig config;
        [SerializeField] private GameClockRunner clockRunner;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private DayCarry dayCarry;
        [Tooltip("On in the scene the player wakes up in: the recap shows and the night passes there.")]
        [SerializeField] private bool isWakeScene = true;
        [SerializeField] private Transform player;
        [SerializeField] private SceneArrival[] arrivals = Array.Empty<SceneArrival>();

        private SaveStore _store;
        private SaveData _current;
        private bool _subscribed;
        private bool _leaving;
        private bool _loadScenes = true;

        public SaveData Current => _current;

        public bool WaitingForRecap { get; private set; }

        // The scene asked for last; tests check it instead of loading it.
        public string LoadedScene { get; private set; }

        // For objects built from code: call on an inactive GameObject, before it is activated.
        // Tests save to a temporary folder and stay in their scene instead of loading another one.
        public void Init(SessionConfig sessionConfig, GameClockRunner runner, EventBusAsset bus, string savesFolder, bool loadScenes, DayCarry carry = null, bool wakeScene = true)
        {
            config = sessionConfig;
            clockRunner = runner;
            eventBus = bus;
            _store = new SaveStore(savesFolder);
            _loadScenes = loadScenes;
            dayCarry = carry;
            isWakeScene = wakeScene;
        }

        // Starts a new game in its own save file, next to the others, then reloads the morning scene.
        public void StartNewGame()
        {
            var data = SaveData.CreateNew(SaveStore.NewSlotId(), clockRunner.Config.FirstDay);
            Write(data);
            Debug.Log($"[Save] New game {data.slotId}");
            LoadScene(config.WakeScene);
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
            eventBus.Subscribe<TravelRequested>(OnTravelRequested);
            eventBus.Subscribe<TitleScreenChosen>(OnTitleScreenChosen);
            eventBus.Subscribe<QuitChosen>(OnQuitChosen);
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
            eventBus.Unsubscribe<TravelRequested>(OnTravelRequested);
            eventBus.Unsubscribe<TitleScreenChosen>(OnTitleScreenChosen);
            eventBus.Unsubscribe<QuitChosen>(OnQuitChosen);
            _subscribed = false;
        }

        // After every OnEnable, so all modules hear the load.
        private void Start()
        {
            if (dayCarry != null && dayCarry.TryTake(out var carried))
            {
                Resume(carried);
                return;
            }

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

            // The day always opens in front of the house: another scene started directly skips the recap.
            if (_current.HasRecap && isWakeScene)
            {
                WaitingForRecap = true;
                eventBus.Publish(new DayRecapOpened(_current.recap));
            }
            else
            {
                clockRunner.Begin(_current.day);
            }
        }

        // The day goes on where the player left it, or ends here when it ran out in the other scene.
        private void Resume(CarriedDay carried)
        {
            _current = carried.Day;
            eventBus.Publish(new GameLoaded(_current));
            if (carried.DayOver)
            {
                EndDay(_current.day);
                return;
            }

            PlacePlayer(carried.Arrival);
            clockRunner.Begin(_current.day, carried.Minutes);
            eventBus.Publish(new TravelArrived());
        }

        private void PlacePlayer(string arrivalId)
        {
            var arrival = Array.Find(arrivals, candidate => candidate != null && candidate.Id == arrivalId);
            if (arrival == null || player == null)
            {
                Debug.LogWarning($"[Session] No arrival {arrivalId} or no player in this scene; the player stays at the scene start.", this);
                return;
            }

            // A CharacterController puts the player back where it was unless it is off while teleporting.
            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            player.SetPositionAndRotation(arrival.transform.position, arrival.transform.rotation);
            if (controller != null)
            {
                controller.enabled = true;
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
            if (message.InteractionId == config.SleepInteractionId && clockRunner.Clock.IsStarted && !_leaving)
            {
                eventBus.Publish(new SleepOffered());
            }
        }

        private void OnSleepConfirmed(SleepConfirmed message)
        {
            if (clockRunner.Clock.IsStarted && !_leaving)
            {
                clockRunner.Clock.StartNextDay();
            }
        }

        private void OnTravelRequested(TravelRequested message)
        {
            if (_leaving || !clockRunner.Clock.IsStarted || string.IsNullOrEmpty(message.Scene))
            {
                return;
            }

            if (dayCarry == null)
            {
                Debug.LogError($"{nameof(GameSession)} on {name} needs the {nameof(DayCarry)} to travel.", this);
                return;
            }

            _leaving = true;
            clockRunner.Clock.SetPaused(true);
            var minutes = clockRunner.Clock.Minutes;
            Carry(message.Arrival, minutes, false);
            Debug.Log($"[Session] Leaving for {message.Scene} at {minutes} min");
            eventBus.Publish(new TravelStarted());
            LoadSceneAfterFade(message.Scene);
        }

        // Back to the title screen from the pause menu. The game is saved only when a day ends,
        // so the day in progress is not kept.
        private void OnTitleScreenChosen(TitleScreenChosen message)
        {
            if (_leaving)
            {
                return;
            }

            _leaving = true;
            LoadScene(config.TitleScene);
        }

        private void OnQuitChosen(QuitChosen message)
        {
            Application.Quit();
        }

        // The clock carries on into the next morning in this scene; reloading the wake scene
        // starts that morning cleanly from the save, exactly as when the game is launched again.
        private void OnDayEnded(DayEnded message)
        {
            if (_leaving)
            {
                return;
            }

            if (isWakeScene || dayCarry == null)
            {
                EndDay(message.Day);
                return;
            }

            // Crops grow and the save is written in the wake scene, where the farm is.
            _leaving = true;
            Carry(null, 0, true);
            LoadScene(config.WakeScene);
        }

        private void EndDay(int day)
        {
            _leaving = true;
            var next = _current.ForNextDay(day);
            eventBus.Publish(new NightPassed(day));
            eventBus.Publish(new GameSaving(next));
            Write(next);
            _current = next;
            Debug.Log($"[Save] Day {day} saved in game {next.slotId}");
            LoadScene(config.WakeScene);
        }

        // Modules write the day as it is now into a copy, which the next scene's session picks up.
        private void Carry(string arrival, int minutes, bool dayOver)
        {
            var day = _current.ForSameDay();
            eventBus.Publish(new GameSaving(day));
            dayCarry.Hold(new CarriedDay(day, minutes, arrival, dayOver));
        }

        private async void LoadSceneAfterFade(string scene)
        {
            // Real seconds: the clock is paused while the screen fades.
            await Awaitable.WaitForSecondsAsync(config.TravelFadeSeconds);
            if (this != null)
            {
                LoadScene(scene);
            }
        }

        private void LoadScene(string scene)
        {
            LoadedScene = scene;
            if (_loadScenes)
            {
                SceneManager.LoadScene(scene);
            }
        }

        private void Write(SaveData data)
        {
            data.lastPlayedUtc = DateTime.UtcNow.ToString("o", CultureInfo.InvariantCulture);
            _store.Save(data);
        }
    }
}
