using UnityEngine;

namespace Moonbrew.Ui
{
    [CreateAssetMenu(menuName = "Moonbrew/Ui/Loading Screen", fileName = "SO_LoadingScreen")]
    public class LoadingScreenData : ScriptableObject
    {
        [SerializeField] private Sprite circle;
        [SerializeField] private Sprite glow;
        [SerializeField] private Sprite sparkle;
        [Tooltip("Real seconds the screen stays up at least, so it never just flashes.")]
        [SerializeField, Min(0f)] private float minSeconds = 1.2f;
        [SerializeField, Min(0f)] private float fadeSeconds = 0.5f;
        [SerializeField] private float circleDegreesPerSecond = 35f;
        [SerializeField, Min(0)] private int sparkles = 10;
        [Tooltip("Distance of the sparkles from the centre of the circle, in pixels of the 640x360 UI.")]
        [SerializeField, Min(0f)] private float sparkleRadius = 78f;
        [SerializeField, Min(0f)] private float twinkleSpeed = 3f;

        public Sprite Circle => circle;

        public Sprite Glow => glow;

        public Sprite Sparkle => sparkle;

        public float MinSeconds => minSeconds;

        public float FadeSeconds => fadeSeconds;

        public float CircleDegreesPerSecond => circleDegreesPerSecond;

        public int Sparkles => sparkles;

        public float SparkleRadius => sparkleRadius;

        public float TwinkleSpeed => twinkleSpeed;

        // For data built from code.
        public void Init(float minimum, float fade, int sparkleCount)
        {
            minSeconds = minimum;
            fadeSeconds = fade;
            sparkles = sparkleCount;
        }
    }
}
