using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Cafe
{
    // A café table and its seats. Each seat sits on a chair, facing the table.
    public class CafeTable : MonoBehaviour
    {
        [SerializeField] private Transform[] seats = new Transform[0];

        public IReadOnlyList<Transform> Seats => seats;

        // For tables built from code.
        public void Init(Transform[] tableSeats)
        {
            seats = tableSeats ?? new Transform[0];
        }
    }
}
