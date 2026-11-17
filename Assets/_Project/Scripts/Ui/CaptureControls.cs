using Moonbrew.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Capture mode shortcuts for filming the devlog: hide the HUD and control the clock. Debug builds only.
    public class CaptureControls : MonoBehaviour
    {
        [SerializeField] private CaptureConfig config;
        [SerializeField] private GameClockRunner clockRunner;
        [SerializeField] private UIDocument[] hudDocuments;
        [SerializeField] private InputActionReference toggleHudAction;
        [SerializeField] private InputActionReference pauseClockAction;
        [SerializeField] private InputActionReference advanceHourAction;
        [SerializeField] private InputActionReference nextPhaseAction;

        public bool HudVisible { get; private set; } = true;

        public void Init(CaptureConfig captureConfig, GameClockRunner runner, UIDocument[] documents)
        {
            config = captureConfig;
            clockRunner = runner;
            hudDocuments = documents;
        }

        public void SetHudVisible(bool visible)
        {
            HudVisible = visible;
            foreach (var document in hudDocuments)
            {
                if (document != null && document.rootVisualElement != null)
                {
                    document.rootVisualElement.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void Awake()
        {
            if (!Debug.isDebugBuild)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Subscribe(toggleHudAction, OnToggleHud);
            Subscribe(pauseClockAction, OnPauseClock);
            Subscribe(advanceHourAction, OnAdvanceHour);
            Subscribe(nextPhaseAction, OnNextPhase);
        }

        private void OnDisable()
        {
            Unsubscribe(toggleHudAction, OnToggleHud);
            Unsubscribe(pauseClockAction, OnPauseClock);
            Unsubscribe(advanceHourAction, OnAdvanceHour);
            Unsubscribe(nextPhaseAction, OnNextPhase);
            SetHudVisible(true);
        }

        private void OnToggleHud(InputAction.CallbackContext context)
        {
            SetHudVisible(!HudVisible);
        }

        private void OnPauseClock(InputAction.CallbackContext context)
        {
            clockRunner.Clock.SetPaused(!clockRunner.Clock.IsPaused);
            Debug.Log($"[Capture] Clock {(clockRunner.Clock.IsPaused ? "paused" : "running")}");
        }

        private void OnAdvanceHour(InputAction.CallbackContext context)
        {
            clockRunner.Clock.AdvanceMinutes(config.AdvanceMinutes);
        }

        private void OnNextPhase(InputAction.CallbackContext context)
        {
            clockRunner.Clock.SkipToNextPhase();
        }

        private static void Subscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference != null)
            {
                reference.action.performed += handler;
                reference.action.Enable();
            }
        }

        private static void Unsubscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> handler)
        {
            if (reference != null)
            {
                reference.action.performed -= handler;
            }
        }
    }
}
