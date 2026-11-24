using System.Collections.Generic;
using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Npc;
using Moonbrew.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // When the owner takes a table's order, each customer in turn appears at the bottom right and says what they want,
    // with a face and a pose, in a speech bubble beside them. A click shows the whole line, then the next one.
    // The game waits meanwhile. Drawn on its own panel at screen resolution, so the portrait stays sharp.
    [RequireComponent(typeof(UIDocument))]
    public class DialoguePanel : MonoBehaviour
    {
        private const string ScreenClass = "dialogue";
        private const string PortraitClass = "dialogue__portrait";
        private const string BubbleClass = "dialogue__bubble";
        private const string NameClass = "dialogue__name";
        private const string TextClass = "dialogue__text";
        private const string TailClass = "dialogue__tail";
        private const string HintClass = "dialogue__hint";
        private const string HintText = "▼";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private VillagerCatalog villagers;
        [SerializeField] private ItemCatalog items;
        [SerializeField] private StyleSheet styleSheet;
        [Tooltip("What customers call the owner when she has no name yet.")]
        [SerializeField] private string ownerFallbackAddress = "friend";
        [SerializeField, Min(1f)] private float lettersPerSecond = 45f;
        [Tooltip("The size the dialogue is laid out at, scaled to the screen.")]
        [SerializeField] private Vector2Int referenceResolution = new Vector2Int(640, 360);
        [SerializeField] private int sortingOrderAbove = 20;
        [Tooltip("Where the portrait model stands, far from the game world so its camera sees nothing else.")]
        [SerializeField] private Vector3 stagePosition = new Vector3(0f, -500f, 0f);
        [SerializeField] private Vector2Int portraitSize = new Vector2Int(480, 600);
        [Tooltip("Camera distance from the portrait's head, and how far below the head it aims.")]
        [SerializeField] private float cameraDistance = 3.1f;
        [SerializeField] private float cameraDrop = 0.15f;
        [SerializeField, Range(5f, 60f)] private float cameraFieldOfView = 24f;

        private readonly Queue<DialogueScript.Turn> _turns = new Queue<DialogueScript.Turn>();
        private readonly List<DialogueLine> _lines = new List<DialogueLine>();
        private UIDocument _document;
        private GameObject _overlay;
        private PanelSettings _overlayPanel;
        private VisualElement _screen;
        private VisualElement _portrait;
        private Label _name;
        private Label _text;
        private GameObject _stage;
        private RenderTexture _portraitTexture;
        private PortraitPoser _poser;
        private VillagerData _speaker;
        private string _ownerName;
        private string _dishName;
        private string _line;
        private int _lineIndex;
        private float _typing;
        private bool _subscribed;

        public bool IsOpen => _screen != null;

        public string SpeakerName => _name?.text;

        public string LineText => _line;

        public PortraitPoser Poser => _poser;

        public void Init(EventBusAsset bus, VillagerCatalog villagerCatalog, ItemCatalog itemCatalog)
        {
            Unsubscribe();
            eventBus = bus;
            villagers = villagerCatalog;
            items = itemCatalog;
            Subscribe();
        }

        // A click: the whole line first, then the next one, then the next customer; the last line closes it. Also used by tests.
        public void Advance()
        {
            if (!IsOpen)
            {
                return;
            }

            if (DialogueScript.Revealed(_line, _typing, lettersPerSecond) < _line.Length)
            {
                _typing = float.MaxValue;
                ShowTyped();
                return;
            }

            if (_lineIndex + 1 < _lines.Count)
            {
                ShowLine(_lineIndex + 1);
            }
            else if (_turns.Count > 0)
            {
                NextSpeaker();
            }
            else
            {
                Close();
            }
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Close();
            Unsubscribe();
        }

        private void OnDestroy()
        {
            if (_overlay != null)
            {
                Destroy(_overlay);
            }

            if (_overlayPanel != null)
            {
                Destroy(_overlayPanel);
            }
        }

        private void OnOrderTaken(OrderTaken message)
        {
            if (IsOpen || message.Restored)
            {
                return;
            }

            var turns = DialogueScript.Speakers(message.CustomerIds, message.DishIds, villagers);
            if (turns.Count == 0 || !Open())
            {
                return;
            }

            foreach (var turn in turns)
            {
                _turns.Enqueue(turn);
            }

            NextSpeaker();
        }

        private void OnLoaded(GameLoaded message)
        {
            _ownerName = message.Data?.ownerName;
        }

        private void OnNewGame(NewGameStarted message)
        {
            _ownerName = message.Data?.ownerName;
        }

        private bool Open()
        {
            var root = OverlayRoot();
            if (root == null)
            {
                return false;
            }

            eventBus.Publish(new GameMenuToggled(true));
            eventBus.Publish(new DialogueToggled(true));
            _screen = Part(new VisualElement(), ScreenClass);
            _screen.pickingMode = PickingMode.Position;
            _screen.RegisterCallback<PointerDownEvent>(_ => Advance());
            _portrait = Part(new VisualElement(), PortraitClass);
            var bubble = Part(new VisualElement(), BubbleClass);
            _name = Part(new Label(), NameClass);
            _text = Part(new Label(), TextClass);
            bubble.Add(_name);
            bubble.Add(_text);
            bubble.Add(Part(new VisualElement(), TailClass));
            bubble.Add(Part(new Label(HintText), HintClass));
            _screen.Add(_portrait);
            _screen.Add(bubble);
            root.Add(_screen);
            return true;
        }

        // A panel of its own, drawn straight to the screen rather than through the pixelated image.
        private VisualElement OverlayRoot()
        {
            if (_overlay != null)
            {
                return _overlay.GetComponent<UIDocument>().rootVisualElement;
            }

            if (_document == null || _document.panelSettings == null)
            {
                return null;
            }

            _overlayPanel = Instantiate(_document.panelSettings);
            _overlayPanel.name = "Dialogue Panel";
            _overlayPanel.targetTexture = null;
            _overlayPanel.clearColor = false;
            _overlayPanel.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            _overlayPanel.referenceResolution = referenceResolution;
            _overlayPanel.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            _overlayPanel.match = 1f;
            _overlayPanel.sortingOrder = _document.panelSettings.sortingOrder + sortingOrderAbove;

            // Not a child of this object: a UIDocument under another one joins its visual tree.
            _overlay = new GameObject("Dialogue Overlay");
            _overlay.SetActive(false);
            _overlay.AddComponent<UIDocument>().panelSettings = _overlayPanel;
            _overlay.SetActive(true);
            var root = _overlay.GetComponent<UIDocument>().rootVisualElement;
            if (styleSheet != null)
            {
                root.styleSheets.Add(styleSheet);
            }

            return root;
        }

        private void NextSpeaker()
        {
            var turn = _turns.Dequeue();
            _speaker = turn.Speaker;
            var dish = items != null ? items.Find(turn.DishId) : null;
            _dishName = dish != null ? dish.DisplayName : turn.DishId;
            _lines.Clear();
            foreach (var line in _speaker.OrderLines)
            {
                if (line.Speaker == Speaker.Villager)
                {
                    _lines.Add(line);
                }
            }

            BuildStage(_speaker);
            ShowLine(0);
        }

        private void ShowLine(int index)
        {
            _lineIndex = index;
            var line = _lines[index];
            _line = DialogueScript.Fill(line.Text, string.IsNullOrEmpty(_ownerName) ? ownerFallbackAddress : _ownerName, _dishName);
            _typing = 0f;
            _name.text = _speaker.DisplayName;
            _poser?.Show(line.Expression, line.Pose);
            ShowTyped();
        }

        // Typing is visual only, on real time: the game clock is paused.
        private void Update()
        {
            if (!IsOpen)
            {
                return;
            }

            _typing = Mathf.Min(float.MaxValue, _typing + Time.unscaledDeltaTime);
            ShowTyped();
        }

        private void ShowTyped()
        {
            var shown = DialogueScript.Revealed(_line, _typing, lettersPerSecond);
            if (_text.text.Length != shown)
            {
                _text.text = _line.Substring(0, shown);
            }
        }

        private void BuildStage(VillagerData speaker)
        {
            ClearStage();
            if (speaker.PortraitModel == null)
            {
                return;
            }

            _stage = new GameObject("Dialogue Portrait");
            _stage.transform.position = stagePosition;
            var model = Instantiate(speaker.PortraitModel, stagePosition, Quaternion.identity, _stage.transform);
            model.SetActive(true);
            _poser = model.GetComponent<PortraitPoser>();
            var animator = model.GetComponent<Animator>();
            var head = animator != null && animator.isHuman ? animator.GetBoneTransform(HumanBodyBones.Head) : model.transform;
            var aim = head.position + Vector3.down * cameraDrop;

            _portraitTexture = new RenderTexture(portraitSize.x, portraitSize.y, 24, RenderTextureFormat.ARGB32) { name = "Dialogue Portrait" };
            var cameraObject = new GameObject("Portrait Camera");
            cameraObject.transform.SetParent(_stage.transform);
            cameraObject.transform.SetPositionAndRotation(aim + Vector3.forward * cameraDistance, Quaternion.LookRotation(Vector3.back));
            var portraitCamera = cameraObject.AddComponent<Camera>();
            portraitCamera.clearFlags = CameraClearFlags.SolidColor;
            portraitCamera.backgroundColor = Color.clear;
            portraitCamera.fieldOfView = cameraFieldOfView;
            portraitCamera.nearClipPlane = 0.1f;
            portraitCamera.farClipPlane = cameraDistance * 3f;
            portraitCamera.targetTexture = _portraitTexture;

            var lightFrom = aim + new Vector3(0.8f, 0.9f, 1.4f);
            var lightObject = new GameObject("Portrait Light");
            lightObject.transform.SetParent(_stage.transform);
            lightObject.transform.SetPositionAndRotation(lightFrom, Quaternion.LookRotation(aim - lightFrom));
            var portraitLight = lightObject.AddComponent<Light>();
            portraitLight.type = LightType.Spot;
            portraitLight.range = 6f;
            portraitLight.spotAngle = 60f;
            portraitLight.intensity = 6f;
            _portrait.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_portraitTexture));
        }

        private void ClearStage()
        {
            _poser = null;
            if (_stage != null)
            {
                Destroy(_stage);
                _stage = null;
            }

            if (_portraitTexture != null)
            {
                _portraitTexture.Release();
                Destroy(_portraitTexture);
                _portraitTexture = null;
            }
        }

        private void Close()
        {
            _turns.Clear();
            if (_screen == null)
            {
                return;
            }

            _screen.RemoveFromHierarchy();
            _screen = null;
            _portrait = null;
            _name = null;
            _text = null;
            ClearStage();
            eventBus.Publish(new DialogueToggled(false));
            eventBus.Publish(new GameMenuToggled(false));
        }

        private static T Part<T>(T element, string className) where T : VisualElement
        {
            element.pickingMode = PickingMode.Ignore;
            element.AddToClassList(className);
            return element;
        }

        private void Subscribe()
        {
            if (_subscribed || eventBus == null || !isActiveAndEnabled)
            {
                return;
            }

            eventBus.Subscribe<OrderTaken>(OnOrderTaken);
            eventBus.Subscribe<GameLoaded>(OnLoaded);
            eventBus.Subscribe<NewGameStarted>(OnNewGame);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && eventBus != null)
            {
                eventBus.Unsubscribe<OrderTaken>(OnOrderTaken);
                eventBus.Unsubscribe<GameLoaded>(OnLoaded);
                eventBus.Unsubscribe<NewGameStarted>(OnNewGame);
            }

            _subscribed = false;
        }
    }
}
