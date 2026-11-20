using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.UIElements;

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

        // Layers drawn at screen resolution on top of the pixelated image (the bubbles), so their icons stay crisp.
        [SerializeField] private LayerMask sharpLayers;

        [Tooltip("The interface panel, drawn at the image's resolution so it is pixelated like the game.")]
        [SerializeField] private PanelSettings uiPanel;

        private Camera _camera;
        private Camera _outputCamera;
        private Canvas _outputCanvas;
        private RawImage _output;
        private Material _outputMaterial;
        private RenderTexture _texture;
        private Camera _sharpCamera;
        private RawImage _sharpOutput;
        private RenderTexture _sharpTexture;
        private RawImage _uiOutput;
        private RenderTexture _uiTexture;
        private PanelScaleMode _uiScaleMode;
        private float _uiScale;
        private bool _uiClearColor;
        private Vector2Int _screenSize;
        private int _scale = 1;
        private Vector2Int _renderSize;
        private bool _pixelMode;

        public bool IsPixelMode => enabled && _pixelMode;

        // Follows the window's shape once the output is laid out; the settings give the target until then.
        public Vector2Int RenderSize => _renderSize != Vector2Int.zero ? _renderSize : settings != null ? settings.Resolution : Vector2Int.zero;

        public Vector2Int TextureSize => RenderSize + Vector2Int.one * (MarginTexels * 2);

        // The texture of the sharp layers, or null when there are none or the image is not pixelated.
        public RenderTexture SharpTexture => _sharpTexture;

        // The material that draws the low-resolution image on screen, or null for the default one.
        public Material OutputMaterial => _outputMaterial;

        // Screen pixels per image pixel.
        public int Scale => _scale;

        // True while the interface panel is drawn into the low-resolution texture.
        public bool IsUiPixelated => _uiTexture != null && uiPanel != null && uiPanel.targetTexture == _uiTexture;

        // Where the interface texture is shown on screen, counted from the bottom-left corner.
        public RectInt UiViewport => _uiOutput == null
            ? default
            : new RectInt(
                Vector2Int.RoundToInt(_uiOutput.rectTransform.anchoredPosition),
                Vector2Int.RoundToInt(_uiOutput.rectTransform.sizeDelta));

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(PixelRenderSettings renderSettings, LayerMask sharp = default, PanelSettings panel = null)
        {
            settings = renderSettings;
            sharpLayers = sharp;
            uiPanel = panel;
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
            SetVisibleRect(VisibleRect(screenPixels / _scale));
        }

        // The sharp layers slide with the image, so they stay on the objects they belong to.
        private void SetVisibleRect(Rect rect)
        {
            _output.uvRect = rect;
            if (_sharpOutput != null)
            {
                _sharpOutput.uvRect = rect;
            }
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
            SetUiPixelated(false);
        }

        // Unity raises no event when the window is resized, so the size is compared each frame.
        private void LateUpdate()
        {
            if (_pixelMode && (Screen.width != _screenSize.x || Screen.height != _screenSize.y))
            {
                Apply();
            }

            // Follows the zoom of the free camera.
            if (_sharpCamera != null)
            {
                _sharpCamera.orthographicSize = _camera.orthographicSize;
            }
        }

        private void OnDestroy()
        {
            ReleaseTexture(ref _texture);
            ReleaseTexture(ref _sharpTexture);
            ReleaseTexture(ref _uiTexture);

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
                _camera.cullingMask |= sharpLayers;
                SetUiPixelated(false);
                return;
            }

            _screenSize = new Vector2Int(Screen.width, Screen.height);
            var layout = PixelScaler.Compute(_screenSize, settings.Resolution);
            _scale = layout.Scale;
            _renderSize = layout.RenderSize;

            EnsureTexture(ref _texture, TextureSize, _output, "PixelRender");
            _camera.targetTexture = _texture;
            SetLayout(_output, layout);

            if (_sharpCamera != null)
            {
                _camera.cullingMask &= ~sharpLayers;
                _sharpCamera.orthographic = _camera.orthographic;
                _sharpCamera.orthographicSize = _camera.orthographicSize;
                _sharpCamera.nearClipPlane = _camera.nearClipPlane;
                _sharpCamera.farClipPlane = _camera.farClipPlane;
                EnsureTexture(ref _sharpTexture, TextureSize * _scale, _sharpOutput, "PixelSharp");
                _sharpCamera.targetTexture = _sharpTexture;
                SetLayout(_sharpOutput, layout);
            }

            if (_uiOutput != null)
            {
                // No margin: the interface never slides with the camera.
                EnsureTexture(ref _uiTexture, RenderSize, _uiOutput, "PixelUi");
                SetLayout(_uiOutput, layout);
                SetUiPixelated(true);
            }

            SetVisibleRect(VisibleRect(Vector2.zero));
        }

        // The panel asset is shared with the editor: its settings are put back as soon as the interface is sharp again.
        private void SetUiPixelated(bool pixelated)
        {
            if (uiPanel == null || _uiOutput == null)
            {
                return;
            }

            if (pixelated)
            {
                // One interface pixel per image pixel, whatever the window size.
                uiPanel.scaleMode = PanelScaleMode.ConstantPixelSize;
                uiPanel.scale = 1f;
                uiPanel.clearColor = true;
                uiPanel.colorClearValue = Color.clear;
                uiPanel.targetTexture = _uiTexture;
                uiPanel.SetScreenToPanelSpaceFunction(ScreenToUiPanel);
                return;
            }

            uiPanel.targetTexture = null;
            uiPanel.scaleMode = _uiScaleMode;
            uiPanel.scale = _uiScale;
            uiPanel.clearColor = _uiClearColor;
            uiPanel.SetScreenToPanelSpaceFunction(null);
        }

        private Vector2 ScreenToUiPanel(Vector2 screenPoint)
        {
            var panelPoint = PixelScaler.ScreenToPanel(screenPoint, Screen.height, UiViewport, RenderSize);

            // NaN tells UI Toolkit the point is outside the panel: on the black bands, nothing reacts.
            return panelPoint ?? new Vector2(float.NaN, float.NaN);
        }

        private static void SetLayout(RawImage image, PixelLayout layout)
        {
            image.rectTransform.anchoredPosition = layout.Viewport.position;
            image.rectTransform.sizeDelta = layout.Viewport.size;
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

            _output = CreateImage("PixelOutput");
            _output.material = _outputMaterial;

            if (sharpLayers != 0)
            {
                CreateSharpOutput();
            }

            // Created last, so the interface is drawn over the image and the sharp layers.
            if (uiPanel != null)
            {
                _uiScaleMode = uiPanel.scaleMode;
                _uiScale = uiPanel.scale;
                _uiClearColor = uiPanel.clearColor;
                _uiOutput = CreateImage("PixelUiOutput");
            }
        }

        private void CreateSharpOutput()
        {
            // Same transform as the game camera, so the sharp layers line up with the pixelated image;
            // a transparent background lets the image show through.
            _sharpCamera = new GameObject("PixelSharpCamera").AddComponent<Camera>();
            _sharpCamera.transform.SetParent(transform, false);
            _sharpCamera.cullingMask = sharpLayers;
            _sharpCamera.clearFlags = CameraClearFlags.SolidColor;
            _sharpCamera.backgroundColor = Color.clear;
            _sharpCamera.depth = _camera.depth + 1;
            _sharpOutput = CreateImage("PixelSharpOutput");
        }

        private RawImage CreateImage(string imageName)
        {
            var image = new GameObject(imageName).AddComponent<RawImage>();
            image.transform.SetParent(_outputCanvas.transform, false);
            image.raycastTarget = false;
            image.rectTransform.anchorMin = Vector2.zero;
            image.rectTransform.anchorMax = Vector2.zero;
            image.rectTransform.pivot = Vector2.zero;
            return image;
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

            if (_sharpCamera != null)
            {
                _sharpCamera.gameObject.SetActive(active);
            }
        }

        private void EnsureTexture(ref RenderTexture texture, Vector2Int size, RawImage image, string textureName)
        {
            if (texture != null && texture.width == size.x && texture.height == size.y)
            {
                return;
            }

            ReleaseTexture(ref texture);
            texture = new RenderTexture(size.x, size.y, DepthBufferBits)
            {
                name = textureName,
                filterMode = FilterMode.Point,
            };
            texture.Create();
            image.texture = texture;
        }

        private void ReleaseTexture(ref RenderTexture texture)
        {
            if (texture == null)
            {
                return;
            }

            if (_camera != null && _camera.targetTexture == texture)
            {
                _camera.targetTexture = null;
            }

            if (_sharpCamera != null && _sharpCamera.targetTexture == texture)
            {
                _sharpCamera.targetTexture = null;
            }

            texture.Release();
            Destroy(texture);
            texture = null;
        }
    }
}
