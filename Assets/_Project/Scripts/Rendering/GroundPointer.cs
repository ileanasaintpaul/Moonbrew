using Moonbrew.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace Moonbrew.Rendering
{
    // Turns a click into the ground point under the mouse, through the pixel render, and announces it
    // as GroundClicked. Clicks on a dialog (any UI element that catches the mouse) are left to the UI.
    [RequireComponent(typeof(PixelRenderer))]
    public class GroundPointer : MonoBehaviour
    {
        private const float MaxClickDistance = 500f;

        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private InputActionReference useAction;
        [Tooltip("The ground is the horizontal plane at this height.")]
        [SerializeField] private float groundHeight;
        [SerializeField] private UIDocument[] blockingDocuments = new UIDocument[0];

        private PixelRenderer _renderer;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(EventBusAsset bus)
        {
            eventBus = bus;
        }

        // The ground point under a screen point, or null on the black bands or towards the sky.
        public Vector3? GroundPoint(Vector2 screenPoint)
        {
            var ray = _renderer.ScreenPointToRay(screenPoint);
            if (!ray.HasValue)
            {
                return null;
            }

            var ground = new Plane(Vector3.up, new Vector3(0f, groundHeight, 0f));
            return ground.Raycast(ray.Value, out var distance) ? ray.Value.GetPoint(distance) : (Vector3?)null;
        }

        // Also called by tests, which have no mouse.
        public bool Click(Vector2 screenPoint)
        {
            if (OverUi(screenPoint) || eventBus == null)
            {
                return false;
            }

            // A clickable object under the mouse takes the click, so nothing happens on the ground behind it.
            var ray = _renderer.ScreenPointToRay(screenPoint);
            if (ray.HasValue
                && Physics.Raycast(ray.Value, out var hit, MaxClickDistance, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Collide)
                && hit.collider.GetComponentInParent<ClickTarget>() is ClickTarget target)
            {
                eventBus.Publish(new TargetClicked(target));
                return true;
            }

            var point = GroundPoint(screenPoint);
            if (!point.HasValue || eventBus == null)
            {
                return false;
            }

            eventBus.Publish(new GroundClicked(point.Value));
            return true;
        }

        private void Awake()
        {
            _renderer = GetComponent<PixelRenderer>();
        }

        private void OnEnable()
        {
            if (useAction != null)
            {
                useAction.action.performed += OnUse;
                useAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (useAction != null)
            {
                useAction.action.performed -= OnUse;
            }
        }

        private void OnUse(InputAction.CallbackContext context)
        {
            if (Pointer.current != null)
            {
                Click(Pointer.current.position.ReadValue());
            }
        }

        private bool OverUi(Vector2 screenPoint)
        {
            foreach (var document in blockingDocuments)
            {
                var panel = document != null && document.rootVisualElement != null ? document.rootVisualElement.panel : null;
                if (panel == null)
                {
                    continue;
                }

                // UI Toolkit counts y from the top of the screen.
                var panelPoint = RuntimePanelUtils.ScreenToPanel(panel, new Vector2(screenPoint.x, Screen.height - screenPoint.y));
                var picked = panel.Pick(panelPoint);
                if (picked != null && picked != document.rootVisualElement && picked != panel.visualTree)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
