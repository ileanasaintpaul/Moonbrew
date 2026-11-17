using System;

namespace Moonbrew.Farming
{
    // The water left in the can. Saved as the water used, so a fresh can is simply full.
    public class WateringCan
    {
        public WateringCan(int capacity, int used = 0)
        {
            if (capacity < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), capacity, "A can holds at least one watering.");
            }

            Capacity = capacity;
            Water = Math.Max(0, Math.Min(capacity, capacity - used));
        }

        public int Capacity { get; }

        public int Water { get; private set; }

        public int Used => Capacity - Water;

        public bool IsEmpty => Water == 0;

        // One watering; false when the can is empty.
        public bool Pour()
        {
            if (IsEmpty)
            {
                return false;
            }

            Water--;
            return true;
        }

        // Returns false when the can was already full.
        public bool Refill()
        {
            if (Water == Capacity)
            {
                return false;
            }

            Water = Capacity;
            return true;
        }
    }
}
