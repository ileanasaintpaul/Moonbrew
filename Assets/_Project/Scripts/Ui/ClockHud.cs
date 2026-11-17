using Moonbrew.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Shows the current day, phase and time. Display only: it reads the clock and never changes it.
    [RequireComponent(typeof(UIDocument))]
    public class ClockHud : MonoBehaviour
    {
        private const string LabelClass = "clock-hud";

        [SerializeField] private GameClockRunner clockRunner;
        [SerializeField] private StyleSheet styleSheet;
        [SerializeField] private string dayFormat = "Jour {0} — {1} — {2}";
        [SerializeField] private string[] phaseNames = { "Matin", "Journée", "Soir", "Nuit" };

        private UIDocument _document;
        private Label _label;
        private GameClock _clock;

        public string Text => _label?.text;

        public void Init(GameClockRunner runner)
        {
            OnDisable();
            clockRunner = runner;
            OnEnable();
        }

        private void Awake()
        {
            _document = GetComponent<UIDocument>();
        }

        private void OnEnable()
        {
            if (clockRunner == null)
            {
                return;
            }

            _clock = clockRunner.Clock;
            _clock.DayStarted += OnDayStarted;
            _clock.PhaseChanged += OnPhaseChanged;
            _clock.TimeChanged += OnTimeChanged;
            Refresh();
        }

        private void OnDisable()
        {
            if (_clock != null)
            {
                _clock.DayStarted -= OnDayStarted;
                _clock.PhaseChanged -= OnPhaseChanged;
                _clock.TimeChanged -= OnTimeChanged;
                _clock = null;
            }

            _label?.RemoveFromHierarchy();
            _label = null;
        }

        private void Start()
        {
            Refresh();
        }

        private void OnDayStarted(int day)
        {
            Refresh();
        }

        private void OnPhaseChanged(DayPhase phase)
        {
            Refresh();
        }

        private void OnTimeChanged(int minutes)
        {
            Refresh();
        }

        // The UIDocument builds its root when enabled, which may come after this component: create the label lazily.
        private void Refresh()
        {
            if (_clock == null || !EnsureLabel())
            {
                return;
            }

            _label.text = ClockLabel.Format(dayFormat, phaseNames, _clock.Day, _clock.Phase, _clock.Minutes);
        }

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
    }
}
