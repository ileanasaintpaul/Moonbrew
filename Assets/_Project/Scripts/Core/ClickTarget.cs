using UnityEngine;

namespace Moonbrew.Core
{
    // Marks an object the mouse can click directly (a station, a bubble), so the click goes to it
    // instead of the ground behind. Needs a collider on it or on a child.
    public class ClickTarget : MonoBehaviour
    {
    }
}
