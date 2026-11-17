using UnityEngine;

namespace Moonbrew.Farming
{
    // A clump of grass: it stops the hoe until the sickle cuts it. Its trigger lets the farm find it.
    // The ID is set once by the editor setup from its position, so a save can tell which clumps were cut.
    [RequireComponent(typeof(Collider))]
    public class CuttableGrass : MonoBehaviour
    {
        [SerializeField] private string id;

        public string Id => id;

        public bool IsCut => !gameObject.activeSelf;

        public void Init(string grassId)
        {
            id = grassId;
        }

        public void Cut()
        {
            gameObject.SetActive(false);
        }
    }
}
