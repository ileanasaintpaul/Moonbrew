using UnityEngine;

namespace Moonbrew.Player
{
    // Turns the body gauges (-100 to 100) into the models' blend shape weights (0 to 100). The type and the musculature
    // have one blend shape each, half-way at 0; the size has one towards slim and one towards heavy.
    public static class BodyShapeWeights
    {
        public static float Type(int value) => (Clamp(value) + AppearanceCatalog.ShapeLimit) * 0.5f;

        public static float Muscle(int value) => (Clamp(value) + AppearanceCatalog.ShapeLimit) * 0.5f;

        public static float Heavy(int size) => Mathf.Max(0, Clamp(size));

        public static float Skinny(int size) => Mathf.Max(0, -Clamp(size));

        private static int Clamp(int value) => Mathf.Clamp(value, -AppearanceCatalog.ShapeLimit, AppearanceCatalog.ShapeLimit);
    }
}
