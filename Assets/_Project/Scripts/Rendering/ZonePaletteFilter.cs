using UnityEngine;
using UnityEngine.InputSystem;

namespace Moonbrew.Rendering
{
    // Reduces the low-resolution image to the zone palette before it is scaled up.
    // The sharp rendering mode (F2) keeps the original colors, for comparison.
    [RequireComponent(typeof(PixelRenderer))]
    public class ZonePaletteFilter : MonoBehaviour
    {
        private static readonly int PaletteColorsId = Shader.PropertyToID("_PaletteColors");
        private static readonly int PaletteCountId = Shader.PropertyToID("_PaletteCount");
        private static readonly int PaletteStrengthId = Shader.PropertyToID("_PaletteStrength");

        [SerializeField] private ZonePalette palette;
        [SerializeField] private Material filterMaterial;
        [SerializeField] private bool startActive = true;
        [SerializeField] private InputActionReference toggleAction;

        private readonly Vector4[] _colors = new Vector4[ZonePalette.MaxColors];
        private PixelRenderer _renderer;
        private Material _material;

        public bool IsActive { get; private set; }

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(ZonePalette zonePalette, Material material)
        {
            palette = zonePalette;
            filterMaterial = material;
        }

        public void SetActive(bool active)
        {
            IsActive = active && _material != null;
            _renderer.SetOutputMaterial(IsActive ? _material : null);
        }

        private void Awake()
        {
            _renderer = GetComponent<PixelRenderer>();

            if (palette == null || filterMaterial == null)
            {
                Debug.LogError($"{nameof(ZonePaletteFilter)} on {name} needs a palette and the filter material.", this);
                return;
            }

            // An instance, so each scene's palette never writes into the shared material asset.
            _material = new Material(filterMaterial);
            var count = ZonePalette.ToLinear(palette.Colors, _colors);
            _material.SetVectorArray(PaletteColorsId, _colors);
            _material.SetInt(PaletteCountId, count);
            _material.SetFloat(PaletteStrengthId, palette.Strength);
            SetActive(startActive);
        }

        private void OnEnable()
        {
            // The toggle is a capture and tuning shortcut, absent from release builds.
            if (toggleAction != null && Debug.isDebugBuild)
            {
                toggleAction.action.performed += OnToggle;
                toggleAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (toggleAction != null)
            {
                toggleAction.action.performed -= OnToggle;
            }
        }

        private void OnDestroy()
        {
            if (_material != null)
            {
                Destroy(_material);
            }
        }

        private void OnToggle(InputAction.CallbackContext context)
        {
            SetActive(!IsActive);
        }
    }
}
