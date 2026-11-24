using Moonbrew.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // The stamina bar, under the balance. Display only: it learns everything from the event bus.
    [RequireComponent(typeof(UIDocument))]
    public class StaminaHud : MonoBehaviour
    {
        private const string BarClass = "stamina-hud";
        private const string FillClass = "stamina-hud__fill";
        private const float FullPercent = 100f;

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private StyleSheet styleSheet;

        private UIDocument _document;
        private VisualElement _bar;
        private VisualElement _fill;
        private bool _subscribed;

        public float FillPercent => _fill != null ? _fill.style.width.value.value : -1f;

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
            _bar?.RemoveFromHierarchy();
            _bar = null;
            _fill = null;
        }

        private void OnStaminaChanged(StaminaChanged message)
        {
            if (EnsureBar() && message.Max > 0)
            {
                _fill.style.width = Length.Percent(FullPercent * message.Current / message.Max);
            }
        }

        // The UIDocument builds its root when enabled, which may come after this component.
        private bool EnsureBar()
        {
            if (_bar != null)
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

            _bar = new VisualElement { pickingMode = PickingMode.Ignore };
            _bar.AddToClassList(BarClass);
            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.AddToClassList(FillClass);
            _bar.Add(_fill);
            root.Add(_bar);
            return true;
        }

        private void Subscribe()
        {
            if (_subscribed || eventBus == null || !isActiveAndEnabled)
            {
                return;
            }

            eventBus.Subscribe<StaminaChanged>(OnStaminaChanged);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && eventBus != null)
            {
                eventBus.Unsubscribe<StaminaChanged>(OnStaminaChanged);
            }

            _subscribed = false;
        }
    }
}
