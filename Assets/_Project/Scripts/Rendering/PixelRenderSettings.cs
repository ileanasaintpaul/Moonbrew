using UnityEngine;

namespace Moonbrew.Rendering
{
    [CreateAssetMenu(menuName = "Moonbrew/Rendering/Pixel Render Settings", fileName = "SO_PixelRenderSettings")]
    public class PixelRenderSettings : ScriptableObject
    {
        [SerializeField, Min(1)] private int width = 640;
        [SerializeField, Min(1)] private int height = 360;
        [SerializeField] private bool startInPixelMode = true;

        public Vector2Int Resolution => new Vector2Int(width, height);

        public bool StartInPixelMode => startInPixelMode;
    }
}
