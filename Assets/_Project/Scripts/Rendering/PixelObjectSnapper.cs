using UnityEngine;

namespace Moonbrew.Rendering
{
    // Keeps a moving object's model on the camera's texel grid so it moves by whole pixels instead of
    // being rasterized differently every frame. Only the visual child moves; the root keeps its exact position.
    public class PixelObjectSnapper : MonoBehaviour
    {
        [SerializeField] private PixelCameraSnapper cameraSnapper;
        [SerializeField] private Transform visual;

        private Vector3 _restLocalPosition;
        private bool _subscribed;

        public void Init(PixelCameraSnapper snapper, Transform visualRoot)
        {
            Unsubscribe();
            cameraSnapper = snapper;
            visual = visualRoot;
            _restLocalPosition = visual.localPosition;
            Subscribe();
        }

        private void Awake()
        {
            if (visual != null)
            {
                _restLocalPosition = visual.localPosition;
            }
        }

        private void OnEnable()
        {
            Subscribe();
        }

        private void OnDisable()
        {
            Unsubscribe();
            if (visual != null)
            {
                visual.localPosition = _restLocalPosition;
            }
        }

        private void OnCameraSnapped(Quaternion cameraRotation, float texelSize)
        {
            if (texelSize <= 0f)
            {
                visual.localPosition = _restLocalPosition;
                return;
            }

            var anchor = transform.TransformPoint(_restLocalPosition);
            visual.position = PixelSnap.Snap(anchor, cameraRotation, texelSize).Position;
        }

        private void Subscribe()
        {
            if (!_subscribed && cameraSnapper != null && visual != null && isActiveAndEnabled)
            {
                cameraSnapper.CameraSnapped += OnCameraSnapped;
                _subscribed = true;
            }
        }

        private void Unsubscribe()
        {
            if (_subscribed && cameraSnapper != null)
            {
                cameraSnapper.CameraSnapped -= OnCameraSnapped;
            }

            _subscribed = false;
        }
    }
}
