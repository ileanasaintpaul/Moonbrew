using UnityEngine;

namespace Moonbrew.Ui
{
    // The creation screen's camera glides between a view of the whole owner and a close view of the face.
    public class CreationCamera : MonoBehaviour
    {
        [Tooltip("Where the camera stands to show the whole owner.")]
        [SerializeField] private Transform bodyView;
        [Tooltip("Where the camera stands to show the face.")]
        [SerializeField] private Transform faceView;
        [Tooltip("Time to reach most of the way to the new view, in seconds.")]
        [SerializeField, Min(0.01f)] private float smoothTime = 0.25f;
        [Tooltip("Narrowest width over height that shows the whole layout at the camera's own field of view: a narrower screen widens the view.")]
        [SerializeField, Min(0.1f)] private float designAspect = 2.04f;

        private Transform _target;
        private Vector3 _velocity;
        private Camera _camera;
        private float _baseFieldOfView;

        public bool OnFace => _target == faceView;

        // The vertical field of view that keeps the design's horizontal view on a narrower screen.
        public static float FieldOfViewFor(float fieldOfView, float aspect, float design)
        {
            if (aspect <= 0f || aspect >= design)
            {
                return fieldOfView;
            }

            return 2f * Mathf.Atan(Mathf.Tan(fieldOfView * 0.5f * Mathf.Deg2Rad) * design / aspect) * Mathf.Rad2Deg;
        }

        // For objects built from code.
        public void Init(Transform body, Transform face)
        {
            bodyView = body;
            faceView = face;
        }

        public void ShowFace(bool face)
        {
            _target = face ? faceView : bodyView;
        }

        private void Awake()
        {
            if (bodyView == null || faceView == null)
            {
                Debug.LogError($"{nameof(CreationCamera)} on {name} needs the body and face views.", this);
                enabled = false;
                return;
            }

            _target = bodyView;
            transform.SetPositionAndRotation(bodyView.position, bodyView.rotation);
            _camera = GetComponent<Camera>();
            _baseFieldOfView = _camera != null ? _camera.fieldOfView : 0f;
        }

        // Camera movement: the only per-frame work, in unscaled time since the menu runs whatever the game clock does.
        private void LateUpdate()
        {
            transform.position = Vector3.SmoothDamp(transform.position, _target.position, ref _velocity, smoothTime, Mathf.Infinity, Time.unscaledDeltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, _target.rotation, 1f - Mathf.Exp(-Time.unscaledDeltaTime / smoothTime));
            if (_camera != null)
            {
                _camera.fieldOfView = FieldOfViewFor(_baseFieldOfView, _camera.aspect, designAspect);
            }
        }
    }
}
