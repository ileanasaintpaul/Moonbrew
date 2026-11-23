using System;

namespace Moonbrew.Player
{
    public class Stamina
    {
        public Stamina(int max)
        {
            Max = Math.Max(1, max);
            Current = Max;
        }

        public int Max { get; }

        public int Current { get; private set; }

        public bool IsEmpty => Current == 0;

        // Tools still work when it runs out, so a cost larger than what is left just empties it.
        public void Spend(int cost)
        {
            if (cost > 0)
            {
                Current = Math.Max(0, Current - cost);
            }
        }

        // A negative value means full.
        public void Restore(int saved)
        {
            Current = saved < 0 ? Max : Math.Min(saved, Max);
        }

        public void Wake(bool slept, int missedSleepPenalty)
        {
            Current = slept ? Max : Math.Max(0, Max - Math.Max(0, missedSleepPenalty));
        }
    }
}
