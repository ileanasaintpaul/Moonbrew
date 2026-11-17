using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonbrew.Player
{
    public static class InteractionTargeting
    {
        // Index of the candidate closest to the origin on the ground plane, or -1 when none is within the radius.
        public static int FindClosest(Vector3 origin, IReadOnlyList<Vector3> candidates, float radius)
        {
            if (candidates == null)
            {
                throw new ArgumentNullException(nameof(candidates));
            }

            var closest = -1;
            var closestDistance = radius * radius;
            for (var i = 0; i < candidates.Count; i++)
            {
                var offset = candidates[i] - origin;
                var distance = offset.x * offset.x + offset.z * offset.z;
                if (distance <= closestDistance)
                {
                    closest = i;
                    closestDistance = distance;
                }
            }

            return closest;
        }
    }
}
