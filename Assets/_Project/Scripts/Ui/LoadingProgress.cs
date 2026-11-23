using UnityEngine;

namespace Moonbrew.Ui
{
    // When the loading screen fades: it stays opaque until the scene is ready and it has been seen long enough.
    public class LoadingProgress
    {
        private readonly float _minSeconds;
        private readonly float _fadeSeconds;
        private float _elapsed;
        private float _fading;
        private bool _ready;

        public LoadingProgress(float minSeconds, float fadeSeconds)
        {
            _minSeconds = Mathf.Max(0f, minSeconds);
            _fadeSeconds = Mathf.Max(0f, fadeSeconds);
        }

        public float Alpha { get; private set; } = 1f;

        public bool IsDone { get; private set; }

        public void SceneReady()
        {
            _ready = true;
        }

        public void Tick(float seconds)
        {
            if (seconds <= 0f || IsDone)
            {
                return;
            }

            _elapsed += seconds;
            if (!_ready || _elapsed < _minSeconds)
            {
                return;
            }

            _fading += seconds;
            Alpha = _fadeSeconds > 0f ? Mathf.Clamp01(1f - _fading / _fadeSeconds) : 0f;
            IsDone = Alpha <= 0f;
        }

        // Each sparkle pulses between dim and bright, out of step with the others.
        public static float Twinkle(float time, int index, float speed)
        {
            const float PhaseStep = 1.7f;
            return 0.5f + 0.5f * Mathf.Sin(time * speed + index * PhaseStep);
        }
    }
}
