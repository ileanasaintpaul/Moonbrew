using Moonbrew.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moonbrew.Player
{
    // Wires input, the CharacterController and the animator to PlayerMotor.
    [RequireComponent(typeof(CharacterController))]
    public class PlayerController : MonoBehaviour
    {
        // Animator "Gait": 0 idle, 1 walk, 2 run.
        private const float IdleGait = 0f;
        private const float WalkGait = 1f;
        private const float RunGait = 2f;

        private static readonly int GaitHash = Animator.StringToHash("Gait");

        [SerializeField] private PlayerConfig settings;
        [SerializeField] private Transform cameraTransform;
        [SerializeField] private Animator animator;
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference sprintAction;
        [SerializeField] private EventBusAsset eventBus;

        private CharacterController _controller;
        private Vector2 _moveInput;
        private bool _running;
        private bool _inputSuspended;
        private float _verticalSpeed;

        public Vector3 Velocity { get; private set; }

        public void Init(PlayerConfig playerConfig, Transform playerCamera, EventBusAsset bus = null)
        {
            if (eventBus != null && isActiveAndEnabled)
            {
                eventBus.Unsubscribe<FreeCameraToggled>(OnFreeCameraToggled);
            }

            settings = playerConfig;
            cameraTransform = playerCamera;
            eventBus = bus;
            if (eventBus != null && isActiveAndEnabled)
            {
                eventBus.Subscribe<FreeCameraToggled>(OnFreeCameraToggled);
            }
        }

        public void SetMoveInput(Vector2 input)
        {
            _moveInput = input;
        }

        public void SetRunning(bool running)
        {
            _running = running;
        }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
        }

        private void OnEnable()
        {
            Subscribe(moveAction, OnMove, OnMove);
            Subscribe(sprintAction, OnSprint, OnSprint);
            if (eventBus != null)
            {
                eventBus.Subscribe<FreeCameraToggled>(OnFreeCameraToggled);
            }
        }

        private void OnDisable()
        {
            Unsubscribe(moveAction, OnMove, OnMove);
            Unsubscribe(sprintAction, OnSprint, OnSprint);
            if (eventBus != null)
            {
                eventBus.Unsubscribe<FreeCameraToggled>(OnFreeCameraToggled);
            }
        }

        // Movement changes every frame, so it is one of the few things allowed in Update.
        private void Update()
        {
            var deltaTime = Time.deltaTime;
            var cameraYaw = cameraTransform != null ? cameraTransform.eulerAngles.y : 0f;
            var speed = _running ? settings.RunSpeed : settings.WalkSpeed;
            var input = _inputSuspended ? Vector2.zero : _moveInput;
            var planar = PlayerMotor.PlanarVelocity(input, cameraYaw, speed);

            _verticalSpeed = _controller.isGrounded ? settings.Gravity * deltaTime : _verticalSpeed + settings.Gravity * deltaTime;
            _controller.Move((planar + Vector3.up * _verticalSpeed) * deltaTime);
            Velocity = planar;

            var yaw = PlayerMotor.Turn(transform.eulerAngles.y, planar, settings.TurnSpeed * deltaTime);
            transform.rotation = Quaternion.Euler(0f, yaw, 0f);

            if (animator != null)
            {
                var gait = planar.sqrMagnitude < 1e-6f ? IdleGait : _running ? RunGait : WalkGait;
                animator.SetFloat(GaitHash, gait, settings.AnimationDamping, deltaTime);
            }
        }

        // The free camera uses the same keys to pan, so the player stands still while it is on.
        private void OnFreeCameraToggled(FreeCameraToggled message)
        {
            _inputSuspended = message.Active;
        }

        private void OnMove(InputAction.CallbackContext context)
        {
            _moveInput = context.ReadValue<Vector2>();
        }

        private void OnSprint(InputAction.CallbackContext context)
        {
            _running = context.ReadValueAsButton();
        }

        private static void Subscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> performed, System.Action<InputAction.CallbackContext> canceled)
        {
            if (reference == null)
            {
                return;
            }

            reference.action.performed += performed;
            reference.action.canceled += canceled;
            reference.action.Enable();
        }

        private static void Unsubscribe(InputActionReference reference, System.Action<InputAction.CallbackContext> performed, System.Action<InputAction.CallbackContext> canceled)
        {
            if (reference == null)
            {
                return;
            }

            reference.action.performed -= performed;
            reference.action.canceled -= canceled;
        }
    }
}
