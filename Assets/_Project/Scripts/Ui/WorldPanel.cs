using UnityEngine;
using UnityEngine.UIElements;

namespace Moonbrew.Ui
{
    // Shows an interface panel on a quad in the scene, so the camera draws it in perspective (deeper on one side),
    // and points the mouse at it: a screen point is cast onto the quad and read as a point of the panel.
    [RequireComponent(typeof(MeshRenderer))]
    public class WorldPanel : MonoBehaviour
    {
        [Tooltip("The document drawn on the quad. Its panel settings must be its own: they get a target texture.")]
        [SerializeField] private UIDocument document;
        [SerializeField] private Camera viewCamera;
        [Tooltip("Size of the panel's texture, in pixels: the panel is laid out at this size.")]
        [SerializeField] private Vector2Int size = new Vector2Int(640, 600);
        [Tooltip("Point keeps pixel art sharp when the quad is larger than the texture.")]
        [SerializeField] private FilterMode filterMode = FilterMode.Bilinear;

        private static readonly int BaseMapId = Shader.PropertyToID("_BaseMap");

        private RenderTexture _texture;
        private Material _material;

        // For objects built from code: call on an inactive GameObject, before it is activated.
        public void Init(UIDocument panelDocument, Camera panelCamera, Vector2Int panelSize)
        {
            document = panelDocument;
            viewCamera = panelCamera;
            size = panelSize;
        }

        private void Awake()
        {
            if (document == null || document.panelSettings == null || viewCamera == null)
            {
                Debug.LogError($"{nameof(WorldPanel)} on {name} needs the document, with its own panel settings, and the camera.", this);
                enabled = false;
                return;
            }

            _texture = new RenderTexture(size.x, size.y, 24, RenderTextureFormat.ARGB32) { name = "World panel", filterMode = filterMode };
            _texture.Create();
            var settings = document.panelSettings;
            settings.targetTexture = _texture;
            settings.clearColor = true;
            settings.colorClearValue = Color.clear;
            settings.SetScreenToPanelSpaceFunction(ScreenToPanel);
            var surface = GetComponent<MeshRenderer>();
            _material = new Material(surface.sharedMaterial);
            _material.SetTexture(BaseMapId, _texture);
            surface.sharedMaterial = _material;
        }

        private void OnDestroy()
        {
            if (document != null && document.panelSettings != null)
            {
                document.panelSettings.targetTexture = null;
                document.panelSettings.SetScreenToPanelSpaceFunction(null);
            }

            if (_texture != null)
            {
                _texture.Release();
                Destroy(_texture);
            }

            Destroy(_material);
        }

        // UI Toolkit gives the point from the top of the screen; the quad spans -0.5 to 0.5 in its own space.
        // NaN means outside the panel: nothing on it reacts.
        private Vector2 ScreenToPanel(Vector2 screenPoint)
        {
            var outside = new Vector2(float.NaN, float.NaN);
            var ray = viewCamera.ScreenPointToRay(new Vector2(screenPoint.x, Screen.height - screenPoint.y));
            var plane = new Plane(transform.forward, transform.position);
            if (!plane.Raycast(ray, out var distance))
            {
                return outside;
            }

            var local = transform.InverseTransformPoint(ray.GetPoint(distance));
            if (Mathf.Abs(local.x) > 0.5f || Mathf.Abs(local.y) > 0.5f)
            {
                return outside;
            }

            return new Vector2((local.x + 0.5f) * size.x, (0.5f - local.y) * size.y);
        }
    }
}
