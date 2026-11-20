using Moonbrew.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Escape pauses the game: a strip on the left offers to resume, go back to the title screen or quit.
    // Pausing goes through GameMenuToggled, like the recipe book: the clock, the player and the customers stop.
    [RequireComponent(typeof(UIDocument))]
    public class PauseMenu : MonoBehaviour
    {
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private VisualTreeAsset template;
        [SerializeField] private InputAction toggle = new InputAction("Pause", InputActionType.Button, "<Keyboard>/escape");

        private VisualElement _menu;
        private bool _otherMenuOpen;

        public bool IsOpen => _menu != null;

        public Button ButtonNamed(string buttonName) => _menu?.Q<Button>(buttonName);

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(EventBusAsset bus, VisualTreeAsset menuTemplate)
        {
            eventBus = bus;
            template = menuTemplate;
        }

        // Not over another menu such as the recipe book: Escape is for one menu at a time.
        public void Open()
        {
            if (IsOpen || _otherMenuOpen)
            {
                return;
            }

            _menu = template.Instantiate();
            _menu.style.position = Position.Absolute;
            _menu.style.left = 0;
            _menu.style.top = 0;
            _menu.style.right = 0;
            _menu.style.bottom = 0;
            GetComponent<UIDocument>().rootVisualElement.Add(_menu);
            ButtonNamed("resume").clicked += Close;
            ButtonNamed("title-screen").clicked += OnTitleScreen;
            ButtonNamed("quit").clicked += OnQuit;
            eventBus.Publish(new GameMenuToggled(true));
        }

        public void Close()
        {
            if (!IsOpen)
            {
                return;
            }

            _menu.RemoveFromHierarchy();
            _menu = null;
            eventBus.Publish(new GameMenuToggled(false));
        }

        private void Awake()
        {
            if (eventBus == null || template == null)
            {
                Debug.LogError($"{nameof(PauseMenu)} on {name} needs the event bus and its template.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            eventBus.Subscribe<GameMenuToggled>(OnGameMenuToggled);
            toggle.performed += OnToggle;
            toggle.Enable();
        }

        private void OnDisable()
        {
            eventBus.Unsubscribe<GameMenuToggled>(OnGameMenuToggled);
            toggle.performed -= OnToggle;
            toggle.Disable();
            Close();
        }

        // Tells the other menus apart from this one, which publishes the same event.
        private void OnGameMenuToggled(GameMenuToggled message)
        {
            if (!IsOpen)
            {
                _otherMenuOpen = message.Open;
            }
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            if (IsOpen)
            {
                Close();
            }
            else
            {
                Open();
            }
        }

        private void OnTitleScreen()
        {
            eventBus.Publish(new TitleScreenChosen());
        }

        private void OnQuit()
        {
            eventBus.Publish(new QuitChosen());
        }
    }
}
