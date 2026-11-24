using System.Collections.Generic;
using Moonbrew.Core;

namespace Moonbrew.Npc
{
    // Friendship points with each villager.
    public class Relationships
    {
        private readonly Dictionary<string, int> _points = new Dictionary<string, int>();

        public int PointsWith(string villagerId)
        {
            return villagerId != null && _points.TryGetValue(villagerId, out var points) ? points : 0;
        }

        // Returns false, changing nothing, for an empty ID or no points.
        public bool Add(string villagerId, int points)
        {
            if (string.IsNullOrEmpty(villagerId) || points <= 0)
            {
                return false;
            }

            _points[villagerId] = PointsWith(villagerId) + points;
            return true;
        }

        public List<RelationSave> ToSave()
        {
            var saved = new List<RelationSave>();
            foreach (var pair in _points)
            {
                saved.Add(new RelationSave { villagerId = pair.Key, points = pair.Value });
            }

            return saved;
        }

        public void Restore(IEnumerable<RelationSave> saved)
        {
            _points.Clear();
            foreach (var relation in saved ?? new List<RelationSave>())
            {
                if (relation != null && !string.IsNullOrEmpty(relation.villagerId) && relation.points > 0)
                {
                    _points[relation.villagerId] = relation.points;
                }
            }
        }
    }
}
