using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using Moonbrew.Core;
using Moonbrew.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Title screen, main menu, saved games and settings. Shows the screens and passes the player's
    // choices on the bus; GameLauncher starts the game.
    [RequireComponent(typeof(UIDocument))]
    public class TitleMenu : MonoBehaviour
    {
        private const string ShownClass = "screen--shown";
        private const string StarClass = "star";
        private const string BigStarClass = "star--big";
        private const string DimStarClass = "star--dim";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private SessionConfig sessionConfig;
        [Tooltip("Applies the pixel rendering setting at once; it is remembered for the game scenes either way.")]
        [SerializeField] private PixelRenderer pixelRenderer;
        [SerializeField] private InputAction anyKey = new InputAction("AnyKey", InputActionType.Button, "<Keyboard>/anyKey");
        [SerializeField] private InputAction back = new InputAction("Back", InputActionType.Button, "<Keyboard>/escape");

        [Header("Texts")]
        [SerializeField] private string dayFormat = "Day {0}";
        [SerializeField] private string lastPlayedFormat = "{0:d MMM yyyy, HH:mm}";
        [SerializeField] private string onText = "On";
        [SerializeField] private string offText = "Off";
        [SerializeField] private string resolutionFormat = "{0} x {1}";

        [Header("Sky")]
        [SerializeField, Min(0)] private int starCount = 70;
        [SerializeField, Range(0f, 1f)] private float bigStarShare = 0.15f;
        [SerializeField] private int starSeed = 7;
        [SerializeField, Min(50)] private long twinkleMilliseconds = 900;

        private readonly List<VisualElement> _stars = new List<VisualElement>();
        private readonly List<Resolution> _resolutions = new List<Resolution>();
        private GameSlots _slots;
        private VisualElement _root;
        private VisualElement _current;
        private System.Random _random;

        public string CurrentScreen => _current?.name;

        public Button ButtonNamed(string buttonName) => _root?.Q<Button>(buttonName);

        // The saved games list, most recent first. For tests.
        public int GameRowCount => _root.Q<ScrollView>("games").childCount;

        public Button GameRowAt(int index) => (Button)_root.Q<ScrollView>("games")[index];

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(EventBusAsset bus, string savesFolder)
        {
            eventBus = bus;
            _slots = new GameSlots(new SaveStore(savesFolder));
        }

        public void OpenMainMenu()
        {
            Show("main-screen");
            ButtonNamed("continue").SetEnabled(_slots.CanContinue);
        }

        private void Awake()
        {
            if (eventBus == null || (_slots == null && sessionConfig == null))
            {
                Debug.LogError($"{nameof(TitleMenu)} on {name} needs the event bus and the session config.", this);
                enabled = false;
                return;
            }

            _slots ??= new GameSlots(new SaveStore(Path.Combine(Application.persistentDataPath, sessionConfig.SavesFolder)));
            _random = new System.Random(starSeed);
        }

        private void OnEnable()
        {
            _root = GetComponent<UIDocument>().rootVisualElement;
            _root.Q("title-screen").RegisterCallback<PointerDownEvent>(OnTitleClicked);
            ButtonNamed("continue").clicked += OnContinue;
            ButtonNamed("new-game").clicked += OnNewGame;
            ButtonNamed("load").clicked += OpenLoad;
            ButtonNamed("settings").clicked += OpenSettings;
            ButtonNamed("quit").clicked += OnQuit;
            ButtonNamed("load-back").clicked += OpenMainMenu;
            ButtonNamed("settings-back").clicked += OpenMainMenu;
            ButtonNamed("fullscreen").clicked += OnFullscreen;
            ButtonNamed("resolution-previous").clicked += OnPreviousResolution;
            ButtonNamed("resolution-next").clicked += OnNextResolution;
            ButtonNamed("pixel-mode").clicked += OnPixelMode;

            anyKey.performed += OnAnyKey;
            back.performed += OnBack;
            anyKey.Enable();
            back.Enable();

            AddStars();
            Show("title-screen");
        }

        private void OnDisable()
        {
            anyKey.performed -= OnAnyKey;
            back.performed -= OnBack;
            anyKey.Disable();
            back.Disable();
        }

        private void Show(string screenName)
        {
            _current?.RemoveFromClassList(ShownClass);
            _current = _root.Q(screenName);
            _current.AddToClassList(ShownClass);
        }

        private void OnTitleClicked(PointerDownEvent evt)
        {
            OpenMainMenu();
        }

        private void OnAnyKey(InputAction.CallbackContext context)
        {
            if (CurrentScreen == "title-screen")
            {
                OpenMainMenu();
            }
        }

        private void OnBack(InputAction.CallbackContext context)
        {
            if (CurrentScreen == "load-screen" || CurrentScreen == "settings-screen")
            {
                OpenMainMenu();
            }
        }

        private void OnContinue()
        {
            eventBus.Publish(new ContinueChosen());
        }

        private void OnNewGame()
        {
            eventBus.Publish(new NewGameChosen());
        }

        private void OnQuit()
        {
            eventBus.Publish(new QuitChosen());
        }

        private void OpenLoad()
        {
            var games = _slots.List();
            var list = _root.Q<ScrollView>("games");
            list.Clear();
            foreach (var game in games)
            {
                list.Add(BuildRow(game));
            }

            _root.Q("no-games").style.display = games.Count == 0 ? DisplayStyle.Flex : DisplayStyle.None;
            Show("load-screen");
        }

        private VisualElement BuildRow(SaveData game)
        {
            var slotId = game.slotId;
            var row = new Button(() => eventBus.Publish(new GameChosen(slotId)));
            row.AddToClassList("game-row");
            var day = new Label(string.Format(CultureInfo.InvariantCulture, dayFormat, game.day)) { pickingMode = PickingMode.Ignore };
            day.AddToClassList("game-row__day");
            row.Add(day);
            var date = new Label(LastPlayed(game)) { pickingMode = PickingMode.Ignore };
            date.AddToClassList("game-row__date");
            row.Add(date);
            return row;
        }

        private string LastPlayed(SaveData game)
        {
            return DateTime.TryParse(game.lastPlayedUtc, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var date)
                ? string.Format(CultureInfo.InvariantCulture, lastPlayedFormat, date.ToLocalTime())
                : string.Empty;
        }

        private void OpenSettings()
        {
            _resolutions.Clear();
            foreach (var resolution in Screen.resolutions)
            {
                if (_resolutions.FindIndex(r => r.width == resolution.width && r.height == resolution.height) < 0)
                {
                    _resolutions.Add(resolution);
                }
            }

            RefreshSettings(Screen.width, Screen.height, Screen.fullScreen);
            Show("settings-screen");
        }

        // Screen changes take effect on the next frame, so the labels show the requested values.
        private void RefreshSettings(int width, int height, bool fullscreen)
        {
            ButtonNamed("fullscreen").text = fullscreen ? onText : offText;
            _root.Q<Label>("resolution").text = string.Format(CultureInfo.InvariantCulture, resolutionFormat, width, height);
            ButtonNamed("pixel-mode").text = DisplayPrefs.PixelMode(pixelRenderer == null || pixelRenderer.IsPixelMode) ? onText : offText;
        }

        private void OnFullscreen()
        {
            var fullscreen = !Screen.fullScreen;
            Screen.fullScreen = fullscreen;
            RefreshSettings(Screen.width, Screen.height, fullscreen);
        }

        private void OnPreviousResolution()
        {
            StepResolution(-1);
        }

        private void OnNextResolution()
        {
            StepResolution(1);
        }

        private void StepResolution(int step)
        {
            if (_resolutions.Count == 0)
            {
                return;
            }

            var index = _resolutions.FindIndex(r => r.width == Screen.width && r.height == Screen.height);
            index = (Math.Max(index, 0) + step + _resolutions.Count) % _resolutions.Count;
            var chosen = _resolutions[index];
            Screen.SetResolution(chosen.width, chosen.height, Screen.fullScreenMode);
            RefreshSettings(chosen.width, chosen.height, Screen.fullScreen);
        }

        private void OnPixelMode()
        {
            var pixelMode = !DisplayPrefs.PixelMode(pixelRenderer == null || pixelRenderer.IsPixelMode);
            DisplayPrefs.SetPixelMode(pixelMode);
            if (pixelRenderer != null)
            {
                pixelRenderer.SetPixelMode(pixelMode);
            }

            RefreshSettings(Screen.width, Screen.height, Screen.fullScreen);
        }

        // Scattered once with a fixed seed, so the sky is the same at every launch; a few stars dim at a time.
        private void AddStars()
        {
            var layer = _root.Q("stars");
            if (_stars.Count > 0)
            {
                return;
            }

            for (var i = 0; i < starCount; i++)
            {
                var star = new VisualElement { pickingMode = PickingMode.Ignore };
                star.AddToClassList(StarClass);
                if (_random.NextDouble() < bigStarShare)
                {
                    star.AddToClassList(BigStarClass);
                }

                star.style.left = Length.Percent((float)_random.NextDouble() * 100f);
                star.style.top = Length.Percent((float)_random.NextDouble() * 70f);
                layer.Add(star);
                _stars.Add(star);
            }

            layer.schedule.Execute(Twinkle).Every(twinkleMilliseconds);
        }

        private void Twinkle()
        {
            if (_stars.Count == 0)
            {
                return;
            }

            _stars[_random.Next(_stars.Count)].ToggleInClassList(DimStarClass);
        }
    }
}
