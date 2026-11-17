using System;
using UnityEngine;

namespace Moonbrew.Player
{
    // Marks an object the player can interact with. It needs a collider so the player can find it.
    public class Interactable : MonoBehaviour
    {
        [SerializeField] private string interactionId;

        public event Action<Interactable> Interacted;

        public string InteractionId => interactionId;

        public void Init(string id)
        {
            interactionId = id;
        }

        public void Interact()
        {
            Debug.Log($"[Interaction] {interactionId}");
            Interacted?.Invoke(this);
        }
    }
}
