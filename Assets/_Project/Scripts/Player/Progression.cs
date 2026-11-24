using System;
using System.Collections.Generic;

namespace Moonbrew.Player
{
    // The owner's level, the experience towards the next one, and the skill points earned so far.
    public class Progression
    {
        private readonly IReadOnlyList<int> _xpToNext;
        private readonly int _pointsPerLevel;
        private readonly HashSet<string> _crafted = new HashSet<string>();

        public Progression(IReadOnlyList<int> xpToNext, int pointsPerLevel)
        {
            _xpToNext = xpToNext != null && xpToNext.Count > 0 ? xpToNext : new[] { 100 };
            _pointsPerLevel = Math.Max(0, pointsPerLevel);
        }

        public int Level { get; private set; } = 1;

        // Experience gathered since the current level was reached.
        public int Xp { get; private set; }

        public int SkillPoints { get; private set; }

        public IEnumerable<string> Crafted => _crafted;

        // The curve's last step repeats for every level past it.
        public int XpToNext => Math.Max(1, _xpToNext[Math.Min(Level - 1, _xpToNext.Count - 1)]);

        // Returns each level reached, in order, so every reward is given.
        public List<int> AddXp(int amount)
        {
            var reached = new List<int>();
            if (amount <= 0)
            {
                return reached;
            }

            Xp += amount;
            while (Xp >= XpToNext)
            {
                Xp -= XpToNext;
                Level++;
                SkillPoints += _pointsPerLevel;
                reached.Add(Level);
            }

            return reached;
        }

        // True the first time an item is made, so its experience is given once.
        public bool FirstCraft(string itemId)
        {
            return !string.IsNullOrEmpty(itemId) && _crafted.Add(itemId);
        }

        public void Restore(int level, int xp, int skillPoints, IEnumerable<string> crafted)
        {
            Level = Math.Max(1, level);
            Xp = Math.Max(0, xp);
            SkillPoints = Math.Max(0, skillPoints);
            _crafted.Clear();
            foreach (var id in crafted ?? new string[0])
            {
                FirstCraft(id);
            }
        }
    }
}
