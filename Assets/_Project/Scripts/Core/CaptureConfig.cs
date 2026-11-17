using UnityEngine;

namespace Moonbrew.Core
{
    // Settings of the capture mode used to film the devlog (debug builds only).
    [CreateAssetMenu(menuName = "Moonbrew/Core/Capture Config", fileName = "SO_CaptureConfig")]
    public class CaptureConfig : ScriptableObject
    {
        [SerializeField, Min(1)] private int advanceMinutes = 60;
        [SerializeField, Min(0f)] private float cameraPanSpeed = 8f;
        [SerializeField, Min(0.1f)] private float cameraZoomStep = 0.5f;
        [SerializeField, Min(0.1f)] private float cameraMinZoom = 3f;
        [SerializeField, Min(0.1f)] private float cameraMaxZoom = 20f;

        // In-game minutes added by the "advance" shortcut.
        public int AdvanceMinutes => advanceMinutes;

        // World units per second at the reference zoom; panning scales with the zoom so it feels the same.
        public float CameraPanSpeed => cameraPanSpeed;

        // Orthographic size change per zoom input step.
        public float CameraZoomStep => cameraZoomStep;

        public float CameraMinZoom => cameraMinZoom;

        public float CameraMaxZoom => cameraMaxZoom;
    }
}
