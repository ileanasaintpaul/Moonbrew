using UnityEngine;

namespace Moonbrew.Core
{
    public static class ModelBounds
    {
        // Height of the highest point drawn by a model, or the fallback when it draws nothing.
        // Used to set things (a bubble...) just above models of any size.
        public static float Top(GameObject model, float fallback)
        {
            if (model == null)
            {
                return fallback;
            }

            var top = float.NegativeInfinity;
            foreach (var modelRenderer in model.GetComponentsInChildren<Renderer>())
            {
                top = Mathf.Max(top, modelRenderer.bounds.max.y);
            }

            return float.IsNegativeInfinity(top) ? fallback : top;
        }
    }
}
