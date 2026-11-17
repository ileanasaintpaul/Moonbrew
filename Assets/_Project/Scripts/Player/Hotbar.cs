using System;

namespace Moonbrew.Player
{
    // Which hotbar slot the player is holding. The hotbar shows the first slots of the player's bag.
    public class Hotbar
    {
        public Hotbar(int slotCount)
        {
            if (slotCount < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(slotCount), "A hotbar needs at least one slot.");
            }

            SlotCount = slotCount;
        }

        public int SlotCount { get; }

        public int SelectedIndex { get; private set; }

        // Returns true when the selection changed; out-of-range slots are ignored.
        public bool Select(int index)
        {
            if (index < 0 || index >= SlotCount || index == SelectedIndex)
            {
                return false;
            }

            SelectedIndex = index;
            return true;
        }

        // The number keys: 1 to 9 select the first nine slots, 0 the tenth. The input action
        // gives the slot number from 1 (Scale processor on each key), 0 meaning "released".
        public bool SelectFromKey(float slotNumber)
        {
            var number = (int)Math.Round(slotNumber);
            return number >= 1 && Select(number - 1);
        }
    }
}
