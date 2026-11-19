using UnityEngine;

namespace Moonbrew.Core
{
    // A trigger on a path: walking into it takes the player to another scene.
    [RequireComponent(typeof(Collider))]
    public class ScenePortal : MonoBehaviour
    {
        private const string PlayerTag = "Player";

        [SerializeField] private string scene;
        [Tooltip("ID of the SceneArrival the player comes out at in that scene.")]
        [SerializeField] private string arrival;
        [SerializeField] private EventBusAsset eventBus;

        public void Init(string targetScene, string targetArrival, EventBusAsset bus)
        {
            scene = targetScene;
            arrival = targetArrival;
            eventBus = bus;
        }

        private void OnTriggerEnter(Collider other)
        {
            if (eventBus != null && other.CompareTag(PlayerTag))
            {
                eventBus.Publish(new TravelRequested(scene, arrival));
            }
        }
    }
}
