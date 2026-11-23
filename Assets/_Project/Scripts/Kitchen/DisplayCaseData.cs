using UnityEngine;

namespace Moonbrew.Kitchen
{
    [CreateAssetMenu(menuName = "Moonbrew/Kitchen/Display Case", fileName = "SO_DisplayCase")]
    public class DisplayCaseData : ScriptableObject
    {
        [Tooltip("How close to the display case the owner must stand to open it, in meters.")]
        [SerializeField, Min(0.1f)] private float reach = 2f;

        public float Reach => reach;
    }
}
