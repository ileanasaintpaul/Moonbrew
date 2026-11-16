using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Moonbrew.Rendering
{
    [RequireComponent(typeof(Camera))]
    public class PixelRenderer : MonoBehaviour
    {
        private const int DepthBufferBits = 24;

        [SerializeField] private PixelRenderSettings settings;
        [SerializeField] private InputAction toggleMode = new InputAction("TogglePixelMode", InputActionType.Button, "<Keyboard>/f2");

        private Camera _camera;
        private Camera _outputCamera;
        private Canvas _outputCanvas;
        private RawImage _output;
        private RenderTexture _texture;
        private Vector2Int _screenSize;
        private bool _pixelMode;

        private void Awake()
        {
            _camera = GetComponent<Camera>();

            if (settings == null)
            {
                Debug.LogError($"{nameof(PixelRenderer)} on {name} has no {nameof(PixelRenderSettings)} assigned.", this);
                enabled = false;
                return;
            }

            _pixelMode = settings.StartInPixelMode;
            CreateOutput();
        }

        private void OnEnable()
        {
            if (_output == null)
            {
                return;
            }

            toggleMode.performed += OnToggleMode;
            toggleMode.Enable();
            Apply();
        }

        private void OnDisable()
        {
            toggleMode.performed -= OnToggleMode;
            toggleMode.Disable();

            if (_camera != null)
            {
                _camera.targetTexture = null;
            }

            SetOutputActive(false);
        }

        // Unity raises no event when the window is resized, so the size is compared each frame.
        private void LateUpdate()
        {
            if (_pixelMode && (Screen.width != _screenSize.x || Screen.height != _screenSize.y))
            {
                Apply();
            }
        }

        private void OnDestroy()
        {
            ReleaseTexture();

            if (_outputCamera != null)
            {
                Destroy(_outputCamera.gameObject);
            }

            if (_outputCanvas != null)
            {
                Destroy(_outputCanvas.gameObject);
            }
        }

        private void OnToggleMode(InputAction.CallbackContext context)
        {
            _pixelMode = !_pixelMode;
            Apply();
        }

        private void Apply()
        {
            SetOutputActive(_pixelMode);

            if (!_pixelMode)
            {
                _camera.targetTexture = null;
                return;
            }

            EnsureTexture(settings.Resolution);
            _camera.targetTexture = _texture;

            _screenSize = new Vector2Int(Screen.width, Screen.height);
            var viewport = PixelScaler.Compute(_screenSize, settings.Resolution).Viewport;
            _output.rectTransform.anchoredPosition = viewport.position;
            _output.rectTransform.sizeDelta = viewport.size;
        }

        // The game camera renders into the texture; this camera only clears the screen to black
        // around the scaled image, and an overlay canvas draws the texture on top.
        private void CreateOutput()
        {
            _outputCamera = new GameObject("PixelOutputCamera").AddComponent<Camera>();
            _outputCamera.transform.SetParent(transform, false);
            _outputCamera.cullingMask = 0;
            _outputCamera.clearFlags = CameraClearFlags.SolidColor;
            _outputCamera.backgroundColor = Color.black;
            _outputCamera.depth = _camera.depth - 1;

            _outputCanvas = new GameObject("PixelOutputCanvas").AddComponent<Canvas>();
            _outputCanvas.transform.SetParent(transform, false);
            _outputCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _outputCanvas.sortingOrder = short.MinValue;

            _output = new GameObject("PixelOutput").AddComponent<RawImage>();
            _output.transform.SetParent(_outputCanvas.transform, false);
            _output.raycastTarget = false;
            _output.rectTransform.anchorMin = Vector2.zero;
            _output.rectTransform.anchorMax = Vector2.zero;
            _output.rectTransform.pivot = Vector2.zero;
        }

        private void SetOutputActive(bool active)
        {
            if (_outputCamera != null)
            {
                _outputCamera.gameObject.SetActive(active);
            }

            if (_outputCanvas != null)
            {
                _outputCanvas.gameObject.SetActive(active);
            }
        }

        private void EnsureTexture(Vector2Int size)
        {
            if (_texture != null && _texture.width == size.x && _texture.height == size.y)
            {
                return;
            }

            ReleaseTexture();
            _texture = new RenderTexture(size.x, size.y, DepthBufferBits)
            {
                name = "PixelRender",
                filterMode = FilterMode.Point,
            };
            _texture.Create();
            _output.texture = _texture;
        }

        private void ReleaseTexture()
        {
            if (_texture == null)
            {
                return;
            }

            if (_camera != null && _camera.targetTexture == _texture)
            {
                _camera.targetTexture = null;
            }

            _texture.Release();
            Destroy(_texture);
            _texture = null;
        }
    }
}
