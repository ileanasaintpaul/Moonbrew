using Moonbrew.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Shows the current day, phase and time. Display only: it learns everything from the event bus
    // and has no reference to the clock.
    [RequireComponent(typeof(UIDocument))]
    public class ClockHud : MonoBehaviour
    {
        private const string LabelClass = "clock-hud";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private string dayFormat = "Jour {0} — {1} — {2}";
        [SerializeField] private string[] phaseNames = { "Matin", "Journée", "Soir", "Nuit" };

        private UIDocument _document;
        private Label _label;
        private bool _subscribed;
        private bool _hasPhase;
        private int _day;
        private DayPhase _phase;
        private int _minutes;

        public string Text => _label?.text;

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
            _label?.RemoveFromHierarchy();
            _label = null;
        }

        private void OnDayStarted(DayStarted message)
        {
            _day = message.Day;
            Refresh();
        }

        private void OnPhaseChanged(PhaseChanged message)
        {
            _day = message.Day;
            _phase = message.Phase;
            _hasPhase = true;
            Refresh();
        }

        private void OnTimeChanged(TimeChanged message)
        {
            _minutes = message.Minutes;
            Refresh();
        }

        // Nothing is shown until the clock has announced its first phase.
        private void Refresh()
        {
            if (!_hasPhase || !EnsureLabel())
            {
                return;
            }

            _label.text = ClockLabel.Format(dayFormat, phaseNames, _day, _phase, _minutes);
        }

        // The UIDocument builds its root when enabled, which may come after this component: create the label lazily.
        private bool EnsureLabel()
        {
            if (_label != null)
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

            _label = new Label();
            _label.AddToClassList(LabelClass);
            root.Add(_label);
            return true;
        }

        private void Subscribe()
        {
            if (_subscribed || eventBus == null || !isActiveAndEnabled)
            {
                return;
            }

            eventBus.Subscribe<DayStarted>(OnDayStarted);
            eventBus.Subscribe<PhaseChanged>(OnPhaseChanged);
            eventBus.Subscribe<TimeChanged>(OnTimeChanged);
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && eventBus != null)
            {
                eventBus.Unsubscribe<DayStarted>(OnDayStarted);
                eventBus.Unsubscribe<PhaseChanged>(OnPhaseChanged);
                eventBus.Unsubscribe<TimeChanged>(OnTimeChanged);
            }

            _subscribed = false;
        }
    }
}
