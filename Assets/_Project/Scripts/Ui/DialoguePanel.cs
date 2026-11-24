using Moonbrew.Core;
using Moonbrew.Inventory;
using Moonbrew.Npc;
using Moonbrew.Player;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // A villager's order, told as a conversation: their portrait at the bottom right, alive with faces and poses,
    // and a speech bubble beside it. A click shows the whole line, then the next one. The game waits meanwhile.
    [RequireComponent(typeof(UIDocument))]
    public class DialoguePanel : MonoBehaviour
    {
        private const string ScreenClass = "dialogue";
        private const string PortraitClass = "dialogue__portrait";
        private const string BubbleClass = "dialogue__bubble";
        private const string OwnerBubbleClass = "dialogue__bubble--owner";
        private const string NameClass = "dialogue__name";
        private const string TextClass = "dialogue__text";
        private const string TailClass = "dialogue__tail";
        private const string HintClass = "dialogue__hint";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private VillagerCatalog villagers;
        [SerializeField] private ItemCatalog items;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private string ownerFallbackName = "You";
        [Tooltip("What villagers call the owner when she has no name yet.")]
        [SerializeField] private string ownerFallbackAddress = "friend";
        [SerializeField, Min(1f)] private float lettersPerSecond = 45f;
        [Tooltip("Where the portrait model stands, far from the game world so its camera sees nothing else.")]
        [SerializeField] private Vector3 stagePosition = new Vector3(0f, -500f, 0f);
        [SerializeField] private Vector2Int portraitSize = new Vector2Int(384, 448);
        [Tooltip("Camera distance from the portrait's head, and how far below the head it aims.")]
        [SerializeField] private float cameraDistance = 2.1f;
        [SerializeField] private float cameraDrop = 0.32f;
        [SerializeField, Range(5f, 60f)] private float cameraFieldOfView = 24f;

        private UIDocument _document;
        private VisualElement _screen;
        private Label _name;
        private Label _text;
        private VisualElement _bubble;
        private GameObject _stage;
        private RenderTexture _portraitTexture;
        private PortraitPoser _poser;
        private VillagerData _villager;
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

        // A click: the whole line first, then the next one; the last one closes the conversation. Also used by tests.
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

            ShowLine(_lineIndex + 1);
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

        private void OnOrderTaken(OrderTaken message)
        {
            if (IsOpen || message.Restored || !DialogueScript.FindSpeaker(message.CustomerIds, message.DishIds, villagers, out var villager, out var dishId))
            {
                return;
            }

            var dish = items != null ? items.Find(dishId) : null;
            _dishName = dish != null ? dish.DisplayName : dishId;
            Open(villager);
        }

        private void OnLoaded(GameLoaded message)
        {
            _ownerName = message.Data?.ownerName;
        }

        private void OnNewGame(NewGameStarted message)
        {
            _ownerName = message.Data?.ownerName;
        }

        private void Open(VillagerData villager)
        {
            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null)
            {
                return;
            }

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            _villager = villager;
            eventBus.Publish(new GameMenuToggled(true));
            eventBus.Publish(new DialogueToggled(true));
            BuildStage(villager);
            _screen = Part(new VisualElement(), ScreenClass);
            _screen.pickingMode = PickingMode.Position;
            _screen.RegisterCallback<PointerDownEvent>(_ => Advance());
            var portrait = Part(new VisualElement(), PortraitClass);
            if (_portraitTexture != null)
            {
                portrait.style.backgroundImage = new StyleBackground(Background.FromRenderTexture(_portraitTexture));
            }

            _bubble = Part(new VisualElement(), BubbleClass);
            _name = Part(new Label(), NameClass);
            _text = Part(new Label(), TextClass);
            _bubble.Add(_name);
            _bubble.Add(_text);
            _bubble.Add(Part(new VisualElement(), TailClass));
            _bubble.Add(Part(new Label("▼"), HintClass));
            _screen.Add(portrait);
            _screen.Add(_bubble);
            root.Add(_screen);
            ShowLine(0);
        }

        private void ShowLine(int index)
        {
            if (index >= _villager.OrderLines.Count)
            {
                Close();
                return;
            }

            _lineIndex = index;
            var line = _villager.OrderLines[index];
            var byOwner = line.Speaker == Speaker.Owner;
            var owner = string.IsNullOrEmpty(_ownerName) ? ownerFallbackName : _ownerName;
            _line = DialogueScript.Fill(line.Text, string.IsNullOrEmpty(_ownerName) ? ownerFallbackAddress : _ownerName, _dishName);
            _typing = 0f;
            _name.text = byOwner ? owner : _villager.DisplayName;
            _bubble.EnableInClassList(OwnerBubbleClass, byOwner);
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

            var villagerTalking = shown < _line.Length && _villager.OrderLines[_lineIndex].Speaker == Speaker.Villager;
            _poser?.SetTalking(villagerTalking);
        }

        private void BuildStage(VillagerData villager)
        {
            if (villager.PortraitModel == null)
            {
                return;
            }

            _stage = new GameObject("Dialogue Portrait");
            _stage.transform.position = stagePosition;
            var model = Instantiate(villager.PortraitModel, stagePosition, Quaternion.identity, _stage.transform);
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

            var lightObject = new GameObject("Portrait Light");
            lightObject.transform.SetParent(_stage.transform);
            lightObject.transform.SetPositionAndRotation(aim + new Vector3(0.8f, 0.9f, 1.4f), Quaternion.LookRotation(aim - (aim + new Vector3(0.8f, 0.9f, 1.4f))));
            var portraitLight = lightObject.AddComponent<Light>();
            portraitLight.type = LightType.Spot;
            portraitLight.range = 5f;
            portraitLight.spotAngle = 60f;
            portraitLight.intensity = 6f;
        }

        private void Close()
        {
            if (_screen == null)
            {
                return;
            }

            _screen.RemoveFromHierarchy();
            _screen = null;
            _bubble = null;
            _name = null;
            _text = null;
            _poser = null;
            if (_stage != null)
            {
                Destroy(_stage);
            }

            if (_portraitTexture != null)
            {
                _portraitTexture.Release();
                Destroy(_portraitTexture);
            }

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
