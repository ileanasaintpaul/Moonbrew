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
        private const string CartoucheClass = "clock-hud";
        private const string RowClass = "clock-hud__row";
        private const string IconClass = "clock-hud__icon";
        private const string TimeClass = "clock-hud__time";
        private const string PhaseClass = "clock-hud__phase";
        private const string DayClass = "clock-hud__day";

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private StyleSheet styleSheet;
        [Tooltip("{0} = day number, {1} = phase name, {2} = time.")]
        [SerializeField] private string dayFormat = "Day {0}";
        [SerializeField] private string[] phaseNames = { "Morning", "Day", "Evening", "Night" };

        private UIDocument _document;
        private VisualElement _cartouche;
        private Label _time;
        private Label _phaseLabel;
        private Label _day;
        private bool _subscribed;
        private bool _hasPhase;
        private int _dayNumber;
        private DayPhase _phase;
        private int _minutes;

        public string TimeText => _time?.text;

        public string PhaseText => _phaseLabel?.text;

        public string DayText => _day?.text;

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
            _cartouche?.RemoveFromHierarchy();
            _cartouche = null;
            _time = null;
            _phaseLabel = null;
            _day = null;
        }

        private void OnDayStarted(DayStarted message)
        {
            _dayNumber = message.Day;
            Refresh();
        }

        private void OnPhaseChanged(PhaseChanged message)
        {
            _dayNumber = message.Day;
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
            if (!_hasPhase || !EnsureCartouche())
            {
                return;
            }

            _time.text = ClockLabel.Time(_minutes);
            _phaseLabel.text = phaseNames[(int)_phase];
            _day.text = ClockLabel.Format(dayFormat, phaseNames, _dayNumber, _phase, _minutes);
        }

        // The UIDocument builds its root when enabled, which may come after this component: create the cartouche lazily.
        private bool EnsureCartouche()
        {
            if (_cartouche != null)
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

            _cartouche = new VisualElement { pickingMode = PickingMode.Ignore };
            _cartouche.AddToClassList(CartoucheClass);
            // Hourglass and time on the first row, phase and day on the second.
            var timeRow = Part(new VisualElement(), RowClass);
            timeRow.Add(Part(new VisualElement(), IconClass));
            _time = Part(new Label(), TimeClass);
            timeRow.Add(_time);
            var dayRow = Part(new VisualElement(), RowClass);
            _phaseLabel = Part(new Label(), PhaseClass);
            _day = Part(new Label(), DayClass);
            dayRow.Add(_phaseLabel);
            dayRow.Add(_day);
            _cartouche.Add(timeRow);
            _cartouche.Add(dayRow);
            root.Add(_cartouche);
            return true;
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
