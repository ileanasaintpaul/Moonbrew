using UnityEngine;
using UnityEngine.InputSystem;

namespace Moonbrew.Ui
{
    // Turns the owner on the creation screen when the player drags across it, as on a skin viewer.
    // A drag counts only if it starts in the part of the screen where the owner stands, not on the panel.
    public class PreviewTurntable : MonoBehaviour
    {
        [Tooltip("The model turned round its vertical axis.")]
        [SerializeField] private Transform target;
        [Tooltip("Degrees turned per pixel dragged.")]
        [SerializeField] private float degreesPerPixel = 0.4f;
        [Tooltip("Where a drag may start, as a share of the screen from its bottom-left corner.")]
        [SerializeField] private Rect dragArea = new Rect(0.5f, 0f, 0.5f, 0.9f);
        [SerializeField] private InputAction press = new InputAction("Turn Press", InputActionType.Button, "<Pointer>/press");
        [SerializeField] private InputAction drag = new InputAction("Turn Drag", InputActionType.Value, "<Pointer>/delta");

        private bool _dragging;

        // For objects built from code.
        public void Init(Transform model)
        {
            target = model;
        }

        // The owner faces the camera: a drag to the right (positive pixels) carries its front to the right of the screen.
        public void Turn(float pixels)
        {
            target.Rotate(0f, -pixels * degreesPerPixel, 0f, Space.World);
        }

        private void Awake()
        {
            if (target == null)
            {
                Debug.LogError($"{nameof(PreviewTurntable)} on {name} needs the model to turn.", this);
                enabled = false;
            }
        }

        private void OnEnable()
        {
            press.started += OnPressStarted;
            press.canceled += OnPressEnded;
            drag.performed += OnDrag;
            press.Enable();
            drag.Enable();
        }

        private void OnDisable()
        {
            press.started -= OnPressStarted;
            press.canceled -= OnPressEnded;
            drag.performed -= OnDrag;
            press.Disable();
            drag.Disable();
            _dragging = false;
        }

        private void OnPressStarted(InputAction.CallbackContext context)
        {
            var pointer = Pointer.current;
            if (pointer == null)
            {
                return;
            }

            var position = pointer.position.ReadValue();
            _dragging = dragArea.Contains(new Vector2(position.x / Screen.width, position.y / Screen.height));
        }

        private void OnPressEnded(InputAction.CallbackContext context)
        {
            _dragging = false;
        }

        private void OnDrag(InputAction.CallbackContext context)
        {
            if (_dragging)
            {
                Turn(context.ReadValue<Vector2>().x);
            }
        }
    }
}
