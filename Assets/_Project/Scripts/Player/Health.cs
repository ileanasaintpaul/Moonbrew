using System;

namespace Moonbrew.Player
{
    public class Health
    {
        public Health(int max)
        {
            Max = Math.Max(1, max);
            Current = Max;
        }

        public int Max { get; }

        public int Current { get; private set; }

        public bool IsEmpty => Current == 0;

        public void Lose(int damage)
        {
            if (damage > 0)
            {
                Current = Math.Max(0, Current - damage);
            }
        }

        // A negative value means full.
        public void Restore(int saved)
        {
            Current = saved < 0 ? Max : Math.Min(saved, Max);
        }

        // A night's sleep heals completely.
        public void Wake()
        {
            Current = Max;
        }
    }
}
