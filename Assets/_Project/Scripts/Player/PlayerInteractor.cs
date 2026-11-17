using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Moonbrew.Player
{
    // On the interact input, interacts with the closest Interactable within reach.
    public class PlayerInteractor : MonoBehaviour
    {
        private const int MaxNearbyColliders = 16;

        [SerializeField] private PlayerConfig settings;
        [SerializeField] private InputActionReference interactAction;

        private readonly Collider[] _nearby = new Collider[MaxNearbyColliders];
        private readonly List<Interactable> _candidates = new List<Interactable>(MaxNearbyColliders);
        private readonly List<Vector3> _positions = new List<Vector3>(MaxNearbyColliders);

        public event Action<Interactable> Interacted;

        public void Init(PlayerConfig playerConfig)
        {
            settings = playerConfig;
        }

        // Returns the object interacted with, or null when nothing is within reach.
        public Interactable TryInteract()
        {
            _candidates.Clear();
            _positions.Clear();
            var count = Physics.OverlapSphereNonAlloc(transform.position, settings.InteractRadius, _nearby, Physics.AllLayers, QueryTriggerInteraction.Collide);
            for (var i = 0; i < count; i++)
            {
                var interactable = _nearby[i].GetComponentInParent<Interactable>();
                if (interactable != null && !_candidates.Contains(interactable))
                {
                    _candidates.Add(interactable);
                    _positions.Add(_nearby[i].ClosestPoint(transform.position));
                }
            }

            var closest = InteractionTargeting.FindClosest(transform.position, _positions, settings.InteractRadius);
            if (closest < 0)
            {
                return null;
            }

            var target = _candidates[closest];
            target.Interact();
            Interacted?.Invoke(target);
            return target;
        }

        private void OnEnable()
        {
            if (interactAction != null)
            {
                interactAction.action.performed += OnInteract;
                interactAction.action.Enable();
            }
        }

        private void OnDisable()
        {
            if (interactAction != null)
            {
                interactAction.action.performed -= OnInteract;
            }
        }

        private void OnInteract(InputAction.CallbackContext context)
        {
            TryInteract();
        }
    }
}
