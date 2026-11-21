using System;
using System.Collections.Generic;

namespace Moonbrew.Player
{
    // The pack draws each body in one piece with one palette. To colour the top and the bottom apart, the body's
    // triangles are shared out by the vertices they move with: a triangle with most of its corners on the legs is bottom.
    public static class OutfitSplit
    {
        public static void Split(IReadOnlyList<int> triangles, IReadOnlyList<bool> onLegs, List<int> top, List<int> bottom)
        {
            if (triangles == null || onLegs == null || top == null || bottom == null)
            {
                throw new ArgumentNullException(triangles == null ? nameof(triangles) : onLegs == null ? nameof(onLegs) : top == null ? nameof(top) : nameof(bottom));
            }

            top.Clear();
            bottom.Clear();
            for (var i = 0; i + 2 < triangles.Count; i += 3)
            {
                var corners = 0;
                for (var k = 0; k < 3; k++)
                {
                    if (onLegs[triangles[i + k]])
                    {
                        corners++;
                    }
                }

                var part = corners >= 2 ? bottom : top;
                part.Add(triangles[i]);
                part.Add(triangles[i + 1]);
                part.Add(triangles[i + 2]);
            }
        }
    }
}
