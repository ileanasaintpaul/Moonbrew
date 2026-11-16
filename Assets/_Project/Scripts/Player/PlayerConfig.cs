using UnityEngine;

namespace Moonbrew.Player
{
    [CreateAssetMenu(menuName = "Moonbrew/Player/Player Config", fileName = "SO_PlayerConfig")]
    public class PlayerConfig : ScriptableObject
    {
        [SerializeField, Min(0f)] private float walkSpeed = 2.2f;
        [SerializeField, Min(0f)] private float runSpeed = 4.8f;
        [SerializeField, Min(0f)] private float turnSpeed = 720f;
        [SerializeField] private float gravity = -20f;
        [SerializeField, Min(0f)] private float interactRadius = 1.6f;
        [SerializeField, Min(0f)] private float animationDamping = 0.1f;

        public float WalkSpeed => walkSpeed;

        public float RunSpeed => runSpeed;

        // Degrees per second.
        public float TurnSpeed => turnSpeed;

        public float Gravity => gravity;

        public float InteractRadius => interactRadius;

        // Seconds for the animator gait to blend towards its target.
        public float AnimationDamping => animationDamping;
    }
}
