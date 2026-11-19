using UnityEngine;

namespace Moonbrew.Core
{
    // Where the player comes out of a passage from another scene, facing this transform's forward.
    public class SceneArrival : MonoBehaviour
    {
        [SerializeField] private string id;

        public string Id => id;
    }
}
