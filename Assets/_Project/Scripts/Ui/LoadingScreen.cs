using System.Collections.Generic;
using Moonbrew.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // The magic circle shown while a game opens. It survives the scene change, then fades out and removes itself.
    [RequireComponent(typeof(UIDocument))]
    public class LoadingScreen : MonoBehaviour
    {
        private const string ScreenClass = "loading-screen";
        private const string CircleBoxClass = "loading-screen__box";
        private const string GlowClass = "loading-screen__glow";
        private const string CircleClass = "loading-screen__circle";
        private const string SparkleClass = "loading-screen__sparkle";

        [SerializeField] private LoadingScreenData data;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private StyleSheet styleSheet;

        private readonly List<VisualElement> _sparkles = new List<VisualElement>();
        private UIDocument _document;
        private VisualElement _screen;
        private VisualElement _circle;
        private LoadingProgress _progress;
        private float _time;
        private bool _subscribed;

        public bool IsShowing => _progress != null;

        public float Alpha => _progress != null ? _progress.Alpha : 0f;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(LoadingScreenData screenData, EventBusAsset bus)
        {
            data = screenData;
            eventBus = bus;
        }

        // The scene it was waiting for has loaded. Also used by tests, which stay in their scene.
        public void SceneReady()
        {
            _progress?.SceneReady();
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (_subscribed || eventBus == null)
            {
                return;
            }

            eventBus.Subscribe<GameLoadStarted>(OnGameLoadStarted);
            SceneManager.sceneLoaded += OnSceneLoaded;
            _subscribed = true;
        }

        private void OnDisable()
        {
            if (!_subscribed)
            {
                return;
            }

            eventBus.Unsubscribe<GameLoadStarted>(OnGameLoadStarted);
            SceneManager.sceneLoaded -= OnSceneLoaded;
            _subscribed = false;
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneReady();
        }

        private void OnGameLoadStarted(GameLoadStarted message)
        {
            var root = _document != null ? _document.rootVisualElement : null;
            if (IsShowing || data == null || root == null)
            {
                return;
            }

            HideSceneInterfaces();
            if (transform.parent == null)
            {
                DontDestroyOnLoad(gameObject);
            }

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            Build(root);
            _progress = new LoadingProgress(data.MinSeconds, data.FadeSeconds);
        }

        // The menus' sharp texts are drawn on a panel above every other: hidden menus take them away too.
        private void HideSceneInterfaces()
        {
            foreach (var sceneRoot in gameObject.scene.GetRootGameObjects())
            {
                foreach (var document in sceneRoot.GetComponentsInChildren<UIDocument>())
                {
                    if (document != _document && document.rootVisualElement != null)
                    {
                        document.rootVisualElement.style.display = DisplayStyle.None;
                    }
                }
            }
        }

        private void Build(VisualElement root)
        {
            _screen = Part(ScreenClass, null);
            var box = Part(CircleBoxClass, null);
            box.Add(Part(GlowClass, data.Glow));
            _circle = Part(CircleClass, data.Circle);
            box.Add(_circle);
            for (var i = 0; i < data.Sparkles; i++)
            {
                var angle = Mathf.PI * 2f * i / data.Sparkles;
                var sparkle = Part(SparkleClass, data.Sparkle);
                sparkle.style.translate = new Translate(Mathf.Cos(angle) * data.SparkleRadius, Mathf.Sin(angle) * data.SparkleRadius);
                box.Add(sparkle);
                _sparkles.Add(sparkle);
            }

            _screen.Add(box);
            root.Add(_screen);
        }

        private static VisualElement Part(string className, Sprite sprite)
        {
            var part = new VisualElement();
            part.AddToClassList(className);
            if (sprite != null)
            {
                part.style.backgroundImage = new StyleBackground(sprite);
            }

            return part;
        }

        // Visual animation only, on real time: the game clock is not running yet.
        private void Update()
        {
            if (_progress == null)
            {
                return;
            }

            var seconds = Time.unscaledDeltaTime;
            _time += seconds;
            _progress.Tick(seconds);
            _screen.style.opacity = _progress.Alpha;
            _circle.style.rotate = new Rotate(_time * data.CircleDegreesPerSecond);
            for (var i = 0; i < _sparkles.Count; i++)
            {
                _sparkles[i].style.opacity = LoadingProgress.Twinkle(_time, i, data.TwinkleSpeed);
            }

            if (_progress.IsDone)
            {
                _screen.RemoveFromHierarchy();
                _progress = null;
                Destroy(gameObject);
            }
        }
    }
}
