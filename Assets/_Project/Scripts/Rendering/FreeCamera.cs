using Moonbrew.Core;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moonbrew.Rendering
{
    // Capture mode photo camera: detaches the Cinemachine camera from the player to pan and zoom freely,
    // then restores the normal follow. Debug builds only.
    [RequireComponent(typeof(CinemachineCamera))]
    public class FreeCamera : MonoBehaviour
    {
        [SerializeField] private CaptureConfig config;
        [SerializeField] private EventBusAsset eventBus;
        [SerializeField] private InputActionReference toggleAction;
        [SerializeField] private InputActionReference moveAction;
        [SerializeField] private InputActionReference zoomAction;

        private CinemachineCamera _camera;
        private Transform _followTarget;
        private float _normalZoom;

        public bool IsActive { get; private set; }

        public void Init(CaptureConfig captureConfig, EventBusAsset bus)
        {
            config = captureConfig;
            eventBus = bus;
        }

        public void SetActive(bool active)
        {
            if (active == IsActive)
            {
                return;
            }

            IsActive = active;
            if (active)
            {
                _followTarget = _camera.Follow;
                _normalZoom = _camera.Lens.OrthographicSize;
                _camera.Follow = null;
            }
            else
            {
                _camera.Follow = _followTarget;
                SetZoom(_normalZoom);
            }

            if (eventBus != null)
            {
                eventBus.Publish(new FreeCameraToggled(active));
            }
        }

        // Moves the detached camera; used by the move input and by tests.
        public void Pan(Vector2 input, float deltaTime)
        {
            if (!IsActive)
            {
                return;
            }

            var lens = _camera.Lens.OrthographicSize;
            transform.position += FreeCameraMotion.Pan(input, transform.eulerAngles.y, config.CameraPanSpeed, lens, _normalZoom, deltaTime);
        }

        public void Zoom(float input)
        {
            if (IsActive)
            {
                SetZoom(FreeCameraMotion.Zoom(_camera.Lens.OrthographicSize, input, config.CameraZoomStep, config.CameraMinZoom, config.CameraMaxZoom));
            }
        }

        private void Awake()
        {
            _camera = GetComponent<CinemachineCamera>();
            if (!Debug.isDebugBuild)
            {
                enabled = false;
            }
        }

        private void OnEnable()
        {
            Subscribe(toggleAction, OnToggle);
            Subscribe(zoomAction, OnZoom);
            if (moveAction != null)
            {
                moveAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            Unsubscribe(toggleAction, OnToggle);
            Unsubscribe(zoomAction, OnZoom);
            SetActive(false);
        }

        // Camera movement changes every frame, so it is one of the few things allowed in Update.
        private void Update()
        {
            if (IsActive && moveAction != null)
            {
                Pan(moveAction.action.ReadValue<Vector2>(), Time.unscaledDeltaTime);
            }
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            SetActive(!IsActive);
        }

        private void OnZoom(InputAction.CallbackContext context)
        {
            Zoom(context.ReadValue<float>());
        }

        private void SetZoom(float size)
        {
            var lens = _camera.Lens;
            lens.OrthographicSize = size;
            _camera.Lens = lens;
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
