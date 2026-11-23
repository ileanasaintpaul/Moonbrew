using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Moonbrew.Core
{
    // Carries out the main menu's choices: makes the chosen game the one the session opens, then loads the morning scene.
    // New Game first opens the character creation scene; the game is created once the owner is confirmed there.
    public class GameLauncher : MonoBehaviour
    {
        [SerializeField] private SessionConfig config;
        [SerializeField] private ClockConfig clockConfig;
        [SerializeField] private EventBusAsset eventBus;

        private GameSlots _slots;
        private bool _loadScenes = true;

        // The scene asked for last; tests check it instead of loading it.
        public string LoadedScene { get; private set; }

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(SessionConfig sessionConfig, ClockConfig clock, EventBusAsset bus, string savesFolder, bool loadScenes)
        {
            config = sessionConfig;
            clockConfig = clock;
            eventBus = bus;
            _slots = new GameSlots(new SaveStore(savesFolder));
            _loadScenes = loadScenes;
        }

        private void Awake()
        {
            if (config == null || clockConfig == null || eventBus == null)
            {
                Debug.LogError($"{nameof(GameLauncher)} on {name} needs its session config, clock config and event bus.", this);
                enabled = false;
                return;
            }

            _slots ??= new GameSlots(new SaveStore(Path.Combine(Application.persistentDataPath, config.SavesFolder)));
        }

        private void OnEnable()
        {
            eventBus.Subscribe<ContinueChosen>(OnContinueChosen);
            eventBus.Subscribe<NewGameChosen>(OnNewGameChosen);
            eventBus.Subscribe<GameChosen>(OnGameChosen);
            eventBus.Subscribe<QuitChosen>(OnQuitChosen);
            eventBus.Subscribe<CharacterCreated>(OnCharacterCreated);
        }

        private void OnDisable()
        {
            eventBus.Unsubscribe<ContinueChosen>(OnContinueChosen);
            eventBus.Unsubscribe<NewGameChosen>(OnNewGameChosen);
            eventBus.Unsubscribe<GameChosen>(OnGameChosen);
            eventBus.Unsubscribe<QuitChosen>(OnQuitChosen);
            eventBus.Unsubscribe<CharacterCreated>(OnCharacterCreated);
        }

        private void OnContinueChosen(ContinueChosen message)
        {
            if (_slots.CanContinue)
            {
                LoadWakeScene();
            }
        }

        private void OnNewGameChosen(NewGameChosen message)
        {
            Load(config.CreationScene);
        }

        private void OnCharacterCreated(CharacterCreated message)
        {
            var data = _slots.CreateNew(clockConfig.FirstDay, DateTime.UtcNow, message.OwnerName, message.Appearance, message.PronounsId);
            Debug.Log($"[Save] New game {data.slotId}");
            LoadWakeScene();
        }

        private void OnGameChosen(GameChosen message)
        {
            if (_slots.Choose(message.SlotId, DateTime.UtcNow))
            {
                LoadWakeScene();
            }
        }

        private void OnQuitChosen(QuitChosen message)
        {
            Application.Quit();
        }

        private void LoadWakeScene()
        {
            LoadedScene = config.WakeScene;
            eventBus.Publish(new GameLoadStarted());
            if (_loadScenes)
            {
                LoadAfterAFrame(config.WakeScene);
            }
        }

        // The frame in between draws the loading screen before the load takes the main thread.
        private async void LoadAfterAFrame(string scene)
        {
            await Awaitable.NextFrameAsync();
            if (this != null)
            {
                SceneManager.LoadSceneAsync(scene);
            }
        }

        private void Load(string scene)
        {
            LoadedScene = scene;
            if (_loadScenes)
            {
                SceneManager.LoadScene(scene);
            }
        }
    }
}
