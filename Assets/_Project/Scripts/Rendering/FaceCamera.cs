using UnityEngine;

namespace Moonbrew.Rendering
{
    // Turns a flat sprite in the world (a bubble) to face the scene camera when it appears. The farm and
    // the café cameras are not turned the same way, so a fixed angle in the prefab looks crooked in one of them.
    public class FaceCamera : MonoBehaviour
    {
        private void OnEnable()
        {
            var view = Camera.main;
            if (view != null)
            {
                transform.rotation = view.transform.rotation;
            }
        }
    }
}
