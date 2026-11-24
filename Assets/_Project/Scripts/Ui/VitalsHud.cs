using Moonbrew.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Health and stamina bars, bottom left, with their values. Display only: it learns everything from the event bus.
    [RequireComponent(typeof(UIDocument))]
    public class VitalsHud : MonoBehaviour
    {
        private const string BoxClass = "vitals";
        private const string RowClass = "vitals__row";
        private const string IconClass = "vitals__icon";
        private const string BarClass = "vitals__bar";
        private const string TrackClass = "vitals__track";
        private const string FillClass = "vitals__fill";
        private const string FrameClass = "vitals__frame";
        private const string ValueClass = "vitals__value";
        private const string HealthModifier = "--health";
        private const string StaminaModifier = "--stamina";
        private const float FullPercent = 100f;

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private StyleSheet styleSheet;
        [Tooltip("{0} = current value, {1} = maximum.")]
        [SerializeField] private string valueFormat = "{0}/{1}";

        private UIDocument _document;
        private VisualElement _box;
        private Gauge _health;
        private Gauge _stamina;
        private bool _subscribed;

        public float HealthPercent => _health.Percent;

        public float StaminaPercent => _stamina.Percent;

        public string HealthText => _health.Value?.text;

        public string StaminaText => _stamina.Value?.text;

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
            _health = default;
            _stamina = default;
        }

        private void OnHealthChanged(HealthChanged message)
        {
            if (EnsureBox())
            {
                Show(_health, message.Current, message.Max);
            }
        }

        private void OnStaminaChanged(StaminaChanged message)
        {
            if (EnsureBox())
            {
                Show(_stamina, message.Current, message.Max);
            }
        }

        private void Show(Gauge gauge, int current, int max)
        {
            if (max <= 0)
            {
                return;
            }

            gauge.Fill.style.width = Length.Percent(FullPercent * Mathf.Clamp(current, 0, max) / max);
            gauge.Value.text = string.Format(valueFormat, current, max);
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

            _box = Part(new VisualElement(), BoxClass);
            _health = Row(HealthModifier);
            _stamina = Row(StaminaModifier);
            root.Add(_box);
            return true;
        }

        private Gauge Row(string modifier)
        {
            var row = Part(new VisualElement(), RowClass);
            row.Add(Part(new VisualElement(), IconClass, modifier));
            var bar = Part(new VisualElement(), BarClass, modifier);
            var fill = Part(new VisualElement(), FillClass);
            var track = Part(new VisualElement(), TrackClass);
            track.Add(fill);
            bar.Add(track);
            bar.Add(Part(new VisualElement(), FrameClass));
            row.Add(bar);
            var value = Part(new Label(), ValueClass);
            row.Add(value);
            _box.Add(row);
            return new Gauge { Fill = fill, Value = value };
        }

        private static T Part<T>(T element, string className, string modifier = null) where T : VisualElement
        {
            element.pickingMode = PickingMode.Ignore;
            element.AddToClassList(className);
            if (modifier != null)
            {
                element.AddToClassList(className + modifier);
            }

            return element;
        }

        private void Subscribe()
        {
            if (_subscribed || eventBus == null || !isActiveAndEnabled)
            {
                return;
            }

            eventBus.Subscribe<HealthChanged>(OnHealthChanged);
            eventBus.Subscribe<StaminaChanged>(OnStaminaChanged);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && eventBus != null)
            {
                eventBus.Unsubscribe<HealthChanged>(OnHealthChanged);
                eventBus.Unsubscribe<StaminaChanged>(OnStaminaChanged);
            }

            _subscribed = false;
        }

        private struct Gauge
        {
            public VisualElement Fill;
            public Label Value;

            public float Percent => Fill != null ? Fill.style.width.value.value : -1f;
        }
    }
}
