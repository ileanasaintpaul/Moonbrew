using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace Moonbrew.Rendering
{
    [RequireComponent(typeof(Camera))]
    public class PixelRenderer : MonoBehaviour
    {
        private const int DepthBufferBits = 24;

        // Extra texels rendered on each side, so the visible area can slide by a sub-pixel offset
        // without moving the frame of the image against the black bands.
        private const int MarginTexels = 1;

        [SerializeField] private PixelRenderSettings settings;
        [SerializeField] private InputAction toggleMode = new InputAction("TogglePixelMode", InputActionType.Button, "<Keyboard>/f2");

        private Camera _camera;
        private Camera _outputCamera;
        private Canvas _outputCanvas;
        private RawImage _output;
        private Material _outputMaterial;
        private RenderTexture _texture;
        private Vector2Int _screenSize;
        private int _scale = 1;
        private Vector2Int _renderSize;
        private bool _pixelMode;

        public bool IsPixelMode => enabled && _pixelMode;

        // Follows the window's shape once the output is laid out; the settings give the target until then.
        public Vector2Int RenderSize => _renderSize != Vector2Int.zero ? _renderSize : settings != null ? settings.Resolution : Vector2Int.zero;

        public Vector2Int TextureSize => RenderSize + Vector2Int.one * (MarginTexels * 2);

        // The material that draws the low-resolution image on screen, or null for the default one.
        public Material OutputMaterial => _outputMaterial;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(PixelRenderSettings renderSettings)
        {
            settings = renderSettings;
        }

        // The camera ray under a screen point, whether the image is pixelated or sharp. Null on the black bands.
        public Ray? ScreenPointToRay(Vector2 screenPoint)
        {
            if (!IsPixelMode || _output == null)
            {
                return _camera.ScreenPointToRay(screenPoint);
            }

            var viewport = new RectInt(
                Vector2Int.RoundToInt(_output.rectTransform.anchoredPosition),
                Vector2Int.RoundToInt(_output.rectTransform.sizeDelta));
            var point = PixelScaler.ScreenToViewport(screenPoint, viewport, _output.uvRect);
            return point.HasValue ? _camera.ViewportPointToRay(point.Value) : (Ray?)null;
        }

        // Can be called before Awake: the material is applied once the output exists.
        public void SetOutputMaterial(Material material)
        {
            _outputMaterial = material;
            if (_output != null)
            {
                _output.material = material;
            }
        }

        // Slides the visible area of the texture by a fraction of a render pixel, so a camera snapped
        // to the pixel grid still appears to move smoothly while the image frame stays still.
        public void SetSubPixelOffset(Vector2 texels)
        {
            if (!IsPixelMode || _output == null)
            {
                return;
            }

            // Rounded to whole screen pixels: a fractional screen offset makes point sampling hesitate
            // between two pixels from one frame to the next.
            var screenPixels = new Vector2(Mathf.Round(texels.x * _scale), Mathf.Round(texels.y * _scale));
            _output.uvRect = VisibleRect(screenPixels / _scale);
        }

        private Rect VisibleRect(Vector2 offsetTexels)
        {
            var texture = TextureSize;
            var visible = RenderSize;
            return new Rect(
                (MarginTexels - offsetTexels.x) / texture.x,
                (MarginTexels - offsetTexels.y) / texture.y,
                (float)visible.x / texture.x,
                (float)visible.y / texture.y);
        }

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

            _screenSize = new Vector2Int(Screen.width, Screen.height);
            var layout = PixelScaler.Compute(_screenSize, settings.Resolution);
            _scale = layout.Scale;
            _renderSize = layout.RenderSize;

            EnsureTexture(TextureSize);
            _camera.targetTexture = _texture;
            _output.rectTransform.anchoredPosition = layout.Viewport.position;
            _output.rectTransform.sizeDelta = layout.Viewport.size;
            _output.uvRect = VisibleRect(Vector2.zero);
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
            _output.material = _outputMaterial;
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
