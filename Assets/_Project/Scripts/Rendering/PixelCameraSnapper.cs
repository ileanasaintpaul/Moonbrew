using Unity.Cinemachine;
using UnityEngine;

namespace Moonbrew.Rendering
{
    [RequireComponent(typeof(Camera), typeof(PixelRenderer))]
    public class PixelCameraSnapper : MonoBehaviour
    {
        [SerializeField] private bool snapEnabled = true;

        private Camera _camera;
        private PixelRenderer _renderer;

        public bool SnapEnabled
        {
            get => snapEnabled;
            set => snapEnabled = value;
        }

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            _renderer = GetComponent<PixelRenderer>();
        }

        private void OnEnable()
        {
            CinemachineCore.CameraUpdatedEvent.AddListener(OnCameraUpdated);
        }

        private void OnDisable()
        {
            CinemachineCore.CameraUpdatedEvent.RemoveListener(OnCameraUpdated);
            _renderer.SetSubPixelOffset(Vector2.zero);
        }

        // Runs after the brain has placed the camera for this frame, before rendering. The brain
        // rewrites the transform every frame, so snapping never accumulates.
        private void OnCameraUpdated(CinemachineBrain brain)
        {
            if (brain.OutputCamera != _camera)
            {
                return;
            }

            if (!snapEnabled || !_camera.orthographic || !_renderer.IsPixelMode)
            {
                _renderer.SetSubPixelOffset(Vector2.zero);
                return;
            }

            var texelSize = PixelSnap.TexelSize(_camera.orthographicSize, _renderer.TextureSize.y);
            var result = PixelSnap.Snap(transform.position, transform.rotation, texelSize);
            transform.position = result.Position;
            _renderer.SetSubPixelOffset(-result.Remainder);
        }
    }
}
