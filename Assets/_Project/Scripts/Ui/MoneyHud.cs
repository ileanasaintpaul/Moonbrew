using Moonbrew.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Shows the balance under the clock. Display only: it learns everything from the event bus.
    [RequireComponent(typeof(UIDocument))]
    public class MoneyHud : MonoBehaviour
    {
        private const string BoxClass = "money-hud";
        private const string IconClass = "money-hud__icon";
        private const string AmountClass = "money-hud__amount";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private StyleSheet styleSheet;

        private UIDocument _document;
        private VisualElement _box;
        private Label _amount;
        private bool _subscribed;

        public string AmountText => _amount?.text;

        public void Init(EventBusAsset bus)
        {
            Unsubscribe();
            eventBus = bus;
            Subscribe();
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
            Unsubscribe();
            _box?.RemoveFromHierarchy();
            _box = null;
            _amount = null;
        }

        private void OnMoneyChanged(MoneyChanged message)
        {
            if (EnsureBox())
            {
                _amount.text = message.Balance.ToString();
            }
        }

        // The UIDocument builds its root when enabled, which may come after this component.
        private bool EnsureBox()
        {
            if (_box != null)
            {
                return true;
            }

            var root = _document != null ? _document.rootVisualElement : null;
            if (root == null)
            {
                return false;
            }

            if (styleSheet != null && !root.styleSheets.Contains(styleSheet))
            {
                root.styleSheets.Add(styleSheet);
            }

            _box = new VisualElement { pickingMode = PickingMode.Ignore };
            _box.AddToClassList(BoxClass);
            var icon = new VisualElement { pickingMode = PickingMode.Ignore };
            icon.AddToClassList(IconClass);
            _amount = new Label { pickingMode = PickingMode.Ignore };
            _amount.AddToClassList(AmountClass);
            _box.Add(icon);
            _box.Add(_amount);
            root.Add(_box);
            return true;
        }

        private void Subscribe()
        {
            if (_subscribed || eventBus == null || !isActiveAndEnabled)
            {
                return;
            }

            eventBus.Subscribe<MoneyChanged>(OnMoneyChanged);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && eventBus != null)
            {
                eventBus.Unsubscribe<MoneyChanged>(OnMoneyChanged);
            }

            _subscribed = false;
        }
    }
}
